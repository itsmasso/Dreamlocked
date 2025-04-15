using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;

public class HouseMapPropPlacer : NetworkBehaviour
{
    [SerializeField] private HouseMapGenerator houseMapGenerator;
    [SerializeField] private GameObject roomLightPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private List<NetworkObject> doorsInRoom = new List<NetworkObject>();
    [SerializeField] private List<NetworkObject> lightsInRoom = new List<NetworkObject>();
    [SerializeField] private List<HouseMapPropScriptableObj> propScriptableObjList = new List<HouseMapPropScriptableObj>();
    [SerializeField] private List<HouseMapPropScriptableObj> deadEndPropsList = new List<HouseMapPropScriptableObj>();
    private List<GameObject> propObjectList = new List<GameObject>();
    [SerializeField] private GameObject hallwayLightPrefab;
    private List<NetworkObject> hallwayLights = new List<NetworkObject>();
    [SerializeField] private float propSpawnInterval;
    [SerializeField] private float hallwayLightSpawnInterval;
    [SerializeField] private float chanceToSpawnHallwayProp = 0.5f;
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
                TrySpawnRoomLights(room);
                SpawnRoomDoors(room);
                TrySpawnInteractableObjectInRoom(room);
            }
        }
    }

    private void TrySpawnRoomLights(Room room)
    {
        if (room.lightsTransforms.Count != 0)
        {
            foreach (Transform lights in room.lightsTransforms)
            {
                Debug.Log("spawning light");
                GameObject roomLightObject = Instantiate(roomLightPrefab, lights.position, Quaternion.identity);

                roomLightObject.transform.rotation *= Quaternion.Euler(0, room.yRotation, 0);
                roomLightObject.GetComponent<NetworkObject>().Spawn(true);
                lightsInRoom.Add(roomLightObject.GetComponent<NetworkObject>());
            }
        }
    }

    public void TrySpawnHallwayLight(int hallwaySpawnIndex, Vector3 position)
    {
        if (IsServer)
        {
            if (hallwaySpawnIndex % hallwayLightSpawnInterval == 0)
            {
                Debug.Log("spawning light");
                GameObject roomLightObject = Instantiate(hallwayLightPrefab, position, Quaternion.identity);
                roomLightObject.GetComponent<NetworkObject>().Spawn(true);
                hallwayLights.Add(roomLightObject.GetComponent<NetworkObject>());
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
                doorObject.GetComponent<Door>().isLocked = room.roomLocked;
                doorsInRoom.Add(doorObject.GetComponent<NetworkObject>());
            }
        }
    }

    public void SpawnDoorProp(Vector3 position, Quaternion rotation)
    {
        HouseMapPropScriptableObj propScriptableObj = PickRandomProp(deadEndPropsList);
        GameObject prop = Instantiate(propScriptableObj.prefab, position, rotation);
        if (prop.TryGetComponent(out NetworkObject propNetObj)) propNetObj.Spawn(true);
        propObjectList.Add(prop);
    }

    private HouseMapPropScriptableObj PickRandomProp(List<HouseMapPropScriptableObj> propList)
    {
        // get the total weight of props
        float totalWeight = 0f;
        foreach (HouseMapPropScriptableObj prop in propList)
        {
            totalWeight += prop.weight;
        }

        // pick a random chance value
        float chosenRandomValue = Random.Range(0, totalWeight);
        // keep trying to pick a prop as pity builds up until it guarentees to pick a prop
        float cumulativeWeight = 0f;
        foreach (HouseMapPropScriptableObj prop in propList)
        {
            cumulativeWeight += prop.weight;
            if (chosenRandomValue <= cumulativeWeight)
            {
                return prop;
            }
        }
        Debug.LogWarning("Failed to pick a prop by weights! Spawning a random prop without weights.");
        return propList[Random.Range(0, propList.Count)];
    }

    public void TrySpawnProp(int hallwaySpawnIndex, Vector3 position, Quaternion rotation, Hallway hallway)
    {
        if (IsServer)
        {
            if (hallwaySpawnIndex % propSpawnInterval == 0)
            {
                float rand = Random.value;
                if(rand < chanceToSpawnHallwayProp)
                {
                    HouseMapPropScriptableObj propScriptableObj = PickRandomProp(propScriptableObjList);
                    GameObject prop = Instantiate(propScriptableObj.prefab, position, rotation);
                    if (prop.TryGetComponent(out NetworkObject propNetObj)) propNetObj.Spawn(true);
                    hallway.spawnedProp = true;
                    propObjectList.Add(prop);
                }
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
                door.Despawn(true);

                // Destroy the local game object
                Destroy(door.gameObject);
            }
        }

        foreach (NetworkObject light in lightsInRoom)
        {
            if (light.IsSpawned)
            {
                // Despawn the networked object
                light.Despawn(true);

                // Destroy the local game object
                Destroy(light.gameObject);
            }
        }

        if (propObjectList.Count != 0)
        {
            foreach (GameObject prop in propObjectList)
            {
                if (prop != null && prop.TryGetComponent(out NetworkObject netObj))
                {
                    if(prop.GetComponent<IHasNetworkChildren>() != null)
                    {
                        prop.GetComponent<IHasNetworkChildren>().DestroyNetworkChildren();
                    }
                    netObj.Despawn(true);
                }
                // Destroy the local game object
                Destroy(prop.gameObject);
            }
        }

        foreach (NetworkObject light in hallwayLights)
        {
            if (light.IsSpawned)
            {
                // Despawn the networked object
                light.Despawn(true);

                // Destroy the local game object
                Destroy(light.gameObject);
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
                propObjectList.Add(interactableObject);
                //Debug.Log("Item Spawned");
            }
        }
    }

}
