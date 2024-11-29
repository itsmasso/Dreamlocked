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
	public GridPoint(Vector2Int _pos, CellType _cellType)
	{
		pos = _pos;
		cellType = _cellType;
	}
}


public class RoomGenerator : MonoBehaviour
{	
	[SerializeField] private float mapRadius;
	[SerializeField] private List<GridPoint> grid;
	[SerializeField] private int roomCount;
	[SerializeField] private Vector2Int roomMaxSize, roomMinSize;
	
	[SerializeField] private int spaceBetweenRooms;
	[SerializeField] private GameObject roomPrefab;
	[SerializeField] private List<RectInt> rooms;
	[SerializeField] private int maxIteration = 30;

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
	}
	
	private void PlaceRooms()
	{
		for(int i = 0; i < roomCount; i++)
		{
			Vector2 randomPoint = Random.insideUnitCircle * mapRadius;
			Vector2Int roomPos = new Vector2Int(Mathf.RoundToInt(randomPoint.x), Mathf.RoundToInt(randomPoint.y));
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

				Gizmos.color = Color.green;

				Vector3 center = new Vector3(room.xMin + room.width / 2f, 1 / 2f, room.yMin + room.height / 2f);
				Vector3 size = new Vector3(room.width, 1, room.height);

				Gizmos.DrawWireCube(center, size);
			}
		}
	}
	
}
