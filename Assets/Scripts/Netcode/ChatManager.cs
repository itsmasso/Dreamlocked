using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Steamworks;
using Steamworks.Data;

public class ChatManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField ChatInputField;

    [SerializeField]
    private TextMeshProUGUI ChatText;

    [SerializeField]
    private GameObject ChatPanel;

    void Start()
    {
        ChatText.text = "";   
    }

    void OnEnable()
    {
        SteamMatchmaking.OnChatMessage += ChatSent;
    }

    private void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        AddMessageToBox(msg);
    }

    private void AddMessageToBox(string msg)
    {
        GameObject message = Instantiate(ChatText.gameObject, ChatPanel.transform);
        message.GetComponent<TextMeshProUGUI>().text = msg;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ToggleChatBox();
        }
    }

    private void ToggleChatBox()
    {
        if(ChatInputField.gameObject.activeSelf)
        {
            if(!String.IsNullOrEmpty(ChatInputField.text))
            {
                LobbySaver.instance.currentLobby?.SendChatString(ChatInputField.text);
                ChatInputField.text = "";
            }
            
            ChatInputField.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            ChatInputField.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(ChatInputField.gameObject);
        }
    }
}