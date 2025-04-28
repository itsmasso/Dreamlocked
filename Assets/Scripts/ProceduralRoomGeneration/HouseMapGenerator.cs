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

public class HouseMapGenerator : NetworkBehaviour
{
	private enum RoomEdgeDirections
	{
		Top,
		Bottom,
		Right,
		Left

	}


	[Header("Map Properties")]
	[Tooltip("200x200x200 is recommended")]
	[SerializeField] private Vector3Int mapSize;
	private const int FLOOR_HEIGHT = 4;
	private Vector3 worldBottomLeft;
	[SerializeField] private int floors;
	[SerializeField] private AstarPath aStarComponent;
	public bool isLevelGenerated;
	[SerializeField] private HouseMapDifficultyListSO difficultyListScriptable;
	private HouseMapDifficultySettingsSO currentDifficultySetting;

	[Header("Grid Properties")]
	[Tooltip("2 is recommended")]
	[SerializeField] private float nodeRadius;
	private int nodeDiameter;
	private Grid3D grid;

	[Header("Prefab Components")]
	[SerializeField] private GameObject roomCeilingPrefab;
	[SerializeField] private GameObject roomFloorPrefab;
	[SerializeField] private GameObject defaultWallPrefab;
	[SerializeField] private List<GameObject> altWallPrefabs = new List<GameObject>();
	[SerializeField] private float chanceToSpawnAltWall = 0.2f;
	[SerializeField] private List<GameObject> normalRoomsPrefabList;

	[Header("Special Room Properties")]
	[SerializeField] private List<GameObject> specialRoomPrefabList;
	private int specialRoomIndex = 0;

	[Header("Room Properties")]
	[SerializeField] private int roomsPerFloor;

	[Header("Spawn Room Algorithm")]
	[SerializeField] private int spaceBetweenRooms;
	[SerializeField] public List<GameObject> rooms { get; private set; }
	[SerializeField] private HouseMapPropPlacer propObjectPlacer;
	[SerializeField] private int maxIteration = 30;

	//Delaunay Triangulation
	private DelaunayTriangulation delaunay;

	[Header("Create Path Algorithm (MST/Prims)")]
	private Prims_MST prims;
	private HashSet<Prims_MST.Edge> selectedEdges;
	[SerializeField] private float spawnCycleChance = 0.125f;

	[Header("Create Hallways (A*)")]
	private List<Node> hallways;
	private AStarPathfinder hallwayPathFinder;
	private int currentHallwaySpawnIndex;


	[Header("Stairs")]
	[SerializeField] private List<GameObject> stairRooms;
	[SerializeField] private int chanceToSpawnStairs;
	[SerializeField] private int spawnStairGuarenteedPity;

	[Header("Debug")]
	public Color color = new Color(1, 0, 0, 0.1f);
	[SerializeField] private bool drawGizmos;
	[SerializeField] private bool drawAllNodes;

	private void Awake()
	{

		nodeDiameter = Mathf.RoundToInt(nodeRadius * 2);
		// mapSize.y = Mathf.RoundToInt(FLOOR_HEIGHT * floors);
		spaceBetweenRooms = Mathf.RoundToInt(spaceBetweenRooms / nodeDiameter) * nodeDiameter;
		worldBottomLeft = transform.position - Vector3.right * mapSize.x / 2 - Vector3.up * mapSize.y / 2 - Vector3.forward * mapSize.z / 2;
		GameManager.Instance.onNextLevel += ClearMap;
		GameManager.Instance.onLobby += ClearMap;
	}

	void Start()
	{
		if (IsServer)
		{
			HouseMapDifficultySettingsSO currentDifficultySettingSO = GameManager.Instance.GetLevelLoader().currentHouseMapDifficultySetting;
			AllSetDifficultySORpc(GetDifficultySOIndex(currentDifficultySettingSO));
		}
		SetDifficulty();
		Generate();

	}
	

