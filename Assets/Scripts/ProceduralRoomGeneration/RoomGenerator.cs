using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Diagnostics;

public enum CellType
{
	None,
	Room,
	Hallway,
	Stairs,
}

public class RoomGenerator : MonoBehaviour
{	

	[Header("Map Properties")]
	[SerializeField] private Vector2Int mapSize;
	private Vector3 worldBottomLeft;
	[SerializeField] private int mapPadding;
	
	[Header("Grid Properties")]
	[SerializeField] private float nodeRadius;
	private float nodeDiameter;
	[SerializeField] private Grid2D grid;
	[Header("Room Properties")]
	[SerializeField] private int roomCount;
	[SerializeField] private Vector2Int roomMaxSize, roomMinSize;
	[SerializeField] private GameObject roomPrefab;
	
	[Header("Spawn Room Algorithm")]
	[SerializeField] private int spaceBetweenRooms;
	[SerializeField] private List<RectInt> rooms;
	[SerializeField] private int maxIteration = 30;

	
	private DelaunayTriangulation delaunay;
	private Prims_MST prims;
	private HashSet<Prims_MST.Edge> selectedEdges;
	[Header("Create Path Algorithm (MST/Prims)")]
	[SerializeField] private float spawnCycleChance = 0.125f;
	
	[Header("Astar pathfinding")]
	private HashSet<Node> hallways;
	private AStarPathfinder aStarPathFinder;

	[Header("Gizmos Draw")]
	public Color color = Color.green;
	
	private void Start()
	{
		mapPadding = roomMaxSize.x < roomMaxSize.y ? roomMaxSize.y : roomMaxSize.x;
		nodeDiameter = nodeRadius*2;
		spaceBetweenRooms = (int)(Mathf.RoundToInt(spaceBetweenRooms / nodeDiameter) * nodeDiameter);
		worldBottomLeft = transform.position - Vector3.right * mapSize.x/2 - Vector3.forward * mapSize.y/2;
		grid = new Grid2D(mapSize, nodeRadius, transform.position);
		Generate();
	}
	
	private void Generate()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		
		
		grid.CreateGrid();
		rooms = new List<RectInt>();
		hallways  = new HashSet<Node>();
		aStarPathFinder = new AStarPathfinder(grid);
		
		PlaceRooms();
		Triangulate();
		CreatePaths();
		CreateHallways();
		
