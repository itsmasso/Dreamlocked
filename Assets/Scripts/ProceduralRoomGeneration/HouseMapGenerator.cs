using System.Collections.Generic;

using UnityEngine;
using System.Diagnostics;
using System;
using System.Linq;
using Unity.Netcode;

public enum CellType
{
	None,
	Room,
	Hallway,
	Stairs,
	Door,
}

public class HouseMapGenerator : NetworkSingleton<HouseMapGenerator>
{	
	private enum RoomEdgeDirections
	{
		Top,
		Bottom,
		Right,
		Left
	
	}
	

	[Header("Map Properties")]
	[SerializeField] private Vector3Int mapSize;
	[SerializeField] private int floorHeight;
	private Vector3 worldBottomLeft;
	[SerializeField] private int floors;
	[SerializeField] private AstarPath aStarComponent;
	public bool isLevelGenerated;

	[Header("Grid Properties")]
	[SerializeField] private float nodeRadius;
	private int nodeDiameter;
	[SerializeField] private Grid3D grid;
	
	[Header("Prefab Components")]
	[SerializeField] private GameObject roomCeilingPrefab;
	[SerializeField]private GameObject roomCeilingWithLightPrefab;
	[SerializeField] private GameObject roomFloorPrefab;
	[SerializeField] private GameObject hallwayLightPrefab;
	[SerializeField] private GameObject wallPrefab;
	[SerializeField] private float wallThickness, ceilingThickness, floorThickness;
	[SerializeField] private List<GameObject> roomPrefabList;
	
	[Header("Special Room Properties")]
	[SerializeField] private List<GameObject> specialRooms;
	private int specialRoomIndex = 0;
	
	[Header("Room Properties")]
	[SerializeField] private int roomsPerFloor;
	
	[Header("Spawn Room Algorithm")]
	[SerializeField] private int spaceBetweenRooms;
	[SerializeField] public List<GameObject> rooms {get; private set;}
	[SerializeField] private HouseMapRoomGenerator roomGenerator;
	[SerializeField] private int maxIteration = 30;

	//Delaunay Triangulation
	private DelaunayTriangulation delaunay;
	
	[Header("Create Path Algorithm (MST/Prims)")]
	private Prims_MST prims;
	private HashSet<Prims_MST.Edge> selectedEdges;
	[SerializeField] private float spawnCycleChance = 0.125f;
	
	[Header("Create Hallways (A*)")]
	[SerializeField] private float hallwayLightSpawnInterval;
	private List<NetworkObject> hallwayLights = new List<NetworkObject>();
	private List<Node> hallways;
	private AStarPathfinder hallwayPathFinder;
	private float currentHallwaySpawnIndex;
	
	
	[Header("Stairs")]
	[SerializeField] private List<GameObject> stairRooms;
	[SerializeField] private int chanceToSpawnStairs;
	[SerializeField] private int spawnStairGuarenteedPity;

	[Header("Debug")]
	public Color color = new Color(1, 0, 0, 0.1f);
	[SerializeField] private bool drawGizmos;
	[SerializeField] private bool drawAllNodes;
	
	protected override void Awake()
	{
		base.Awake();
		nodeDiameter = Mathf.RoundToInt(nodeRadius*2);
		mapSize.y = Mathf.RoundToInt(floorHeight * floors);
		spaceBetweenRooms = Mathf.RoundToInt(spaceBetweenRooms / nodeDiameter) * nodeDiameter;
		worldBottomLeft = transform.position - Vector3.right * mapSize.x/2 - Vector3.up * mapSize.y/2 - Vector3.forward * mapSize.z/2;
		GameManager.Instance.onNextLevel += ClearMap;
	}

	void Start()
	{	
		Generate();
		
	}

	public void Generate()
	{
		
		UnityEngine.Random.InitState(GameManager.Instance.seed.Value);
		
		Stopwatch sw = new Stopwatch();
		sw.Start();
		isLevelGenerated= false;
		grid = new Grid3D(mapSize, nodeRadius, transform.position);
		grid.CreateGrid();
		rooms = new List<GameObject>();
		hallwayPathFinder = new AStarPathfinder(grid);
		hallways  = new List<Node>();
		currentHallwaySpawnIndex = 0;
		specialRoomIndex = 0;
		
		CreateRooms();
		MarkRoomsInGrid(rooms);
		CreateHallways();
		
		foreach(Node n in grid.grid)
		{
			if(n.cellType == CellType.Hallway)
				SpawnHallways(n);
			if(n.cellType == CellType.Door)
				SpawnDoorWay(n);
		}
		
		roomGenerator.SpawnRoomObjects();	
		
		aStarComponent.Scan();
		sw.Stop();
		isLevelGenerated = true;
		if(IsServer)
		{
		    
		    GameManager.Instance.ChangeGameState(GameState.GameStart);
		}

		UnityEngine.Debug.Log("Finished Generating in " + sw.ElapsedMilliseconds + "ms");
		
	}

