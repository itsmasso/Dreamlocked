using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Steamworks;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;



public class CharacterSpawner : NetworkBehaviour
{
    [SerializeField]
    private GameObject Player;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if(IsHost && sceneName == "PlayerScene")
        {
            foreach(ulong id in clientsCompleted)
            {
                GameObject player = Instantiate(Player, GameManager.Instance.GetPlayerSpawnPosition(), Quaternion.identity);
                NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
                playerNetworkObject.SpawnWithOwnership(id, true);
            }
        }
    }

}
