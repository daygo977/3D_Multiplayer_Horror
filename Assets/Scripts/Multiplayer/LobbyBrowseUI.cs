using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBrowseUI : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_InputField passwordField;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;

    [Header("Lobby List")]
    [SerializeField] private Transform lobbyListContent;
    [SerializeField] private GameObject roomEntryPrefab;

    [Header("Password Join Popup")]
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private TMP_InputField inputPasswordField;
    [SerializeField] private Button enterPasswordButton;
    [SerializeField] private Button cancelPasswordButton;

    [Header("Settings")]
    [SerializeField] private string playerNamePrefsKey = "PLAYER_NAME";

    private Lobby pendingLobbyToJoin;

    private async void Start()
    {
        //button listeners
        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(OnCreateLobbyPressed);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshPressed);

        if (enterPasswordButton != null)
            enterPasswordButton.onClick.AddListener(OnEnterPasswordPressed);

        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.AddListener(OnCancelPasswordPressed);

        //popup starts hidden
        if (passwordPanel != null)
            passwordPanel.SetActive(false);

        //restore saved player name
        LoadSavedPlayerName();

        //subscribe to real lobby list updates
        if (UnityLobbyManager.Instance != null)
        {
            UnityLobbyManager.Instance.OnAvailableLobbiesChanged += RebuildLobbyList;
            await UnityLobbyManager.Instance.RefreshAvailableLobbiesAsync();
        }
        else
        {
            Debug.LogWarning("UnityLobbyManager not found");
        }
    }

    private void OnDestroy()
    {
        //remove button listeners
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackPressed);

        if (createLobbyButton != null)
            createLobbyButton.onClick.RemoveListener(OnCreateLobbyPressed);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshPressed);

        if (enterPasswordButton != null)
            enterPasswordButton.onClick.RemoveListener(OnEnterPasswordPressed);

        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.RemoveListener(OnCancelPasswordPressed);

        //unsubscribe from lobby updates
        if (UnityLobbyManager.Instance != null)
            UnityLobbyManager.Instance.OnAvailableLobbiesChanged -= RebuildLobbyList;
    }

    private void OnBackPressed()
    {
        //placeholder until main menu exists
        Debug.Log("Back pressed");
    }

    private async void OnCreateLobbyPressed()
    {
        //read inputs
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";
        string lobbyName = lobbyNameField != null ? lobbyNameField.text.Trim() : "";
        string password = passwordField != null ? passwordField.text.Trim() : "";

        //basic validation
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            Debug.LogWarning("Lobby name is required");
            return;
        }

        //save local name
        SavePlayerName(playerName);

        //create real lobby
        if (UnityLobbyManager.Instance != null)
            await UnityLobbyManager.Instance.CreateLobbyAsync(playerName, lobbyName, password);
    }

    private async void OnRefreshPressed()
    {
        //refresh real lobby list
        if (UnityLobbyManager.Instance != null)
            await UnityLobbyManager.Instance.RefreshAvailableLobbiesAsync();
    }

    private async void OnEnterPasswordPressed()
    {
        //guard pending lobby
        if (pendingLobbyToJoin == null)
        {
            ClosePasswordPanel();
            return;
        }

        //read name and entered password
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";
        string enteredPassword = inputPasswordField != null ? inputPasswordField.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required before joining");
            return;
        }

        SavePlayerName(playerName);

        //try join protected lobby
        if (UnityLobbyManager.Instance != null)
        {
            bool joined = await UnityLobbyManager.Instance.JoinLobbyAsync(
                pendingLobbyToJoin.Id,
                playerName,
                enteredPassword
            );

            if (joined)
                ClosePasswordPanel();
        }
    }

    private void OnCancelPasswordPressed()
    {
        //close password popup
        ClosePasswordPanel();
    }

    private void OpenPasswordPanel(Lobby lobby)
    {
        //set pending lobby
        pendingLobbyToJoin = lobby;

        //clear old password
        if (inputPasswordField != null)
            inputPasswordField.text = "";

        //show popup
        if (passwordPanel != null)
            passwordPanel.SetActive(true);
    }

    private void ClosePasswordPanel()
    {
        //clear pending lobby
        pendingLobbyToJoin = null;

        //clear input
        if (inputPasswordField != null)
            inputPasswordField.text = "";

        //hide popup
        if (passwordPanel != null)
            passwordPanel.SetActive(false);
    }

    private void SavePlayerName(string playerName)
    {
        //save local name
        PlayerPrefs.SetString(playerNamePrefsKey, playerName);
        PlayerPrefs.Save();
    }

    private void LoadSavedPlayerName()
    {
        //load local name
        if (playerNameField != null && PlayerPrefs.HasKey(playerNamePrefsKey))
            playerNameField.text = PlayerPrefs.GetString(playerNamePrefsKey);
    }

    private void RebuildLobbyList(List<Lobby> lobbies)
    {
        //clear old rows
        ClearLobbyList();

        if (lobbies == null)
            return;

        //build rows from real lobby data
        foreach (Lobby lobby in lobbies)
        {
            CreateRoomEntry(lobby);
        }
    }

    private void ClearLobbyList()
    {
        //guard content ref
        if (lobbyListContent == null)
        {
            Debug.LogWarning("LobbyListContent is missing");
            return;
        }

        //destroy old entries
        for (int i = lobbyListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(lobbyListContent.GetChild(i).gameObject);
        }
    }

    private void CreateRoomEntry(Lobby lobby)
    {
        //guard refs
        if (roomEntryPrefab == null || lobbyListContent == null)
        {
            Debug.LogWarning("RoomEntryPrefab or LobbyListContent is missing");
            return;
        }

        //spawn row
        GameObject entry = Instantiate(roomEntryPrefab, lobbyListContent);

        //assign room name and click action
        LobbyRoomEntryUI entryUI = entry.GetComponent<LobbyRoomEntryUI>();
        if (entryUI != null)
        {
            entryUI.Setup(lobby.Name, () => OnJoinRoomPressed(lobby));
        }
        else
        {
            Debug.LogWarning("Room entry prefab is missing LobbyRoomEntryUI");
        }
    }

    private async void OnJoinRoomPressed(Lobby lobby)
    {
        //read player name
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required before joining");
            return;
        }

        SavePlayerName(playerName);

        //guard manager
        if (UnityLobbyManager.Instance == null)
            return;

        //open popup for protected lobbies
        if (UnityLobbyManager.Instance.LobbyRequiresPassword(lobby))
        {
            OpenPasswordPanel(lobby);
            return;
        }

        //join open lobby
        await UnityLobbyManager.Instance.JoinLobbyAsync(lobby.Id, playerName);
    }
}