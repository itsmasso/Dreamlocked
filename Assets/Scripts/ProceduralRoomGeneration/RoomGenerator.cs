using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using UnityEditor.Timeline;
using System;
using Unity.VisualScripting;

public enum CellType
{
	None,
	Room,
	Hallway,
	Stairs,
	Door,
}

public class RoomGenerator : MonoBehaviour
{	
	private enum RoomEdgeDirections
	{
		Top,
		Bottom,
		Right,
		Left
	
	}
	
	public class Room
	{
		public BoundsInt bounds;
		public bool isStairs;
		public Room(BoundsInt bounds, bool isStairs)
		{
			this.bounds = bounds;
			this.isStairs = isStairs;
		}
	}

	[Header("Map Properties")]
	[SerializeField] private Vector3Int mapSize;
	[SerializeField] private int floorHeight;
	private Vector3 worldBottomLeft;
	[SerializeField] private int floors;
	
	private int currentFloor;
	
	[Header("Grid Properties")]
	[SerializeField] private float nodeRadius;
	private int nodeDiameter;
	[SerializeField] private Grid3D grid;
	
	[Header("Prefab Components")]
	[SerializeField] private GameObject roomCeilingPrefab, roomFloorPrefab, wallPrefab, doorPrefab;
	[SerializeField] private float wallThickness,ceilingThickness,floorThickness;
	
	[Header("Room Properties")]
	[SerializeField] private int roomsPerFloor;
	[SerializeField] private Vector3Int roomMaxSize, roomMinSize;
	
	[Header("Spawn Room Algorithm")]
	[SerializeField] private int spaceBetweenRooms;
	[SerializeField] private List<Room> rooms;
	
	[SerializeField] private int maxIteration = 30;

	
	private DelaunayTriangulation delaunay;
	private Prims_MST prims;
	private HashSet<Prims_MST.Edge> selectedEdges;
	[Header("Create Path Algorithm (MST/Prims)")]
	[SerializeField] private float spawnCycleChance = 0.125f;
	
	[Header("Create Hallways (A*)")]
	private HashSet<Node> hallways;
	private AStarPathfinder hallwayPathFinder;
	
	[Header("Stairs")]
	[SerializeField] private int chanceToSpawnStairs;
	[SerializeField] private int spawnStairGuarenteedPity;

	[Header("Debug")]
	public Color color = new Color(1, 0, 0, 0.1f);
	[SerializeField] private bool drawGizmos;
	[SerializeField] private bool drawAllNodes;
	[SerializeField] private GameObject roomPlaceholder, hallwayPlaceholder, doorPlaceholder;
	
	private void Start()
	{
		nodeDiameter = Mathf.RoundToInt(nodeRadius*2);
		mapSize.y = Mathf.RoundToInt(floorHeight * floors);
		spaceBetweenRooms = Mathf.RoundToInt(spaceBetweenRooms / nodeDiameter) * nodeDiameter;
		worldBottomLeft = transform.position - Vector3.right * mapSize.x/2 - Vector3.up * mapSize.y/2 - Vector3.forward * mapSize.z/2;
		grid = new Grid3D(mapSize, nodeRadius, transform.position);
		Generate();
	}
	
	private void Generate()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		
		currentFloor = 0;
		grid.CreateGrid();
		rooms = new List<Room>();
		hallwayPathFinder = new AStarPathfinder(grid);
		hallways  = new HashSet<Node>();
		
		
		CreateRooms();
		MarkRoomsInGrid(rooms);
		CreateHallways();

