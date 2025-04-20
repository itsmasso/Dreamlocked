using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "House Map Difficulty Settings Scriptable Object", menuName = "Scriptable Objects/HouseMapDifficultySettingsSO")]
public class HouseMapDifficultySettingsSO : ScriptableObject
{
    [Tooltip("200x200x200 is recommended")]
    public Vector3Int mapSize;
    public int levelsUntilHarderDifficulty;
    public int floors;
    [Tooltip("Lower number means more lights, higher number means less lights")]
    public int hallwayLightSpawnSpacing; //How many ceiling tiles to build before trying to spawn a light there
    public List<GameObject> specialRoomPrefabsList = new List<GameObject>();
    public List<GameObject> roomPrefabsList = new List<GameObject>();
    [Tooltip("Warning: Too many for a small map size and rooms may begin to overlap!")]
    public int roomsPerFloor;
    public float chanceToSpawnLurker;
    public float lurkerSpawnDelay;
    public int lurkerSpawnAmount;
    public int mannequinSpawnAmount;
    public float chanceToSpawnMannequin;
    public float chanceToSpawnGFClock;
    
    
}
