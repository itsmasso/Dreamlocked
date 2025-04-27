using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Steamworks;
using Steamworks.Data;
using UnityEngine.UI;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField ChatInputField;
    [SerializeField] private TextMeshProUGUI ChatText; // Still here, but not used if you're instantiating prefabs
    [SerializeField] private GameObject ChatPanel;
    [SerializeField] private GameObject ChatMessagePrefab; 
    [SerializeField] private Transform ChatContent; 
    [SerializeField] private ScrollRect ChatScroll;

    private const int MaxMessages = 100;

    void Start()
    {
        ChatText.text = ""; // Not used anymore if you’re using prefabs
    }

    void OnEnable()
    {
        SteamMatchmaking.OnChatMessage += ChatSent;
    }

    void OnDisable()
    {
        SteamMatchmaking.OnChatMessage -= ChatSent;
    }

    private void ChatSent(Lobby lobby, Friend friend, string msg)
    {
        string time = DateTime.Now.ToString("HH:mm");
        Debug.Log($"[ChatManager] Received message from {friend.Name}: {msg}");
        AddMessageToBox($"[{time}] {friend.Name}: {msg}");
    }

    private void AddMessageToBox(string msg)
    {
        Debug.Log($"[ChatManager] Instantiating message: {msg}");

        GameObject message = Instantiate(ChatMessagePrefab, ChatContent);
        var text = message.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            Debug.LogError("ChatMessagePrefab is missing TextMeshProUGUI!");
            return;
        }

        text.text = msg;

        // Limit messages
        if (ChatContent.childCount > MaxMessages)
        {
            Destroy(ChatContent.GetChild(0).gameObject);
        }

        StartCoroutine(ScrollToBottomNextFrame());
    }


    void Update()
    {
        // ENTER while input field is active → send message + stay focused
        if (ChatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            if (!string.IsNullOrWhiteSpace(ChatInputField.text))
            {
                LobbySaver.instance.currentLobby?.SendChatString(ChatInputField.text);
                ChatInputField.text = "";

                // Re-focus after sending to keep typing
                StartCoroutine(RefocusInput());
            }
        }
        // ENTER to open chat input if not visible
        else if (!ChatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            ChatInputField.gameObject.SetActive(true);
            StartCoroutine(RefocusInput());
        }

        // ESC to close input
        if (ChatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ChatInputField.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private IEnumerator RefocusInput()
    {
        yield return null; // wait 1 frame
        EventSystem.current.SetSelectedGameObject(ChatInputField.gameObject);
        ChatInputField.ActivateInputField();
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null; // wait one frame
        Canvas.ForceUpdateCanvases();
        ChatScroll.verticalNormalizedPosition = 0f;
    }

    public void ClearChat()
    {
        foreach (Transform child in ChatContent)
        {
            if (child.name != "ChatMessage") // or tag it instead
            {
                Destroy(child.gameObject);
            }
        }
    }


    private void ToggleChatBox()
    {
        if (ChatInputField.gameObject.activeSelf)
        {
            if (!string.IsNullOrEmpty(ChatInputField.text))
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