    /*****************************************************************
	* GetRoomsList
	*****************************************************************
	* Author: Dylan Werelius
	*****************************************************************
	* Description:
		This function will return a list of the positions of all the
		rooms that have been spawned. I use it in the UnitManager
		script to spawn the mannequin monsters in each room.
	*****************************************************************/
    public List<Vector3> GetNormalRoomsList()
	{
		List<Vector3> roomPositions = new List<Vector3>();
		//UnityEngine.Debug.Log("Getting Room Positions");
		foreach(GameObject room in rooms) {
			if (!room.GetComponent<Room>().isSpecialRoom && !room.GetComponent<Room>().isStairs)
			{
				roomPositions.Add(room.transform.position);
			}
		}
		return roomPositions;
	}
	
	public Vector3 GetPlayerSpawnPosition(){
		//UnityEngine.Debug.Log(rooms.Count);
		Vector3 position = rooms.FirstOrDefault(r => r.GetComponent<Room>().isMainRoom == true).transform.position;
		
		return position;
	}
	
	public Vector3 GetRandomHallwayPosition()
	{
		Node randomHallwayNode = hallways[UnityEngine.Random.Range(0, hallways.Count)];
		return new Vector3(randomHallwayNode.pos.x, randomHallwayNode.pos.y - nodeRadius, randomHallwayNode.pos.z);
	}
	
	public Vector3 GetRandomRoomPosition()
	{
		return rooms[UnityEngine.Random.Range(0, rooms.Count)].transform.position;
	}
	
	public List<Vector3> GetHallwayList()
	{
		List<Vector3> hallwayPosList = new List<Vector3>();
		foreach(Node node in hallways)
			hallwayPosList.Add(new Vector3(node.pos.x, node.pos.y - nodeRadius, node.pos.z));
		return hallwayPosList;
	}
	
	public Room GetRoomFromPosition(Vector3 position)
	{
	    foreach(GameObject roomObj in rooms)
	    {
	        Room room = roomObj.GetComponent<Room>();
	        if(room.PositionInBounds(position))
	        {
	            return room;
	        }
	    }
	    return null;
	}
	
	public Vector3 GetDoorClosestToTarget(Vector3 targetPosition)
	{
	    Room room = GetRoomFromPosition(targetPosition);
	    if(room != null)
	    {
	    	Vector3 doorNodePos = room.doorNode.Select(d => d.position).OrderBy(d => Vector3.Distance(d, targetPosition)).ThenBy(_ => UnityEngine.Random.value).FirstOrDefault(); 
	    	Vector3[] doorNeighbours = new Vector3[]
	    	{
	    	   new Vector3(doorNodePos.x + nodeRadius/2, doorNodePos.y, doorNodePos.z),  
	    	   new Vector3(doorNodePos.x - nodeRadius/2, doorNodePos.y, doorNodePos.z), 
	    	   new Vector3(doorNodePos.x, doorNodePos.y, doorNodePos.z + nodeRadius/2), 
	    	   new Vector3(doorNodePos.x, doorNodePos.y, doorNodePos.z - nodeRadius/2), 
	    	   doorNodePos
	    	};
	    	
	    	return doorNeighbours.Where(n => grid.GetNode(n).cellType == CellType.Hallway || grid.GetNode(n).cellType == CellType.Door).OrderBy(_ => UnityEngine.Random.value).FirstOrDefault();
	    }
	    return Vector3.zero;
	}
	
