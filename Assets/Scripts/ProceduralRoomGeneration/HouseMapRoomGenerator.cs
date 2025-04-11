using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class HouseMapRoomGenerator : NetworkBehaviour
{
    [SerializeField] private HouseMapGenerator houseMapGenerator;
    [SerializeField] private GameObject roomLightPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField]private List<NetworkObject> doorsInRoom = new List<NetworkObject>();
    [SerializeField]private List<NetworkObject> lightsInRoom = new List<NetworkObject>();
    void Start()
    {
        
    }

    public void SpawnRoomObjects()
	{
		//spawning lights inside room
		if(IsServer)
		{
			foreach(GameObject roomObj in houseMapGenerator.rooms)
			{
				Room room = roomObj.GetComponent<Room>();
			    TrySpawnLights(room);
			    SpawnRoomDoors(room);
			}
		}
	}
	
	private void TrySpawnLights(Room room)
	{
	    if(room.lightsTransforms.Count != 0)
        {
            foreach(Transform lights in room.lightsTransforms)
            {
                GameObject roomLightObject = Instantiate(roomLightPrefab, lights.position, Quaternion.identity);
                
                roomLightObject.transform.rotation *= Quaternion.Euler(0, room.yRotation, 0);
                roomLightObject.GetComponent<NetworkObject>().Spawn(true);
                lightsInRoom.Add(roomLightObject.GetComponent<NetworkObject>());
            }
        }
	}
	
	private void SpawnRoomDoors(Room room)
	{
	    if(room.doorTransforms.Count != 0)
        {
            foreach(Transform doors in room.doorTransforms)
            {
                GameObject doorObject = Instantiate(doorPrefab, doors.position, doors.transform.rotation);
                
                doorObject.GetComponent<NetworkObject>().Spawn(true);
                doorsInRoom.Add(doorObject.GetComponent<NetworkObject>());
            }
        }
	}
	
public void ClearObjects()
{
    // Despawn doors and lights before reloading the scene
    foreach (NetworkObject door in doorsInRoom)
    {
        if (door.IsSpawned)
        {
            // Despawn the networked object
            door.Despawn();
            
            // Destroy the local game object
            Destroy(door.gameObject);
        }
    }

    foreach (NetworkObject light in lightsInRoom)
    {
        if (light.IsSpawned)
        {
            // Despawn the networked object
            light.Despawn();
            
            // Destroy the local game object
            Destroy(light.gameObject);
        }
    }

    // Clear the lists of networked objects
    //doorsInRoom.Clear();
    //lightsInRoom.Clear();
}
	
}
