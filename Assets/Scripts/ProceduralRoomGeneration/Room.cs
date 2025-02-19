using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
	public Vector3Int position;
	public Vector3Int size;
	public bool isStairs;
	public bool isMainRoom;
	public List<Transform> doorNode;
	
	public GameObject roomPrefab;
	
	public Vector3Int Min => position;
	public Vector3Int Max => position + size;
	
	public Vector3 Center
	{
		get
		{
			return position + size / 2;
		}
	}
	
}