	private Vector3Int GetRandomRoomSpawnPosition(int currentFloor, Vector3 size)
	{
		//calculating min and max based on grid/map size
		int roomXPos = UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.x + size.x + nodeDiameter*2)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.x + mapSize.x - size.x - nodeDiameter*2)/nodeDiameter)) * nodeDiameter;
		int roomYPos = (int)worldBottomLeft.y + currentFloor * floorHeight;
		int roomZPos = UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.z + size.z + nodeDiameter*2)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.z + mapSize.z - size.z - nodeDiameter*2)/nodeDiameter)) * nodeDiameter;
	
		//ensure positions snap to grid
		while(roomXPos % nodeDiameter != 0)
			roomXPos++;
		while(roomYPos % nodeDiameter != 0 && roomYPos > 0)
			roomYPos++;
		while(roomZPos % nodeDiameter != 0)
			roomZPos++;
		
		Vector3Int roomPos = new Vector3Int(roomXPos, roomYPos, roomZPos);
		
		return roomPos;
	}

	private void CreateRooms()
	{
		for(int floorCount = 0; floorCount < floors; floorCount++)
		{
			int pity = 0;
			for(int roomCount = 0; roomCount < roomsPerFloor; roomCount++)
			{	
				float randomNum = UnityEngine.Random.value;
				GameObject newRoom;
				if((randomNum < chanceToSpawnStairs/100f || pity >= spawnStairGuarenteedPity) && floorCount < floors-1)
				{
					//spawn stair room
					newRoom = stairRooms[0];	
				}
				else
				{
					//spawn normal room
					// Old Working Code (Uncomment if you break something)
					// if(!spawnedMainRoom) 
					// {
					// 	newRoom = mainRoomPrefab;
					// 	spawnedMainRoom = true;
					if (specialRoomIndex < specialRooms.Count) {
						newRoom = specialRooms[specialRoomIndex];
						specialRoomIndex++;
					}else{
						newRoom = roomPrefabList[UnityEngine.Random.Range(0, roomPrefabList.Count)];
					}
				}
				
				//choosing random rotation for the room
				int[] angles = { 0, 90, 180, 270 };
				int randomIndex = UnityEngine.Random.Range(0, angles.Length);
				int chosenAngle = angles[randomIndex];

				//creating room
				GameObject room = Instantiate(newRoom, transform.position, Quaternion.Euler(0, chosenAngle, 0));
				room.transform.SetParent(gameObject.transform);
				Room roomComponent = room.GetComponent<Room>();
				roomComponent.yRotation = chosenAngle;
				room.transform.position = GetRandomRoomSpawnPosition(floorCount, roomComponent.size);
				roomComponent.position = Vector3Int.RoundToInt(room.transform.position);		
				
				//setting room rotation to random rotation
				if(chosenAngle == 90 || chosenAngle == 270)
				{
					int temp = roomComponent.size.x;
					roomComponent.size.x = roomComponent.size.z;
					roomComponent.size.z = temp;
				}
				
				rooms.Add(room); 
				
				SpaceRooms(rooms);
			}
		}
	}
	
	
	private void MarkRoomsInGrid(List<GameObject> rooms)
	{
		//marking the rooms in the grid
		foreach(GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			for(int x = 0; x < room.size.x/nodeDiameter; x++)
			{
				for(int y = 0; y < room.size.y/nodeDiameter; y++)
				{
					for(int z = 0; z < room.size.z/nodeDiameter; z++)
					{
						Vector3 nodePos = new Vector3(room.Min.x + (x * nodeDiameter + nodeRadius), room.Min.y + (y * nodeDiameter + nodeRadius), room.Min.z + (z * nodeDiameter + nodeRadius));
						
						//Default to marking the node as part of the room
						if(grid.GetNode(nodePos).cellType != CellType.Door)
						{
							grid.SetNodeType(nodePos, CellType.Room);
						}
					}
				}
			}
			
		}
		
		foreach(GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			foreach(Transform doorNode in room.doorNode)
			{
				grid.SetNodeType(doorNode.position, CellType.Door);
			}
		}

	}
	
	
	private void SpaceRooms(List<GameObject> rooms)
	{
		bool allRoomsSeperated = false;
		int iterations = 0;
		while(!allRoomsSeperated && iterations < maxIteration)
		{
			for (int currentRoom = 0; currentRoom < rooms.Count; currentRoom++)
			{
				allRoomsSeperated = true;
				for (int otherRoom = 0; otherRoom < rooms.Count; otherRoom++)
				{
					
					//skip if comparing the same room
					if (currentRoom == otherRoom)
						continue;
						
					Room roomA = rooms[currentRoom].GetComponent<Room>();
					Room roomB = rooms[otherRoom].GetComponent<Room>();

					//if rooms are too close to each other, adjust their positions
					if (IsRoomTooClose(roomA, roomB, spaceBetweenRooms))
					{
						allRoomsSeperated = false;
						//get direction of the two rooms
						Vector3 direction = (roomA.Center - roomB.Center).normalized;

						int xMovement = Mathf.RoundToInt(direction.x * nodeDiameter);
						int zMovement = Mathf.RoundToInt(direction.z * nodeDiameter);
						
						//ensure positions snap to grid
						while(xMovement % nodeDiameter != 0)
							xMovement++;
						while(zMovement % nodeDiameter != 0)
							zMovement++;
							
						Vector3Int movement = new Vector3Int(xMovement,0,zMovement);
						
						//Move rooms away from each other
						roomA.position += movement;
						roomB.position -= movement; // Move in opposite direction
						
						//clamp positions to make sure rooms stay within bounds
						roomA.position = new Vector3Int(
							Mathf.Clamp(roomA.position.x, (int)worldBottomLeft.x + nodeDiameter*2, (int)(worldBottomLeft.x + mapSize.x - roomA.size.x - nodeDiameter*2)),
							roomA.position.y,
							Mathf.Clamp(roomA.position.z, (int)worldBottomLeft.z + nodeDiameter*2, (int)(worldBottomLeft.z + mapSize.z - roomA.size.z - nodeDiameter*2))
						);
						roomB.position = new Vector3Int(
							Mathf.Clamp(roomB.position.x, (int)worldBottomLeft.x + nodeDiameter*2, (int)(worldBottomLeft.x + mapSize.x - roomB.size.x - nodeDiameter*2)),
							roomB.position.y,
							Mathf.Clamp(roomB.position.z, (int)worldBottomLeft.z + nodeDiameter*2, (int)(worldBottomLeft.z + mapSize.z - roomB.size.z - nodeDiameter*2))
						);

						//Update the rooms in the list
						rooms[currentRoom].GetComponent<Room>().position = roomA.position;
						rooms[otherRoom].GetComponent<Room>().position = roomB.position;
					}
				}
			}
			iterations++;
		}
		if(iterations == maxIteration)
		{
			//Debug.LogWarning("Max iterations reached! Removing overlapped rooms.");
			List<GameObject> roomsToRemove = new List<GameObject>();
			for (int currentRoom = 0; currentRoom < rooms.Count; currentRoom++)
			{
				for (int otherRoom = 0; otherRoom < rooms.Count; otherRoom++)
				{
					//Skip if comparing the same room
					if (currentRoom == otherRoom)
						continue;
					
					Room roomA = rooms[currentRoom].GetComponent<Room>();			
					Room roomB = rooms[otherRoom].GetComponent<Room>();

					if (IsRoomTooClose(roomA, roomB, spaceBetweenRooms))
					{
						if(!roomsToRemove.Contains(roomB.gameObject))
						{
							roomsToRemove.Add(roomB.gameObject);
						}
					}
				}
			}
			
			foreach(GameObject room in roomsToRemove)
			{
				rooms.Remove(room);
			}
		}
		
		foreach(GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			room.transform.position = new Vector3(room.position.x + room.size.x/2, room.position.y, room.position.z+ room.size.z/2);
		}
	}
	
	private bool IsRoomTooClose(Room roomA, Room roomB, int offsetSpace)
	{
		//Expand room b by the offset on all sides
		BoundsInt expandedRoomB = new BoundsInt(
			roomB.Min.x - offsetSpace,
			roomB.Min.y,
			roomB.Min.z - offsetSpace,
			roomB.size.x + 2 * offsetSpace,
			roomB.size.y,
			roomB.size.z + 2 * offsetSpace
		);
		
		bool overlapX = roomA.Min.x < expandedRoomB.xMax && roomA.Max.x > expandedRoomB.xMin;
		bool overlapY =  roomA.Min.y < expandedRoomB.yMax && roomA.Max.y > expandedRoomB.yMin;
		bool overlapZ = roomA.Min.z < expandedRoomB.zMax && roomA.Max.z > expandedRoomB.zMin;
		
		return overlapX && overlapZ && overlapY;
	}
	
	private void CreateHallways()
	{
		for(int floorCount = 0; floorCount < floors; floorCount++)
		{
			Triangulate(floorCount);
			CreatePaths(floorCount);
			PathfindHallways(floorCount);
		}
	}
	

	private void Triangulate(int currentFloor)
	{
		List<Vector2> roomPoints = new List<Vector2>();
		for(int x = 0; x < mapSize.x/nodeDiameter; x++)
		{
			for(int z = 0; z < mapSize.z/nodeDiameter; z++)
			{
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (currentFloor * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);
			
				if(grid.GetNode(worldPoint).cellType == CellType.Door)
				{
					roomPoints.Add(new Vector2(grid.GetNode(worldPoint).pos.x,grid.GetNode(worldPoint).pos.z));
				}
			}
		}
		delaunay = new DelaunayTriangulation(roomPoints, new Vector2Int(mapSize.x, mapSize.z));
		delaunay.Triangulation();
	}
	
	private void CreatePaths(int currentFloor)
	{
		//Create list of edges used for prims algorithm class
		List<Prims_MST.Edge> edges = new List<Prims_MST.Edge>();
		foreach(DelaunayTriangulation.Edge edge in delaunay.GetEdges())
		{
			edges.Add(new Prims_MST.Edge(edge.vertexU, edge.vertexV));
		}
		prims = new Prims_MST(edges);
		//edges part of the MST
		selectedEdges = new HashSet<Prims_MST.Edge>(prims.GenerateMST());
		
		//edges not apart of the MST
		HashSet<Prims_MST.Edge> unselectedEdges = new HashSet<Prims_MST.Edge>(edges);
		unselectedEdges.ExceptWith(selectedEdges);
		
		//giving a chance to allow an unselected edge in the MST to allow some cycles for better environment
		foreach(Prims_MST.Edge edge in unselectedEdges)
		{
			if(UnityEngine.Random.value < spawnCycleChance)
			{
				selectedEdges.Add(edge);
			}
		}
		//Debug
		foreach (Prims_MST.Edge edge in selectedEdges)
		{
			if(drawGizmos)
			{
			    UnityEngine.Debug.DrawLine(
				new Vector3(edge.vertexU.x, (int)worldBottomLeft.y + currentFloor * floorHeight, edge.vertexU.y),
				new Vector3(edge.vertexV.x, (int)worldBottomLeft.y + currentFloor * floorHeight, edge.vertexV.y),
				Color.blue,
				10f
			);
			}
		}
		
	}
	
	private void PathfindHallways(int currentFloor)
	{
		foreach(Prims_MST.Edge edge in selectedEdges)
		{
			List<Node> path = new List<Node>();

			Vector3 hallwayDoorA = new Vector3(edge.vertexU.x, (int)worldBottomLeft.y + (currentFloor * floorHeight) + nodeRadius, edge.vertexU.y);
			Vector3 hallwayDoorB = new Vector3(edge.vertexV.x, (int)worldBottomLeft.y + (currentFloor * floorHeight)+ nodeRadius, edge.vertexV.y);
			
			path = hallwayPathFinder.FindPath(hallwayDoorA, hallwayDoorB, true);
			
			foreach(Node n in path)
			{
				if(n.cellType != CellType.Room && n.cellType != CellType.Door){
					grid.SetNodeType(n.pos, CellType.Hallway);
					hallways.Add(n);
				}
			}
		}
	}
	
	private void SpawnRoomPart(GameObject prefab, Vector3 position, Quaternion rotation)
	{
	    GameObject part = Instantiate(prefab, position, Quaternion.identity);
    	part.transform.SetParent(transform);
    	part.transform.rotation = rotation;
	}
	
	private void TrySpawnHallwayWall(Vector3 nodePos, Vector3 wallPosition, Quaternion wallRotation)
	{
		CellType type = grid.GetNode(nodePos).cellType;
		if (type == CellType.None)
		{
			SpawnRoomPart(wallPrefab, wallPosition, wallRotation);
		}
	}
	
	private void SpawnHallways(Node node)
	{
		Vector3 ceilingPosition = new Vector3(node.pos.x, node.pos.y + nodeRadius - 0.2f, node.pos.z);
		Vector3 floorPosition = new Vector3(node.pos.x, node.pos.y - nodeRadius + 0.05f, node.pos.z);
		
		SpawnRandomCeiling(ceilingPosition);
    	SpawnRoomPart(roomFloorPrefab, floorPosition, Quaternion.identity);
		
		TrySpawnHallwayWall(node.pos - Vector3.right * nodeDiameter, new Vector3(node.pos.x - nodeRadius - 0.05f, node.pos.y + 0.05f, node.pos.z), Quaternion.Euler(0, -180, 0));
    	TrySpawnHallwayWall(node.pos + Vector3.right * nodeDiameter, new Vector3(node.pos.x + nodeRadius, node.pos.y+ 0.05f, node.pos.z), Quaternion.identity);
    	TrySpawnHallwayWall(node.pos + Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y+ 0.05f, node.pos.z + nodeRadius), Quaternion.Euler(0, -90, 0));
    	TrySpawnHallwayWall(node.pos - Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y+ 0.05f, node.pos.z - nodeRadius - 0.05f), Quaternion.Euler(0, 90, 0));
    	currentHallwaySpawnIndex++;
	}
	
	private void SpawnRandomCeiling(Vector3 pos)
	{
	    if(currentHallwaySpawnIndex % hallwayLightSpawnInterval == 0)
	    {
	        GameObject ceiling = Instantiate(roomCeilingWithLightPrefab, pos, Quaternion.identity);
    		ceiling.transform.SetParent(transform);
    		if(IsServer)
    		{
    		    GameObject roomLightObject = Instantiate(hallwayLightPrefab, ceiling.GetComponent<CeilingPiece>().lightsTransform.position, Quaternion.identity);
            	roomLightObject.GetComponent<NetworkObject>().Spawn(true);
            	hallwayLights.Add(roomLightObject.GetComponent<NetworkObject>());
    		}
	    }else
	    {
	        SpawnRoomPart(roomCeilingPrefab, pos, Quaternion.identity);
	    }
	}
	
	private void TrySpawnDoorWall(Vector3 nodePos, Vector3 wallPosition, Quaternion wallRotation)
	{
		if (grid.GetNode(nodePos).cellType == CellType.None)
		{
			SpawnRoomPart(wallPrefab, wallPosition, wallRotation);
		}
	}
	
	private void SpawnDoorWay(Node node)
	{
		Vector3 ceilingPosition = new Vector3(node.pos.x, node.pos.y + nodeRadius - 0.2f, node.pos.z);
		Vector3 floorPosition = new Vector3(node.pos.x, node.pos.y - nodeRadius+ 0.05f, node.pos.z);
		
		SpawnRoomPart(roomCeilingPrefab, ceilingPosition, Quaternion.identity);
    	SpawnRoomPart(roomFloorPrefab, floorPosition, Quaternion.identity);	
		
		TrySpawnDoorWall(node.pos - Vector3.right * nodeDiameter, new Vector3(node.pos.x - nodeRadius - 0.05f, node.pos.y+ 0.05f, node.pos.z), Quaternion.Euler(0, -180, 0));
    	TrySpawnDoorWall(node.pos + Vector3.right * nodeDiameter, new Vector3(node.pos.x + nodeRadius, node.pos.y+ 0.05f, node.pos.z), Quaternion.identity);
    	TrySpawnDoorWall(node.pos + Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y+ 0.05f, node.pos.z + nodeRadius), Quaternion.Euler(0, -90, 0));
    	TrySpawnDoorWall(node.pos - Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y+ 0.05f, node.pos.z - nodeRadius - 0.05f), Quaternion.Euler(0, 90, 0));

	}
	
	public void ClearMap()
	{
		roomGenerator.ClearObjects();
	    foreach(Transform child in transform){
			Destroy(child.gameObject);
		}
		foreach (NetworkObject light in hallwayLights)
        {
            if (light.IsSpawned)
            {
                // Despawn the networked object
                light.Despawn();

                // Destroy the local game object
                Destroy(light.gameObject);
            }
        }
		grid.ClearGrid();
		delaunay.Clear();
		rooms.Clear();
		hallways.Clear();
		
		
		
	}

    public override void OnDestroy()
    {
        base.OnDestroy();
        ClearMap();
        GameManager.Instance.onNextLevel -= ClearMap;
    }
	private void OnDrawGizmos() {
		
		
		if(Application.isPlaying && drawGizmos)
		{
			if(grid != null)
			{
				
				foreach(Node n in grid.grid)
				{
					Gizmos.color = color;		
					if(!drawAllNodes && n.cellType == CellType.None)
					{
						continue;
					}
					
					if(n.cellType == CellType.Room)
					{
						Gizmos.color = Color.red;
					}
				
					if(n.cellType == CellType.Hallway)
					{
						Gizmos.color = Color.blue;
					}
					if(n.cellType == CellType.Door)
					{
						Gizmos.color = Color.yellow;
					}
					
					
					Gizmos.DrawCube(n.pos, Vector3.one * (grid.nodeDiameter-.1f));
				}
			}
		}
	}
	
}
