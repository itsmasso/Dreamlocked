using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelScriptableObject", menuName = "Scriptable Objects/LevelScriptableObject")]
public class LevelScriptableObject : ScriptableObject
{
    public Vector3 mapSize;
    public int floors;
    public List<GameObject> specialRoomPrefabsList = new List<GameObject>();
    public List<GameObject> roomPrefabsList = new List<GameObject>();
    public int roomsPerFloor;
    public List<GameObject> monsterPrefabs = new List<GameObject>();
    
    
}