	private int GetDifficultySOIndex(HouseMapDifficultySettingsSO difficultySetting)
	{
		return difficultyListScriptable.difficultyListSO.IndexOf(difficultySetting);
	}
	[Rpc(SendTo.Everyone)]
	private void AllSetDifficultySORpc(int difficultySOIndex)
	{
		currentDifficultySetting = difficultyListScriptable.difficultyListSO[difficultySOIndex];
	}
	private void SetDifficulty()
	{
		if (currentDifficultySetting != null)
		{
			mapSize = currentDifficultySetting.mapSize;
			floors = currentDifficultySetting.floors;
			foreach (GameObject specialRoom in currentDifficultySetting.specialRoomPrefabsList)
			{
				specialRoomPrefabList.Add(specialRoom);
			}
			foreach (GameObject normalRoom in currentDifficultySetting.roomPrefabsList)
			{
				normalRoomsPrefabList.Add(normalRoom);
			}
			roomsPerFloor = currentDifficultySetting.roomsPerFloor;
			propObjectPlacer.hallwayLightSpawnInterval = currentDifficultySetting.hallwayLightSpawnSpacing;
		}
	}

	public void Generate()
	{

		UnityEngine.Random.InitState(GameManager.Instance.seed.Value);

		Stopwatch sw = new Stopwatch();
		sw.Start();
		isLevelGenerated = false;
		grid = new Grid3D(mapSize, nodeRadius, transform.position);
		grid.CreateGrid();
		rooms = new List<GameObject>();
		hallwayPathFinder = new AStarPathfinder(grid);
		hallways = new List<Node>();
		currentHallwaySpawnIndex = 0;
		specialRoomIndex = 0;

		CreateRooms();
		MarkRoomsInGrid(rooms);
		CreateHallways();

		foreach (Node n in grid.grid)
		{
			if (n.cellType == CellType.Hallway)
				SpawnHallways(n);
			if (n.cellType == CellType.Door)
				SpawnDoorWay(n);
		}

		propObjectPlacer.SpawnRoomObjects();

		aStarComponent.Scan();
		sw.Stop();
		isLevelGenerated = true;
		if (IsServer)
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
		foreach (GameObject room in rooms)
		{
			if (!room.GetComponent<Room>().isSpecialRoom && !room.GetComponent<Room>().isStairs)
			{
				roomPositions.Add(room.transform.position);
			}
		}
		return roomPositions;
	}

	public List<Room> GetNormalRoomComponents()
	{
		List<Room> roomComponents = new List<Room>();

		foreach (GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			if (!room.isSpecialRoom && !room.isStairs)
			{
				roomComponents.Add(room);
			}
		}
		return roomComponents;
	}

	public Vector3 GetPlayerSpawnPosition()
	{
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
		foreach (Node node in hallways)
			hallwayPosList.Add(new Vector3(node.pos.x, node.pos.y - nodeRadius, node.pos.z));
		return hallwayPosList;
	}

	public Room GetRoomFromPosition(Vector3 position)
	{
		foreach (GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			if (room.PositionInBounds(position))
			{
				return room;
			}
		}
		return null;
	}