		foreach(Room room in rooms)
			SpawnRoom(room.bounds);
		/*
		foreach(Node n in grid.grid)
			if(n.cellType == CellType.Hallway)
				SpawnHallways(n, hallwayPlaceholder);
		*/
		sw.Stop();
		UnityEngine.Debug.Log("Finished Generating in " + sw.ElapsedMilliseconds + "ms");
	}
	
	private Vector3Int GetRandomRoomPosition(int currentFloor)
	{
		//calculating min and max based on grid/map size
		int roomXPos = UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.x + roomMaxSize.x + nodeDiameter)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.x + mapSize.x - roomMaxSize.x - nodeDiameter)/nodeDiameter)) * nodeDiameter;
		int roomYPos = (int)worldBottomLeft.y + currentFloor * floorHeight;
		int roomZPos = UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.z + roomMaxSize.z + nodeDiameter)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.z + mapSize.z - roomMaxSize.z - nodeDiameter)/nodeDiameter)) * nodeDiameter;
	
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
	
	private Vector3Int GetRandomRoomSize()
	{
		//picking random room sizes
		int roomWidth = UnityEngine.Random.Range(roomMinSize.x/nodeDiameter, roomMaxSize.x /nodeDiameter + 1) * nodeDiameter;
		int roomHeight = UnityEngine.Random.Range(Mathf.Max(roomMinSize.y / nodeDiameter, 1), Mathf.Min(roomMaxSize.y / (nodeDiameter + 1), floorHeight/nodeDiameter)) * nodeDiameter;
		int roomLength = UnityEngine.Random.Range(roomMinSize.z /nodeDiameter, roomMaxSize.z /nodeDiameter + 1) * nodeDiameter;

		//ensure height snaps to grid
		while(roomHeight % nodeDiameter != 0)
			roomHeight++;
			
		Vector3Int roomSize = new Vector3Int(roomWidth, roomHeight, roomLength);	
		return roomSize;
	}
	
	private void CreateRooms()
	{
		for(int floorCount = 0; floorCount < floors; floorCount++)
		{
			int pity = 0;
			for(int roomCount = 0; roomCount < roomsPerFloor; roomCount++)
			{	
				float randomNum = UnityEngine.Random.value;
				
				if((randomNum < chanceToSpawnStairs/100f || pity >= spawnStairGuarenteedPity) && floorCount < floors-1)
				{
					//spawn stair room
					//if width is 2 blocks wide, then we want length to be 1 block wide to determine direction of stair room
					float randomDirection = UnityEngine.Random.Range(1, 3);
					int roomWidth = Mathf.RoundToInt(randomDirection * nodeDiameter);
					int roomHeight = Mathf.RoundToInt(2 * nodeDiameter);
					int roomLength = randomDirection == 1 ? Mathf.RoundToInt(2*nodeDiameter) : Mathf.RoundToInt(nodeDiameter);
					
					Vector3Int roomPos = GetRandomRoomPosition(floorCount);
				
					Vector3Int roomSize = new Vector3Int(roomWidth, roomHeight, roomLength);

					Room newRoom = new Room(new BoundsInt(roomPos, roomSize), true);

					rooms.Add(newRoom);
					pity = 0;
					
				}
				else
				{
					//spawn normal room
					pity++;
					Vector3Int roomPos = GetRandomRoomPosition(floorCount);
					Vector3Int roomSize = GetRandomRoomSize();	
				
					Room newRoom = new Room(new BoundsInt(roomPos, roomSize), false);
			
					rooms.Add(newRoom); 
				}
				
				SpaceRooms(rooms);
			}
		}
	}
	
	
	private void MarkRoomsInGrid(List<Room> rooms)
	{
		//marking the rooms in the grid
		foreach(Room room in rooms)
		{
			for(int x = 0; x < room.bounds.size.x/nodeDiameter; x++)
			{
				for(int y = 0; y < room.bounds.size.y/nodeDiameter; y++)
				{
					for(int z = 0; z < room.bounds.size.z/nodeDiameter; z++)
					{
						Vector3 nodePos = new Vector3(room.bounds.min.x + (x * nodeDiameter + nodeRadius), room.bounds.min.y + (y * nodeDiameter + nodeRadius), room.bounds.min.z + (z * nodeDiameter + nodeRadius));
						//Check if the room is a stair room	
						if (room.isStairs)
						{
							//Check if the current node position corresponds to a top or bottom corner
							bool isBottomCorner = x == 0 && z == 0 && y == 0; 
							bool isTopCorner = x == room.bounds.size.x / nodeDiameter - 1 &&
										y == room.bounds.size.y / nodeDiameter - 1 &&
										z == room.bounds.size.z / nodeDiameter - 1;

							if (isBottomCorner)
							{
								Vector3 bottomDoorPos;
								
								if(room.bounds.size.x < room.bounds.size.z)
									bottomDoorPos = new Vector3(nodePos.x, nodePos.y, nodePos.z - nodeDiameter);
								else
									bottomDoorPos = new Vector3(nodePos.x - nodeDiameter, nodePos.y, nodePos.z);
									
								grid.SetNodeType(bottomDoorPos, CellType.Door);
						
							}else if(isTopCorner)
							{
								Vector3 topDoorPos;
								
								if(room.bounds.size.x < room.bounds.size.z)
									topDoorPos = new Vector3(nodePos.x, nodePos.y, nodePos.z + nodeDiameter);
								else
									topDoorPos = new Vector3(nodePos.x + nodeDiameter, nodePos.y, nodePos.z);
								
								grid.SetNodeType(topDoorPos, CellType.Door);
				
							}
						}

						//Default to marking the node as part of the room
						if(grid.GetNode(nodePos).cellType != CellType.Door)
						{
							grid.SetNodeType(nodePos, CellType.Room);
						}
					}
				}
			}
			
		}
		
		//Mark a random room edge position as a door
		foreach(Room room in rooms)
		{
			if(!room.isStairs)
			{
				grid.SetNodeType(GenerateRoomDoorNode(room.bounds).pos, CellType.Door);
			}
		}

	}
	
	
	private void SpaceRooms(List<Room> rooms)
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
						
					Room roomA = rooms[currentRoom];
					Room roomB = rooms[otherRoom];

					//if rooms are too close to each other, adjust their positions
					if (IsRoomTooClose(roomA.bounds, roomB.bounds, spaceBetweenRooms))
					{
						allRoomsSeperated = false;
						//get direction of the two rooms
						Vector3 direction = (roomA.bounds.center - roomB.bounds.center).normalized;

						int xMovement = Mathf.RoundToInt(direction.x * nodeDiameter);
						int zMovement = Mathf.RoundToInt(direction.z * nodeDiameter);
						
						//ensure positions snap to grid
						while(xMovement % nodeDiameter != 0)
							xMovement++;
						while(zMovement % nodeDiameter != 0)
							zMovement++;
							
						Vector3Int movement = new Vector3Int(xMovement,0,zMovement);
						
						//Move rooms away from each other
						roomA.bounds.position += movement;
						roomB.bounds.position -= movement; // Move in opposite direction
						
						//clamp positions to make sure rooms stay within bounds
						roomA.bounds.position = new Vector3Int(
							Mathf.Clamp(roomA.bounds.position.x, (int)worldBottomLeft.x + nodeDiameter, (int)(worldBottomLeft.x + mapSize.x - roomA.bounds.size.x - nodeDiameter)),
							roomA.bounds.position.y,
							Mathf.Clamp(roomA.bounds.position.z, (int)worldBottomLeft.z + nodeDiameter, (int)(worldBottomLeft.z + mapSize.z - roomA.bounds.size.z - nodeDiameter))
						);
						roomB.bounds.position = new Vector3Int(
							Mathf.Clamp(roomB.bounds.position.x, (int)worldBottomLeft.x + nodeDiameter, (int)(worldBottomLeft.x + mapSize.x - roomB.bounds.size.x - nodeDiameter)),
							roomB.bounds.position.y,
							Mathf.Clamp(roomB.bounds.position.z, (int)worldBottomLeft.z + nodeDiameter, (int)(worldBottomLeft.z + mapSize.z - roomB.bounds.size.z - nodeDiameter))
						);

						//Update the rooms in the list
						rooms[currentRoom] = roomA;
						rooms[otherRoom] = roomB;
					}
				}
			}
			iterations++;
		}
		if(iterations == maxIteration)
		{
			//Debug.LogWarning("Max iterations reached! Removing overlapped rooms.");
			List<Room> roomsToRemove = new List<Room>();
			for (int currentRoom = 0; currentRoom < rooms.Count; currentRoom++)
			{
				for (int otherRoom = 0; otherRoom < rooms.Count; otherRoom++)
				{
					//Skip if comparing the same room
					if (currentRoom == otherRoom)
						continue;
					
					Room roomA = rooms[currentRoom];			
					Room roomB = rooms[otherRoom];

					if (IsRoomTooClose(roomA.bounds, roomB.bounds, spaceBetweenRooms))
					{
						if(!roomsToRemove.Contains(roomB))
						{
							roomsToRemove.Add(roomB);
						}
					}
				}
			}
			
			foreach(Room room in roomsToRemove)
			{
				rooms.Remove(room);
			}
		}
	}
	
	private bool IsRoomTooClose(BoundsInt roomA, BoundsInt roomB, int offsetSpace)
	{
		//Expand room b by the offset on all sides
		BoundsInt expandedRoomB = new BoundsInt(
			roomB.xMin - offsetSpace,
			roomB.yMin,
			roomB.zMin - offsetSpace,
			roomB.size.x + 2 * offsetSpace,
			roomB.size.y,
			roomB.size.z + 2 * offsetSpace
		);
		
		bool overlapX = roomA.xMin < expandedRoomB.xMax && roomA.xMax > expandedRoomB.xMin;
		bool overlapY =  roomA.yMin < expandedRoomB.yMax && roomA.yMax > expandedRoomB.yMin;
		bool overlapZ = roomA.zMin < expandedRoomB.zMax && roomA.zMax > expandedRoomB.zMin;
		
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
			UnityEngine.Debug.DrawLine(
				new Vector3(edge.vertexU.x, (int)worldBottomLeft.y + currentFloor * floorHeight, edge.vertexU.y),
				new Vector3(edge.vertexV.x, (int)worldBottomLeft.y + currentFloor * floorHeight, edge.vertexV.y),
				Color.blue,
				10f
			);
		}
		
	}
	
	private void PathfindHallways(int currentFloor)
	{
		foreach(Prims_MST.Edge edge in selectedEdges)
		{
			List<Node> path = new List<Node>();

			Vector3 hallwayDoorA = new Vector3(edge.vertexU.x, (int)worldBottomLeft.y + (currentFloor * floorHeight) + nodeRadius, edge.vertexU.y);
			Vector3 hallwayDoorB = new Vector3(edge.vertexV.x, (int)worldBottomLeft.y + (currentFloor * floorHeight)+ nodeRadius, edge.vertexV.y);
			grid.SetNodeType(hallwayDoorA, CellType.Door);
			grid.SetNodeType(hallwayDoorB, CellType.Door);
			
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
	
	private Node GenerateRoomDoorNode(BoundsInt room)
	{
		//check which sides of the room have space 
		List<RoomEdgeDirections> invalidDirections = new List<RoomEdgeDirections>();
		
		if(room.position.x + room.size.x + nodeDiameter >= transform.position.x + mapSize.x/2) //if there is no space on right edge
			invalidDirections.Add(RoomEdgeDirections.Right);
		
		if(room.position.x - room.size.x - nodeDiameter <= transform.position.x - mapSize.x/2) //if there is no space on left edge
			invalidDirections.Add(RoomEdgeDirections.Left);
		
		if(room.position.z + room.size.z + nodeDiameter >= transform.position.z + mapSize.z/2) //if there is no space on top edge
			invalidDirections.Add(RoomEdgeDirections.Top);
			
		if(room.position.z - room.size.z - nodeDiameter <= transform.position.z - mapSize.z/2) //if there is no space on bottom edge
			invalidDirections.Add(RoomEdgeDirections.Bottom);
		
		if(GetRandomDirectionExlcuding(out RoomEdgeDirections randomDirection, invalidDirections.ToArray()))
		{
			switch(randomDirection)
			{
				case RoomEdgeDirections.Top:
					return grid.GetNode(new Vector3(room.min.x + (UnityEngine.Random.Range(1, (room.size.x/nodeDiameter)-2) * nodeDiameter + nodeRadius), room.min.y + nodeRadius, room.max.z + nodeRadius));
				case RoomEdgeDirections.Bottom:		
					return grid.GetNode(new Vector3(room.min.x + (UnityEngine.Random.Range(1, (room.size.x/nodeDiameter)-2) * nodeDiameter + nodeRadius), room.min.y + nodeRadius, room.min.z - nodeRadius));
				case RoomEdgeDirections.Left:
					return grid.GetNode(new Vector3(room.min.x - nodeRadius, room.min.y + nodeRadius, room.min.z + (UnityEngine.Random.Range(1, (room.size.z/nodeDiameter)-2) * nodeDiameter + nodeRadius)));
				case RoomEdgeDirections.Right:
					return grid.GetNode(new Vector3(room.max.x + nodeRadius, room.min.y + nodeRadius, room.min.z + (UnityEngine.Random.Range(1, (room.size.z/nodeDiameter)-2) * nodeDiameter + nodeRadius)));
				default:
					break;
			}
		}
		return null;
	}
	
	private bool GetRandomDirectionExlcuding(out RoomEdgeDirections randomDirection, RoomEdgeDirections[] exclude)
	{
		RoomEdgeDirections[] values = Enum.GetValues(typeof(RoomEdgeDirections))
			.Cast<RoomEdgeDirections>()
			.Where(d => !exclude.Contains(d))
			.ToArray();
			
		if(values.Length == 0)
		{
			randomDirection = default;
			return false;
		}
		randomDirection = values[UnityEngine.Random.Range(0, values.Length)];
		return true;
	}

	private void SpawnRoom(BoundsInt room)
	{
		Vector3 ceilingPosition = new Vector3(room.xMin + room.size.x / 2f, room.yMin + room.size.y, room.zMin + room.size.z / 2f);
		Vector3 floorPosition = new Vector3(room.xMin + room.size.x / 2f, room.yMin, room.zMin + room.size.z / 2f);
		
		GameObject ceiling = Instantiate(roomCeilingPrefab, ceilingPosition, Quaternion.identity);
		GameObject floor = Instantiate(roomFloorPrefab, floorPosition, Quaternion.identity);
		
		
		ceiling.GetComponent<Transform>().localScale = new Vector3(room.size.x, ceilingThickness, room.size.z);
		floor.GetComponent<Transform>().localScale = new Vector3(room.size.x, floorThickness, room.size.z);
		
		for(int x = 0; x < room.size.x/nodeDiameter; x++)
		{
			for(int y = 0; y < room.size.y/nodeDiameter; y++)
			{
				for(int z = 0; z < room.size.z/nodeDiameter; z++)
				{
					Vector3 nodePos = new Vector3(room.min.x + (x * nodeDiameter + nodeRadius), room.min.y + (y * nodeDiameter + nodeRadius), room.min.z + (z * nodeDiameter + nodeRadius));
					if(x == 0)
					{
						//checking z nodes on the left 
						Vector3 wallPosition = new Vector3(nodePos.x - nodeRadius - wallThickness / 2f, nodePos.y, nodePos.z);
						GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity);
						wall.GetComponent<Transform>().localScale = new Vector3(wallThickness, nodeDiameter, nodeDiameter);
					}
					if(z == 0){
						//checking x nodes on the bottom
						Vector3 wallPosition = new Vector3(nodePos.x, nodePos.y, nodePos.z - nodeRadius - wallThickness / 2f);
						GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity);
						wall.GetComponent<Transform>().localScale = new Vector3(nodeDiameter, nodeDiameter, wallThickness);
					}
					if(x == room.size.x/nodeDiameter-1){
						//checking x nodes on the right
						Vector3 wallPosition = new Vector3(nodePos.x + nodeRadius + wallThickness / 2f, nodePos.y, nodePos.z);
						GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity);
						wall.GetComponent<Transform>().localScale = new Vector3(wallThickness, nodeDiameter, nodeDiameter);
					}
					if(z == room.size.z/nodeDiameter-1){
						//checking x nodes on top
						Vector3 wallPosition = new Vector3(nodePos.x, nodePos.y, nodePos.z + nodeRadius + wallThickness / 2f);
						GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity);
						wall.GetComponent<Transform>().localScale = new Vector3(nodeDiameter, nodeDiameter, wallThickness);
					}
					
					
				}
			}
		}
		
	}
	
	private void SpawnHallways(Node node, GameObject prefab)
	{
		GameObject newRoomObject = Instantiate(prefab, node.pos, Quaternion.identity);
		newRoomObject.GetComponent<Transform>().localScale = new Vector3(nodeDiameter, nodeDiameter, nodeDiameter);
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
