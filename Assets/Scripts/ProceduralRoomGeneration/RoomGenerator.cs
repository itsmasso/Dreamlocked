using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
public enum CellType
{
	None,
	Room,
	Hallway,
	Stairs,
}
public class GridPoint
{
	public Vector2Int pos;
	public CellType cellType;
	public GridPoint(Vector2Int pos, CellType cellType)
	{
		this.pos = pos;
		this.cellType = cellType;
	}
}


public class RoomGenerator : MonoBehaviour
{	
	[Header("Map Properties")]
	[SerializeField] private Vector2Int mapSize;
	[SerializeField] private List<GridPoint> grid;
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

	[Header("Gizmos Draw")]
	public Color color = Color.green;
	
	private void Start()
	{
		Generate();
	}
	
	private void Generate()
	{
		grid = new List<GridPoint>();
		rooms = new List<RectInt>();
		PlaceRooms();
		Triangulate();
		CreatePaths();
	}
	
	private void PlaceRooms()
	{
		for(int i = 0; i < roomCount; i++)
		{
			Vector2Int roomPos = new Vector2Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
			Vector2Int roomSize = new Vector2Int(Random.Range(roomMinSize.x, roomMaxSize.x + 1), Random.Range(roomMinSize.y, roomMaxSize.y + 1));

			RectInt newRoom = new RectInt(roomPos, roomSize);
			rooms.Add(newRoom);	
			SpaceRooms();
		}
		
		
		foreach(RectInt room in rooms)
		{
			SpawnRoom(room, roomPrefab);
			foreach(Vector2Int pos in room.allPositionsWithin)
			{
				GridPoint newGridPoint = new GridPoint(pos, CellType.Room);
				grid.Add(newGridPoint);
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
						roomA.position += Vector2Int.RoundToInt(direction);
						roomB.position -= Vector2Int.RoundToInt(direction); // Move in opposite direction

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
			Debug.LogWarning("Max iterations reached! Some rooms may overlap.");
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
			Debug.DrawLine(
				new Vector3(edge.vertexU.x, 1, edge.vertexU.y),
				new Vector3(edge.vertexV.x, 1, edge.vertexV.y),
				Color.blue,
				10f
			);
		}
		
	}
	
	private void SpawnRoom(RectInt room, GameObject prefab)
	{
		GameObject newRoomObject = Instantiate(prefab, new Vector3(room.xMin + room.width / 2f, 0, room.yMin + room.height / 2f), Quaternion.identity);
		newRoomObject.GetComponent<Transform>().localScale = new Vector3(room.width, 7, room.height);
	}
	
	private void OnDrawGizmos() {
		Gizmos.color = color;

		if(Application.isPlaying)
		{
			foreach(RectInt room in rooms)
			{

				Gizmos.color = Color.red;

				Vector3 center = new Vector3(room.xMin + room.width / 2f, 1 / 2f, room.yMin + room.height / 2f);
				Vector3 size = new Vector3(room.width, 1, room.height);

				Gizmos.DrawWireCube(center, size);
			}
		}
	}
	
}
