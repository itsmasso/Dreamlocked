using Steamworks.Data;

using UnityEngine;

public class LobbySaver : MonoBehaviour
{
    public Lobby? currentLobby;
    public static LobbySaver instance;
    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}