using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class HouseMapRoomGenerator : NetworkBehaviour
{
    [SerializeField] private HouseMapGenerator houseMapGenerator;
    [SerializeField] private GameObject roomLightPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private List<NetworkObject> doorsInRoom = new List<NetworkObject>();
    [SerializeField] private List<NetworkObject> lightsInRoom = new List<NetworkObject>();
    [SerializeField] private List<NetworkObject> interactableObjects = new List<NetworkObject>();
  
    void Start()
    {

    }

    public void SpawnRoomObjects()
    {
        //spawning lights inside room
        if (IsServer)
        {
            foreach (GameObject roomObj in houseMapGenerator.rooms)
            {
                Room room = roomObj.GetComponent<Room>();
                TrySpawnLights(room);
                SpawnRoomDoors(room);
                TrySpawnInteractableObjectInRoom(room);
            }
        }
    }

    private void TrySpawnLights(Room room)
    {
        if (room.lightsTransforms.Count != 0)
        {
            foreach (Transform lights in room.lightsTransforms)
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
        if (room.doorTransforms.Count != 0)
        {
            foreach (Transform doors in room.doorTransforms)
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
        
        foreach (NetworkObject interactableObj in interactableObjects)
        {
            if (interactableObj.IsSpawned)
            {
                // Despawn the networked object
                interactableObj.Despawn();

                // Destroy the local game object
                Destroy(interactableObj.gameObject);
            }
        }
        // Clear the lists of networked objects
        //doorsInRoom.Clear();
        //lightsInRoom.Clear();
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
        if ((room.interactableObjectPrefabs.Count > 0) && (room.interactableObjectTransforms.Count == room.interactableObjectPrefabs.Count))
        {
            //Debug.Log("Detected Items to Spawn");
            for (int i = 0; i < room.interactableObjectTransforms.Count; i++)
            {
                Transform spawnTransform = room.interactableObjectTransforms[i];
                GameObject prefab = room.interactableObjectPrefabs[i];
                GameObject interactableObject = Instantiate(prefab, spawnTransform.position, spawnTransform.rotation);
                interactableObject.GetComponent<NetworkObject>().Spawn(true);
                interactableObjects.Add(interactableObject.GetComponent<NetworkObject>());
                //Debug.Log("Item Spawned");
            }
        }
    }

}
