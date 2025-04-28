using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Steamworks; // Needed for Steam names!

public class LobbyUIManager : MonoBehaviour
{
    [Header("Player Icons")]
    [SerializeField] private List<Image> playerIcons;  // Drag your 4 UI Image objects here
   
    [Header("Player Names")]
    [SerializeField] private List<TextMeshProUGUI> playerNames; // Drag your 4 TextMeshProUGUI objects here!
    private int playerCount = 0;

    public static LobbyUIManager Instance { get; private set; }

   private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HideAllIcons();
    }

    private void HideAllIcons()
    {
        foreach (var icon in playerIcons)
        {
            if (icon != null)
                icon.gameObject.SetActive(false);
        }

        foreach (var name in playerNames)
        {
            if (name != null)
                name.gameObject.SetActive(false);
        }
    }

    public void RefreshPlayerIcons(int memberCount)
    {
        if (playerIcons == null || playerIcons.Count == 0)
            return;

        HideAllIcons(); // hide everything first

        if (LobbySaver.instance.currentLobby == null)
            return;

        var members = LobbySaver.instance.currentLobby.Value.Members.ToList();  // <- Needs ToList()

        for (int i = 0; i < memberCount && i < playerIcons.Count; i++)
        {
            playerIcons[i].gameObject.SetActive(true);

            if (i < members.Count)
            {
                var member = members[i];

                if (playerNames != null && i < playerNames.Count && playerNames[i] != null)
                {
                    playerNames[i].gameObject.SetActive(true);
                    playerNames[i].text = member.Name; // << Set Steam Name here
                }
            }
        }

        playerCount = memberCount;
    }


    public void OnPlayerJoined()
    {
        
        var members = LobbySaver.instance.currentLobby.Value.Members.ToList();

        if (playerCount < playerIcons.Count)
        {
            playerIcons[playerCount].gameObject.SetActive(true);

            if (playerCount < playerNames.Count)
            {
                playerNames[playerCount].gameObject.SetActive(true);

                if (playerCount < members.Count)
                {
                    playerNames[playerCount].text = members[playerCount].Name;
                }
                else
                {
                    playerNames[playerCount].text = "Player";
                }
            }

            playerCount++;
            Debug.Log("[LobbyUIManager] Player joined.");
        }
    }

    public void OnPlayerLeft()
    {
        if (playerCount > 0)
        {
            playerCount--;

            if (playerCount < playerIcons.Count)
                playerIcons[playerCount].gameObject.SetActive(false);

            if (playerCount < playerNames.Count)
                playerNames[playerCount].gameObject.SetActive(false);

            Debug.Log("[LobbyUIManager] Player left.");
        }
    }
}
