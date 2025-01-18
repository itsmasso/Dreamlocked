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
	HallwayDoor,
	StairDoor,
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

	[Header("Map Properties")]
	[SerializeField] private Vector3Int mapSize;
	[SerializeField] private int floorHeight;
	private Vector3 worldBottomLeft;
	[SerializeField] private int floors;
	
	private int currentFloor;
	
	[Header("Grid Properties")]
	[SerializeField] private float nodeRadius;
	private float nodeDiameter;
	[SerializeField] private Grid3D grid;
	[Header("Room Properties")]
	[SerializeField] private int roomCount;
	[SerializeField] private Vector3Int roomMaxSize, roomMinSize;
	[SerializeField] private GameObject roomPrefab;
	
	[Header("Spawn Room Algorithm")]
	[SerializeField] private int spaceBetweenRooms;
	[SerializeField] private List<BoundsInt> rooms;
	[SerializeField] private int maxIteration = 30;

	
	private DelaunayTriangulation delaunay;
	private Prims_MST prims;
	private HashSet<Prims_MST.Edge> selectedEdges;
	[Header("Create Path Algorithm (MST/Prims)")]
	[SerializeField] private float spawnCycleChance = 0.125f;
	
	[Header("Create Hallways (A*)")]
	private HashSet<Node> hallways;
	private HashSet<Node> hallwayDoors;
	private AStarPathfinder hallwayPathFinder;
	
	[Header("Stairs")]
	[SerializeField] private int stairsPerFloor;

	[Header("Debug")]
	public Color color = new Color(1, 0, 0, 0.1f);
	[SerializeField] private bool drawGizmos;
	[SerializeField] private bool drawAllNodes;
	[SerializeField] private GameObject roomPlaceholder, hallwayPlaceholder, doorPlaceholder;
	
	private void Start()
	{
		currentFloor++;
		nodeDiameter = nodeRadius*2;
		mapSize.y = Mathf.RoundToInt(floorHeight * floors*nodeDiameter);
		spaceBetweenRooms = (int)(Mathf.RoundToInt(spaceBetweenRooms / nodeDiameter) * nodeDiameter);
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
		rooms = new List<BoundsInt>();
		hallwayPathFinder = new AStarPathfinder(grid);
		hallways  = new HashSet<Node>();
		
		
		while(currentFloor < floors)
		{
			List<BoundsInt> roomsInFloor = new List<BoundsInt>();
			
			
			PlaceRooms(roomsInFloor);
			Triangulate(roomsInFloor);
			CreatePaths();
			CreateHallways();
			currentFloor++;
		}
		foreach(BoundsInt room in rooms)
		{
			SpawnRoom(room, roomPlaceholder);
		}
		//CreateStairs();
		
		sw.Stop();
		UnityEngine.Debug.Log("Finished Generating in " + sw.ElapsedMilliseconds + "ms");
	}
	
	
	private void PlaceRooms(List<BoundsInt> roomsInFloor)
	{
		
		for(int i = 0; i < roomCount; i++)
		{		
			int randomXPos = (int)(UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.x + roomMaxSize.x)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.x + mapSize.x - roomMaxSize.x)/nodeDiameter)) * nodeDiameter);
			int randomZPos = (int)(UnityEngine.Random.Range(Mathf.RoundToInt((worldBottomLeft.z + roomMaxSize.z)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.z + mapSize.z - roomMaxSize.z)/nodeDiameter)) * nodeDiameter);
			
			//ensure it snaps to grid
			while(randomXPos % nodeDiameter != 0)
				randomXPos++;
				
			while(randomZPos % nodeDiameter != 0)
				randomZPos++;
			
			int randomWidth = (int)(UnityEngine.Random.Range(roomMinSize.x/(int)nodeDiameter, roomMaxSize.x /(int)nodeDiameter + 1) * nodeDiameter);
			int randomHeight = (int)(UnityEngine.Random.Range(roomMinSize.y /(int)nodeDiameter, roomMaxSize.y /(int)nodeDiameter + 1) * nodeDiameter);
			int randomLength = (int)(UnityEngine.Random.Range(roomMinSize.z /(int)nodeDiameter, roomMaxSize.z /(int)nodeDiameter + 1) * nodeDiameter);
			
			if(randomHeight <=0)
				randomHeight = (int)nodeDiameter;
				
			
			int roomYPos = currentFloor * floorHeight;
			
			while(roomYPos % nodeDiameter != 0)
				roomYPos++;
			
			Vector3Int roomPos = new Vector3Int(randomXPos, roomYPos, randomZPos);
			
			Vector3Int roomSize = new Vector3Int(randomWidth, randomHeight, randomLength);

			BoundsInt newRoom = new BoundsInt(roomPos, roomSize);
			
			roomsInFloor.Add(newRoom);
			SpaceRooms(roomsInFloor);
		}
		
		
		
		foreach(BoundsInt room in roomsInFloor)
		{
			rooms.Add(room);
			for(int x = 0; x < room.size.x/nodeDiameter; x++)
			{
				for(int y = 0; y < room.size.y/nodeDiameter; y++)
				{
					for(int z = 0; z < room.size.z/nodeDiameter; z++)
					{
						Vector3 nodePos = new Vector3(room.min.x + (x * nodeDiameter + nodeRadius), room.min.y + (y * nodeDiameter + nodeRadius), room.min.z + (z * nodeDiameter + nodeRadius));
						grid.SetNodeType(nodePos, CellType.Room);
					}
				}
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
		bool overlapZ = roomA.zMin < expandedRoomB.zMax && roomA.zMax > expandedRoomB.zMin;
		
		return overlapX && overlapZ;
	}
	
	private void SpaceRooms(List<BoundsInt> rooms)
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
						
					BoundsInt roomA = rooms[currentRoom];
					BoundsInt roomB = rooms[otherRoom];

					//if rooms are too close to each other, adjust their positions
					if (IsRoomTooClose(roomA, roomB, spaceBetweenRooms))
					{
						allRoomsSeperated = false;
						//get direction of the two rooms
						Vector3 direction = (roomA.center - roomB.center).normalized;
						
						int xMovement = Mathf.RoundToInt(direction.x * nodeDiameter);
						int zMovement = Mathf.RoundToInt(direction.z * nodeDiameter);
						
						//ensure positions snap to grid
						while(xMovement % nodeDiameter != 0)
							xMovement++;
						while(zMovement % nodeDiameter != 0)
							zMovement++;
							
						Vector3Int movement = new Vector3Int(
							xMovement,
							0,
							zMovement
						);
						
						//Move rooms away from each other
						roomA.position += movement;
						roomB.position -= movement; // Move in opposite direction
						
						//clamp positions to make sure rooms stay within bounds
						roomA.position = new Vector3Int(
							Mathf.Clamp(roomA.position.x, (int)worldBottomLeft.x, (int)(worldBottomLeft.x + mapSize.x - roomA.size.x)),
							roomA.position.y,
							Mathf.Clamp(roomA.position.z, (int)worldBottomLeft.z, (int)(worldBottomLeft.z + mapSize.z - roomA.size.z))
						);
						roomB.position = new Vector3Int(
							Mathf.Clamp(roomB.position.x, (int)worldBottomLeft.x, (int)(worldBottomLeft.x + mapSize.x - roomB.size.x)),
							roomB.position.y,
							Mathf.Clamp(roomB.position.z, (int)worldBottomLeft.z, (int)(worldBottomLeft.z + mapSize.z - roomB.size.z))
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
			List<BoundsInt> roomsToRemove = new List<BoundsInt>();
			for (int currentRoom = 0; currentRoom < rooms.Count; currentRoom++)
			{
				for (int otherRoom = 0; otherRoom < rooms.Count; otherRoom++)
				{
					//Skip if comparing the same room
					if (currentRoom == otherRoom)
						continue;
					
					BoundsInt roomA = rooms[currentRoom];			
					BoundsInt roomB = rooms[otherRoom];

					if (IsRoomTooClose(roomA, roomB, spaceBetweenRooms))
					{
						if(!roomsToRemove.Contains(roomB))
						{
							roomsToRemove.Add(roomB);
						}
					}
				}
			}
			foreach(BoundsInt room in roomsToRemove)
			{
				rooms.Remove(room);
			}
		}
			
	}

	private void Triangulate(List<BoundsInt> roomsInFloor)
	{
		List<Vector2> roomPoints = new List<Vector2>();
		foreach(BoundsInt room in roomsInFloor)
		{
			Node roomEdge = GetRandomRoomEdgePosition(room);
			if(roomEdge == null)
				UnityEngine.Debug.Log("room edge is null");
		
			roomPoints.Add(new Vector3(roomEdge.pos.x,roomEdge.pos.z));
		}
		delaunay = new DelaunayTriangulation(roomPoints, new Vector2Int(mapSize.x, mapSize.z));
		delaunay.Triangulation();
	}
	
	private void CreatePaths()
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
				new Vector3(edge.vertexU.x, currentFloor * floorHeight, edge.vertexU.y),
				new Vector3(edge.vertexV.x, currentFloor * floorHeight, edge.vertexV.y),
				Color.blue,
				10f
			);
		}
		
	}
	
	private void CreateHallways()
	{
		foreach(Prims_MST.Edge edge in selectedEdges)
		{
			List<Node> path = new List<Node>();
			Vector3 hallwayDoorA = new Vector3(edge.vertexU.x, (currentFloor * floorHeight)+nodeDiameter, edge.vertexU.y);
			Vector3 hallwayDoorB = new Vector3(edge.vertexV.x, (currentFloor * floorHeight)+nodeDiameter, edge.vertexV.y);
			grid.SetNodeType(hallwayDoorA, CellType.HallwayDoor);
			grid.SetNodeType(hallwayDoorB, CellType.HallwayDoor);
			
			path = hallwayPathFinder.FindPath(hallwayDoorA, hallwayDoorB);
			
			foreach(Node n in path)
			{
				if(n.cellType != CellType.Room && n.cellType != CellType.HallwayDoor){
					grid.SetNodeType(n.pos, CellType.Hallway);
					hallways.Add(n);
				}
	
				
			}
			
		}
	}
	
	private void CreateStairs()
	{
		int stairsPlaced = 0;
		int iterations= 0;
		while(stairsPlaced < stairsPerFloor && iterations < 50)
		{
			iterations++;
			BoundsInt randomRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];
			Node stairNode = GetRandomRoomEdgePosition(randomRoom);
			if(stairNode == null)
				continue;
			stairNode.cellType = CellType.Stairs;
			stairsPlaced++;
			
		}
		if(iterations >= 50)
		{
			UnityEngine.Debug.Log("Placing stairs reached max iterations!");
		}
	}
	
	private Node GetRandomRoomEdgePosition(BoundsInt room)
	{
		
		List<RoomEdgeDirections> invalidDirections = new List<RoomEdgeDirections>();
		//if there is no space on right edge
		if(room.position.x + room.size.x + nodeDiameter >= transform.position.x + mapSize.x/2)
			invalidDirections.Add(RoomEdgeDirections.Right);
		//if there is no space on left edge
		if(room.position.x - room.size.x - nodeDiameter <= transform.position.x - mapSize.x/2)
			invalidDirections.Add(RoomEdgeDirections.Left);
		//if there is no space on top edge
		if(room.position.z + room.size.z + nodeDiameter >= transform.position.z + mapSize.z/2)
			invalidDirections.Add(RoomEdgeDirections.Top);
		//if there is no space on bottom edge
		if(room.position.z - room.size.z - nodeDiameter <= transform.position.z - mapSize.z/2)
			invalidDirections.Add(RoomEdgeDirections.Bottom);
		
		if(GetRandomDirectionExlcuding(out RoomEdgeDirections randomDirection, invalidDirections.ToArray()))
		{
			
			switch(randomDirection)
			{
				case RoomEdgeDirections.Top:
					
					return grid.GetNode(new Vector3(room.min.x + (UnityEngine.Random.Range(0, (room.size.x/nodeDiameter)-1) * nodeDiameter + nodeRadius), 0, room.max.z - nodeRadius));
				case RoomEdgeDirections.Bottom:		
					return grid.GetNode(new Vector3(room.min.x + (UnityEngine.Random.Range(0, (room.size.x/nodeDiameter)-1) * nodeDiameter + nodeRadius), 0, room.min.z + nodeRadius));
				case RoomEdgeDirections.Left:
					
					return grid.GetNode(new Vector3(room.min.x + nodeRadius, 0, room.min.z + (UnityEngine.Random.Range(0, (room.size.z/nodeDiameter)-1) * nodeDiameter + nodeRadius)));
				case RoomEdgeDirections.Right:
					
					return grid.GetNode(new Vector3(room.max.x - nodeRadius, 0, room.min.z + (UnityEngine.Random.Range(0, (room.size.z/nodeDiameter)-1) * nodeDiameter + nodeRadius)));
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

	
	
	private void SpawnRoom(BoundsInt room, GameObject prefab)
	{
		//make sure prefabs of room spawn with no walls and generate walls based on grid cell type
		GameObject newRoomObject = Instantiate(prefab, new Vector3(room.xMin + room.size.x / 2f, room.yMin + room.size.y / 2f, room.zMin + room.size.z / 2f), Quaternion.identity);
		newRoomObject.GetComponent<Transform>().localScale = new Vector3(room.size.x, room.size.y, room.size.z);
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
					if(n.cellType == CellType.HallwayDoor)
					{
						Gizmos.color = Color.yellow;
					}
					
					
					Gizmos.DrawCube(n.pos, Vector3.one * (grid.nodeDiameter-.1f));
				}
			}
		}
	}
	
}
