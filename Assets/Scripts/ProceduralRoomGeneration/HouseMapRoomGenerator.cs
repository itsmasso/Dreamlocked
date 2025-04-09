using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class HouseMapRoomGenerator : NetworkBehaviour
{
    [SerializeField] private HouseMapGenerator houseMapGenerator;
    [SerializeField] private GameObject roomLightPrefab;
    [SerializeField] private GameObject doorPrefab;

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
                // Spawn interactable objects in rooms
                TrySpawnInteractableObjectInRoom(room);
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
            }
        }
	}
	
	private void SpawnRoomDoors(Room room)
	{
	    if(room.doorTransforms.Count != 0)
        {
            foreach(Transform doors in room.doorTransforms)
            {
                GameObject doorObject = Instantiate(doorPrefab, doors.position, Quaternion.identity);
                doorObject.transform.rotation *= Quaternion.Euler(0, room.yRotation, 0);
                doorObject.GetComponent<NetworkObject>().Spawn(true);
            }
        }
	}

/*****************************************************************
 * SpawnInteractableObjectInRoom
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    As of writing this, this function will specifically spawn 
    the radio in the Radio Room, but I think that there is a way
    to improve this function so that it can spawn any interactable
    object in any room. You would just need to add a list of
    interactable objects to the Room.cs file and pull the prefabs
    from there and the locations to spawn them.
 *****************************************************************/
    private void TrySpawnInteractableObjectInRoom(Room room)
    {
        //Debug.Log("Trying to Spawn Items");
        if((room.interactableObjectPrefabs.Count > 0) && (room.interactableObjectTransforms.Count > 0) && (room.interactableObjectTransforms.Count == room.interactableObjectPrefabs.Count))
        {
            Debug.Log("Detected Items to Spawn");
            for (int i = 0; i < room.interactableObjectTransforms.Count; i++)
            {
                Transform spawnTransform = room.interactableObjectTransforms[i];
                GameObject prefab = room.interactableObjectPrefabs[i];
                GameObject interactableObject = Instantiate(prefab, spawnTransform.position, spawnTransform.rotation);
                interactableObject.GetComponent<NetworkObject>().Spawn(true);
                Debug.Log("Item Spawned");
            }
        }
    }
	
}
