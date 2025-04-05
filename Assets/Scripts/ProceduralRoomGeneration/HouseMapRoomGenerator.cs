using UnityEngine;
using Unity.Netcode;
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
	
}