	public Vector3 GetDoorClosestToTarget(Vector3 targetPosition)
	{
		Room room = GetRoomFromPosition(targetPosition);
		if (room != null)
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
		int roomXPos = UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.x + size.x + nodeDiameter * 2) / nodeDiameter), Mathf.RoundToInt((worldBottomLeft.x + mapSize.x - size.x - nodeDiameter * 2) / nodeDiameter)) * nodeDiameter;
		int roomYPos = (int)worldBottomLeft.y + currentFloor * FLOOR_HEIGHT;
		int roomZPos = UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.z + size.z + nodeDiameter * 2) / nodeDiameter), Mathf.RoundToInt((worldBottomLeft.z + mapSize.z - size.z - nodeDiameter * 2) / nodeDiameter)) * nodeDiameter;

		//ensure positions snap to grid
		while (roomXPos % nodeDiameter != 0)
			roomXPos++;
		while (roomYPos % nodeDiameter != 0 && roomYPos > 0)
			roomYPos++;
		while (roomZPos % nodeDiameter != 0)
			roomZPos++;

		Vector3Int roomPos = new Vector3Int(roomXPos, roomYPos, roomZPos);

		return roomPos;
	}

	private void CreateRooms()
	{
		for (int floorCount = 0; floorCount < floors; floorCount++)
		{
			int pity = 0;
			for (int roomCount = 0; roomCount < roomsPerFloor; roomCount++)
			{
				float randomNum = UnityEngine.Random.value;
				GameObject newRoom;
				if ((randomNum < chanceToSpawnStairs / 100f || pity >= spawnStairGuarenteedPity) && floorCount < floors - 1)
				{
					//spawn stair room
					newRoom = stairRooms[0];
				}
				else
				{
					if (specialRoomIndex < specialRoomPrefabList.Count)
					{
						newRoom = specialRoomPrefabList[specialRoomIndex];
						specialRoomIndex++;
					}
					else
					{
						newRoom = normalRoomsPrefabList[UnityEngine.Random.Range(0, normalRoomsPrefabList.Count)];
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
				if (chosenAngle == 90 || chosenAngle == 270)
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
		foreach (GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			for (int x = 0; x < room.size.x / nodeDiameter; x++)
			{
				for (int y = 0; y < room.size.y / nodeDiameter; y++)
				{
					for (int z = 0; z < room.size.z / nodeDiameter; z++)
					{
						Vector3 nodePos = new Vector3(room.Min.x + (x * nodeDiameter + nodeRadius), room.Min.y + (y * nodeDiameter + nodeRadius), room.Min.z + (z * nodeDiameter + nodeRadius));

						//Default to marking the node as part of the room
						if (grid.GetNode(nodePos).cellType != CellType.Door)
						{
							grid.SetNodeType(nodePos, CellType.Room);
						}
					}
				}
			}

		}

		foreach (GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			foreach (Transform doorNode in room.doorNode)
			{
				grid.SetNodeType(doorNode.position, CellType.Door);
			}
		}

	}


	private void SpaceRooms(List<GameObject> rooms)
	{
		bool allRoomsSeperated = false;
		int iterations = 0;
		while (!allRoomsSeperated && iterations < maxIteration)
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
						while (xMovement % nodeDiameter != 0)
							xMovement++;
						while (zMovement % nodeDiameter != 0)
							zMovement++;

						Vector3Int movement = new Vector3Int(xMovement, 0, zMovement);

						//Move rooms away from each other
						roomA.position += movement;
						roomB.position -= movement; // Move in opposite direction

						//clamp positions to make sure rooms stay within bounds
						roomA.position = new Vector3Int(
							Mathf.Clamp(roomA.position.x, (int)worldBottomLeft.x + nodeDiameter * 2, (int)(worldBottomLeft.x + mapSize.x - roomA.size.x - nodeDiameter * 2)),
							roomA.position.y,
							Mathf.Clamp(roomA.position.z, (int)worldBottomLeft.z + nodeDiameter * 2, (int)(worldBottomLeft.z + mapSize.z - roomA.size.z - nodeDiameter * 2))
						);
						roomB.position = new Vector3Int(
							Mathf.Clamp(roomB.position.x, (int)worldBottomLeft.x + nodeDiameter * 2, (int)(worldBottomLeft.x + mapSize.x - roomB.size.x - nodeDiameter * 2)),
							roomB.position.y,
							Mathf.Clamp(roomB.position.z, (int)worldBottomLeft.z + nodeDiameter * 2, (int)(worldBottomLeft.z + mapSize.z - roomB.size.z - nodeDiameter * 2))
						);

						//Update the rooms in the list
						rooms[currentRoom].GetComponent<Room>().position = roomA.position;
						rooms[otherRoom].GetComponent<Room>().position = roomB.position;
					}
				}
			}
			iterations++;
		}
		//deleting overlapping rooms
		if (iterations == maxIteration)
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
						if (!roomsToRemove.Contains(roomB.gameObject))
						{
							roomsToRemove.Add(roomB.gameObject);
						}
					}
				}
			}

			foreach (GameObject room in roomsToRemove)
			{
				rooms.Remove(room);
			}
		}

		foreach (GameObject roomObj in rooms)
		{
			Room room = roomObj.GetComponent<Room>();
			room.transform.position = new Vector3(room.position.x + room.size.x / 2, room.position.y, room.position.z + room.size.z / 2);
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
		bool overlapY = roomA.Min.y < expandedRoomB.yMax && roomA.Max.y > expandedRoomB.yMin;
		bool overlapZ = roomA.Min.z < expandedRoomB.zMax && roomA.Max.z > expandedRoomB.zMin;

		return overlapX && overlapZ && overlapY;
	}

	private void CreateHallways()
	{
		for (int floorCount = 0; floorCount < floors; floorCount++)
		{
			Triangulate(floorCount);
			CreatePaths(floorCount);
			PathfindHallways(floorCount);
		}
	}

	private void Triangulate(int currentFloor)
	{
		List<Vector2> roomPoints = new List<Vector2>();
		for (int x = 0; x < mapSize.x / nodeDiameter; x++)
		{
			for (int z = 0; z < mapSize.z / nodeDiameter; z++)
			{
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (currentFloor * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);

				if (grid.GetNode(worldPoint).cellType == CellType.Door)
				{
					roomPoints.Add(new Vector2(grid.GetNode(worldPoint).pos.x, grid.GetNode(worldPoint).pos.z));
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
		foreach (DelaunayTriangulation.Edge edge in delaunay.GetEdges())
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
		foreach (Prims_MST.Edge edge in unselectedEdges)
		{
			if (UnityEngine.Random.value < spawnCycleChance)
			{
				selectedEdges.Add(edge);
			}
		}
		//Debug
		foreach (Prims_MST.Edge edge in selectedEdges)
		{
			if (drawGizmos)
			{
				UnityEngine.Debug.DrawLine(
				new Vector3(edge.vertexU.x, (int)worldBottomLeft.y + currentFloor * FLOOR_HEIGHT, edge.vertexU.y),
				new Vector3(edge.vertexV.x, (int)worldBottomLeft.y + currentFloor * FLOOR_HEIGHT, edge.vertexV.y),
				Color.blue,
				10f
			);
			}
		}

	}

	private void PathfindHallways(int currentFloor)
	{
		foreach (Prims_MST.Edge edge in selectedEdges)
		{
			List<Node> path = new List<Node>();

			Vector3 hallwayDoorA = new Vector3(edge.vertexU.x, (int)worldBottomLeft.y + (currentFloor * FLOOR_HEIGHT) + nodeRadius, edge.vertexU.y);
			Vector3 hallwayDoorB = new Vector3(edge.vertexV.x, (int)worldBottomLeft.y + (currentFloor * FLOOR_HEIGHT) + nodeRadius, edge.vertexV.y);

			path = hallwayPathFinder.FindPath(hallwayDoorA, hallwayDoorB, true);

			foreach (Node n in path)
			{
				if (n.cellType != CellType.Room && n.cellType != CellType.Door)
				{
					grid.SetNodeType(n.pos, CellType.Hallway);
					hallways.Add(n);
				}
			}
		}
	}

	private GameObject SpawnRoomPart(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GameObject part = Instantiate(prefab, position, Quaternion.identity);
		part.transform.SetParent(transform);
		part.transform.rotation = rotation;
		return part;
	}
	private void TrySpawnDoorProp(Node node, Vector3 propPosition)
	{
		var offsetsAndRotations = new (Vector3 offset, Quaternion rotation)[]
		{
		(-Vector3.right * nodeDiameter, Quaternion.Euler(0, -90, 0)),
		( Vector3.right * nodeDiameter, Quaternion.Euler(0, 90, 0)),
		( Vector3.forward * nodeDiameter, Quaternion.identity),
		(-Vector3.forward * nodeDiameter, Quaternion.Euler(0, -180, 0))
		};

		foreach (var (offset, rotation) in offsetsAndRotations)
		{
			Node neighbor = grid.GetNode(node.pos + offset);
			if (neighbor == null || neighbor.cellType == CellType.None)
			{
				propObjectPlacer.SpawnDoorProp(propPosition, rotation);
				return;
			}
		}
	}

	private void TrySpawnHallwayProp(Node hallwayNode, Vector3 propPosition, Hallway hallway)
	{
		var horizontalConnections = new List<Quaternion>();
		var verticalConnections = new List<Quaternion>();

		var horizontalDirections = new (Vector3 nodeOffset, Quaternion rotation)[]
		{
		(-Vector3.right * nodeDiameter, Quaternion.Euler(0, -90, 0)),
		( Vector3.right * nodeDiameter, Quaternion.Euler(0, 90, 0))
		};
		var verticalDirections = new (Vector3 nodeOffset, Quaternion rotation)[]
		{
		( Vector3.forward * nodeDiameter, Quaternion.identity),
		(-Vector3.forward * nodeDiameter, Quaternion.Euler(0, -180, 0))
		};

		// Check horizontal neighbors
		foreach (var (offset, rotation) in horizontalDirections)
		{
			Node neighbor = grid.GetNode(hallwayNode.pos + offset);
			if (neighbor != null && (neighbor.cellType == CellType.None || neighbor.cellType == CellType.Room) && neighbor.cellType != CellType.Door)
			{
				horizontalConnections.Add(rotation);
			}
		}

		// Check vertical neighbors
		foreach (var (offset, rotation) in verticalDirections)
		{
			Node neighbor = grid.GetNode(hallwayNode.pos + offset);
			if (neighbor != null && (neighbor.cellType == CellType.None || neighbor.cellType == CellType.Room) && neighbor.cellType != CellType.Door)
			{
				verticalConnections.Add(rotation);
			}
		}

		// Spawn prop only if both ends of a hallway direction are connected
		if (horizontalConnections.Count == 2)
		{
			// Horizontal hallway
			propObjectPlacer.TrySpawnProp(currentHallwaySpawnIndex, propPosition, Quaternion.Euler(0, -90, 0), hallway);
		}
		else if (verticalConnections.Count == 2)
		{
			// Vertical hallway
			propObjectPlacer.TrySpawnProp(currentHallwaySpawnIndex, propPosition, Quaternion.identity, hallway);
		}
	}

	private void TrySpawnHallwayWall(Vector3 neighborNodePos, Vector3 wallPosition, Quaternion wallRotation)
	{
		CellType type = grid.GetNode(neighborNodePos).cellType;
		if (type == CellType.None)
		{
			if (UnityEngine.Random.value <= chanceToSpawnAltWall)
			{
				SpawnRoomPart(altWallPrefabs[UnityEngine.Random.Range(0, altWallPrefabs.Count)], wallPosition, wallRotation);
			}
			else
			{
				SpawnRoomPart(defaultWallPrefab, wallPosition, wallRotation);
			}
		}
	}

	private void SpawnHallways(Node node)
	{
		Vector3 ceilingPosition = new Vector3(node.pos.x, node.pos.y + nodeRadius - 0.2f, node.pos.z);
		Vector3 floorPosition = new Vector3(node.pos.x, node.pos.y - nodeRadius + 0.05f, node.pos.z);

		SpawnRandomCeiling(ceilingPosition);
		GameObject floor = SpawnRoomPart(roomFloorPrefab, floorPosition, Quaternion.identity);

		if (!floor.GetComponent<Hallway>().spawnedProp && IsServer)
		{
			TrySpawnHallwayProp(node, floor.GetComponent<Hallway>().hallwayObjectPosition.position, floor.GetComponent<Hallway>());
		}

		TrySpawnHallwayWall(node.pos - Vector3.right * nodeDiameter, new Vector3(node.pos.x - nodeRadius - 0.05f, node.pos.y + 0.05f, node.pos.z), Quaternion.Euler(0, -90, 0));
		TrySpawnHallwayWall(node.pos + Vector3.right * nodeDiameter, new Vector3(node.pos.x + nodeRadius, node.pos.y + 0.05f, node.pos.z), Quaternion.Euler(0, 90, 0));
		TrySpawnHallwayWall(node.pos + Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y + 0.05f, node.pos.z + nodeRadius), Quaternion.identity);
		TrySpawnHallwayWall(node.pos - Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y + 0.05f, node.pos.z - nodeRadius - 0.05f), Quaternion.Euler(0, -180, 0));
		currentHallwaySpawnIndex++;
	}

	private void SpawnRandomCeiling(Vector3 pos)
	{
		GameObject ceiling = SpawnRoomPart(roomCeilingPrefab, pos, Quaternion.identity);
		propObjectPlacer.TrySpawnHallwayLight(currentHallwaySpawnIndex, ceiling.GetComponent<CeilingPiece>().lightsTransform.position);
	}

	private void SpawnDoorWay(Node node)
	{
		Vector3 ceilingPosition = new Vector3(node.pos.x, node.pos.y + nodeRadius - 0.2f, node.pos.z);
		Vector3 floorPosition = new Vector3(node.pos.x, node.pos.y - nodeRadius + 0.05f, node.pos.z);

		SpawnRoomPart(roomCeilingPrefab, ceilingPosition, Quaternion.identity);
		GameObject floor = SpawnRoomPart(roomFloorPrefab, floorPosition, Quaternion.identity);
		if (IsServer) TrySpawnDoorProp(node, floor.GetComponent<Hallway>().hallwayObjectPosition.position);

		TrySpawnHallwayWall(node.pos - Vector3.right * nodeDiameter, new Vector3(node.pos.x - nodeRadius - 0.05f, node.pos.y + 0.05f, node.pos.z), Quaternion.Euler(0, -90, 0));
		TrySpawnHallwayWall(node.pos + Vector3.right * nodeDiameter, new Vector3(node.pos.x + nodeRadius, node.pos.y + 0.05f, node.pos.z), Quaternion.Euler(0, 90, 0));
		TrySpawnHallwayWall(node.pos + Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y + 0.05f, node.pos.z + nodeRadius), Quaternion.identity);
		TrySpawnHallwayWall(node.pos - Vector3.forward * nodeDiameter, new Vector3(node.pos.x, node.pos.y + 0.05f, node.pos.z - nodeRadius - 0.05f), Quaternion.Euler(0, -180, 0));

	}

	public void ClearMap()
	{
		UnityEngine.Debug.Log("Clearing Map");
		propObjectPlacer.ClearObjects();
		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}

		grid.ClearGrid();
		delaunay.Clear();
		rooms.Clear();
		hallways.Clear();
		specialRoomPrefabList.Clear();
		normalRoomsPrefabList.Clear();
	}

	public override void OnDestroy()
	{
		if (Application.isPlaying)
		{
			base.OnDestroy();
			ClearMap();
			GameManager.Instance.onNextLevel -= ClearMap;
			GameManager.Instance.onLobby -= ClearMap;
		}

	}
	private void OnDrawGizmos()
	{
		if (Application.isPlaying && drawGizmos)
		{
			if (grid != null)
			{

				foreach (Node n in grid.grid)
				{
					Gizmos.color = color;
					if (!drawAllNodes && n.cellType == CellType.None)
					{
						continue;
					}

					if (n.cellType == CellType.Room)
					{
						Gizmos.color = Color.red;
					}

					if (n.cellType == CellType.Hallway)
					{
						Gizmos.color = Color.blue;
					}
					if (n.cellType == CellType.Door)
					{
						Gizmos.color = Color.yellow;
					}


					Gizmos.DrawCube(n.pos, Vector3.one * (grid.nodeDiameter - .1f));
				}
			}
		}
	}

}