		sw.Stop();
		UnityEngine.Debug.Log("Finished Generating in " + sw.ElapsedMilliseconds + "ms");
	}
	
	
	private void PlaceRooms()
	{
		for(int i = 0; i < roomCount; i++)
		{		
			int randomXPos = (int)(Random.Range(Mathf.RoundToInt((worldBottomLeft.x + mapPadding)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.x + mapSize.x - mapPadding)/nodeDiameter)) * nodeDiameter);
			int randomYPos = (int)(Random.Range(Mathf.RoundToInt((worldBottomLeft.z + mapPadding)/nodeDiameter), Mathf.RoundToInt((worldBottomLeft.z + mapSize.y - mapPadding)/nodeDiameter)) * nodeDiameter);

			int randomWidth = (int)(Random.Range(roomMinSize.x/(int)nodeDiameter, roomMaxSize.x /(int)nodeDiameter + 1) * nodeDiameter);
			int randomHeight = (int)(Random.Range(roomMinSize.y /(int)nodeDiameter, roomMaxSize.y /(int)nodeDiameter + 1) * nodeDiameter);

			Vector2Int roomPos = new Vector2Int(randomXPos, randomYPos);
			Vector2Int roomSize = new Vector2Int(randomWidth, randomHeight);

			RectInt newRoom = new RectInt(roomPos, roomSize);
			rooms.Add(newRoom);	
			SpaceRooms();
		}
		
		
		foreach(RectInt room in rooms)
		{
			SpawnRoom(room, roomPrefab);
			for(int x = 0; x < room.width/nodeDiameter; x++)
			{
				for(int y = 0; y < room.height/nodeDiameter; y++)
				{
					grid.SetNodeType(new Vector3(room.min.x + (x * nodeDiameter + nodeRadius), 0, room.min.y + (y * nodeDiameter + nodeRadius)), CellType.Room);
				}
			}
		}
	}
	
	private bool IsRoomTooClose(RectInt roomA, RectInt roomB, int offsetSpace)
	{
		// Expand room b by the offset on all sides
		RectInt expandedRoomB = new RectInt(
			roomB.x - offsetSpace,
			roomB.y - offsetSpace,
			roomB.width + 2 * offsetSpace,
			roomB.height + 2 * offsetSpace
		);
		return !((roomA.position.x >= (expandedRoomB.position.x + expandedRoomB.size.x)) || 
		 		((roomA.position.x + roomA.size.x) <= expandedRoomB.position.x) || 
				(roomA.position.y >= (expandedRoomB.position.y + expandedRoomB.size.y)) || 
				((roomA.position.y + roomA.size.y) <= expandedRoomB.position.y));
	}
	
	private void SpaceRooms()
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
					
					//Skip if comparing the same room
					if (currentRoom == otherRoom)
						continue;

					RectInt roomA = rooms[currentRoom];
					RectInt roomB = rooms[otherRoom];

					//If rooms are too close to each other, adjust their positions
					if (IsRoomTooClose(roomA, roomB, spaceBetweenRooms))
					{
						allRoomsSeperated = false;
						//get direction of the two rooms
						Vector2 direction = (roomA.center - roomB.center).normalized;
						//Move rooms away from each other
						
						roomA.position += new Vector2Int((int)(Mathf.RoundToInt(direction.x) * nodeDiameter), (int)(Mathf.RoundToInt(direction.y) * nodeDiameter));
						roomB.position -= new Vector2Int((int)(Mathf.RoundToInt(direction.x) * nodeDiameter), (int)(Mathf.RoundToInt(direction.y) * nodeDiameter)); // Move in opposite direction
						
						//clamp positions to make sure rooms stay within bounds
						roomA.position = new Vector2Int(Mathf.Clamp(roomA.position.x, (int)worldBottomLeft.x + mapPadding, (int)worldBottomLeft.x + mapSize.x - mapPadding), Mathf.Clamp(roomA.position.y, (int)worldBottomLeft.z + mapPadding, (int)worldBottomLeft.z + mapSize.y - mapPadding));
						roomB.position = new Vector2Int(Mathf.Clamp(roomB.position.x, (int)worldBottomLeft.x + mapPadding, (int)worldBottomLeft.x + mapSize.x - mapPadding), Mathf.Clamp(roomB.position.y, (int)worldBottomLeft.z + mapPadding, (int)worldBottomLeft.z +mapSize.y - mapPadding));

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
			List<RectInt> roomsToRemove = new List<RectInt>();
			for (int currentRoom = 0; currentRoom < rooms.Count; currentRoom++)
			{
				for (int otherRoom = 0; otherRoom < rooms.Count; otherRoom++)
				{
					//Skip if comparing the same room
					if (currentRoom == otherRoom)
						continue;
					
					RectInt roomA = rooms[currentRoom];			
					RectInt roomB = rooms[otherRoom];

					if (IsRoomTooClose(roomA, roomB, spaceBetweenRooms))
					{
						if(!roomsToRemove.Contains(roomB))
						{
							roomsToRemove.Add(roomB);
						}
					}
				}
			}
			foreach(RectInt room in roomsToRemove)
			{
				rooms.Remove(room);
			}
		}
			
	}

	private void Triangulate()
	{
		List<Vector2> roomPoints = new List<Vector2>();
		foreach(RectInt room in rooms)
		{
			roomPoints.Add(room.center);
		}
		delaunay = new DelaunayTriangulation(roomPoints, mapSize);
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
			if(Random.value < spawnCycleChance)
			{
				selectedEdges.Add(edge);
			}
		}
		
		//Debug
		foreach (Prims_MST.Edge edge in selectedEdges)
		{
			UnityEngine.Debug.DrawLine(
				new Vector3(edge.vertexU.x, 1, edge.vertexU.y),
				new Vector3(edge.vertexV.x, 1, edge.vertexV.y),
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
			path = aStarPathFinder.FindPath(new Vector3(edge.vertexU.x, 0, edge.vertexU.y), new Vector3(edge.vertexV.x, 0, edge.vertexV.y));
			
			foreach(Node n in path)
			{
				hallways.Add(n);
			}
			
		}
	}
	
	
	
	private void SpawnRoom(RectInt room, GameObject prefab)
	{
		GameObject newRoomObject = Instantiate(prefab, new Vector3(room.xMin + room.width / 2f, 0, room.yMin + room.height / 2f), Quaternion.identity);
		newRoomObject.GetComponent<Transform>().localScale = new Vector3(room.width, 7, room.height);
	}
	
	private void OnDrawGizmos() {
		

		if(Application.isPlaying)
		{
			if(grid != null)
			{
				foreach(Node n in grid.grid)
				{
					Gizmos.color = color;
				
					if(n.cellType == CellType.Room)
					{
						Gizmos.color = Color.red;
					}
				
					if(hallways != null)
					{
						if(hallways.Contains(n))
						{
							Gizmos.color = Color.blue;
						}
					}
					Gizmos.DrawCube(n.pos, Vector3.one * (grid.nodeDiameter-.1f));
				}
			}
		}
	}
	
}
