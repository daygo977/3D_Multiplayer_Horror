using System.Collections.Generic;
using TMPro;
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

    [Header("Settings")]
    [SerializeField] private string playerNamePrefsKey = "PLAYER_NAME";

    private void Start()
    {
        //Hook up button events
        backButton.onClick.AddListener(OnBackPressed);
        createLobbyButton.onClick.AddListener(OnCreateLobbyPressed);
        refreshButton.onClick.AddListener(OnRefreshPressed);

        //Load saved player name if one exists
        LoadSavedPlayerName();

        //Temp: make fake rooms so  to test  UI layout
        PopulateTestRooms();
    }

    private void OnDestroy()
    {
        //Clean listeners when object is destroyed
        backButton.onClick.RemoveListener(OnBackPressed);
        createLobbyButton.onClick.RemoveListener(OnCreateLobbyPressed);
        refreshButton.onClick.RemoveListener(OnRefreshPressed);
    }

    private void OnBackPressed()
    {
        Debug.Log("Back pressed. Main menu not implemented yet.");
        //Add later:
        //SceneManager.LoadScene("MainMenu");
    }

    private void OnCreateLobbyPressed()
    {
        string playerName = playerNameField.text.Trim();
        string lobbyName = lobbyNameField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            Debug.LogWarning("Lobby name is required.");
            return;
        }

        SavePlayerName(playerName);

        bool hasPassword = !string.IsNullOrWhiteSpace(password);

        Debug.Log($"Create Lobby clicked | Player: {playerName} | Lobby: {lobbyName} | HasPassword: {hasPassword}");

        //Later this will call real lobby manager:
        //await UnityLobbyManager.Instance.CreateLobbyAsync(playerName, lobbyName, password);
    }

    private void OnRefreshPressed()
    {
        Debug.Log("Refresh clicked.");

        //Temp: refresh fake test rooms
        PopulateTestRooms();

        //Later this will call real lobby manager:
        //await UnityLobbyManager.Instance.RefreshLobbiesAsync();
    }

    private void SavePlayerName(string playerName)
    {
        PlayerPrefs.SetString(playerNamePrefsKey, playerName);
        PlayerPrefs.Save();
    }

    private void LoadSavedPlayerName()
    {
        if (PlayerPrefs.HasKey(playerNamePrefsKey))
        {
            playerNameField.text = PlayerPrefs.GetString(playerNamePrefsKey);
        }
    }

    private void PopulateTestRooms()
    {
        ClearLobbyList();

        List<TestRoomData> fakeRooms = new List<TestRoomData>()
        {
            new TestRoomData("HauntedHouse01", 1, 4, false),
            new TestRoomData("BasementRun", 2, 4, true),
            new TestRoomData("NightShift", 3, 4, false),
            new TestRoomData("HospitalEscape", 1, 4, true),
            new TestRoomData("SchoolCorridor", 4, 4, false)
        };

        foreach (TestRoomData room in fakeRooms)
        {
            CreateRoomEntry(room);
        }
    }

    private void ClearLobbyList()
    {
        for (int i = 0; i < lobbyListContent.childCount; i++)
        {
            Destroy(lobbyListContent.GetChild(i).gameObject);
        }
    }

    private void CreateRoomEntry(TestRoomData roomData)
    {
        if (roomEntryPrefab == null || lobbyListContent == null)
        {
            Debug.LogWarning("RoomEntryPrefab or LobbyListContent is missing.");
            return;
        }

        GameObject entry = Instantiate(roomEntryPrefab, lobbyListContent);

        LobbyRoomEntryUI entryUI = entry.GetComponent<LobbyRoomEntryUI>();
        if (entryUI != null)
        {
            entryUI.Setup(
                roomData.RoomName,
                roomData.CurrentPlayers,
                roomData.MaxPlayers,
                roomData.IsLocked,
                () => OnJoinRoomPressed(roomData)
            );
        }
        else
        {
            Debug.LogWarning("Room entry prefab is missing LobbyRoomEntryUI.");
        }
    }

    private void OnJoinRoomPressed(TestRoomData roomData)
    {
        Debug.Log($"Join clicked for room: {roomData.RoomName}");

        if (roomData.IsLocked)
        {
            Debug.Log("This room is password protected.");
            //Later can show password popup here
        }

        //Later:
        //await UnityLobbyManager.Instance.JoinLobbyAsync(roomData.LobbyId, enteredPassword);
    }

    private class TestRoomData
    {
        public string RoomName;
        public int CurrentPlayers;
        public int MaxPlayers;
        public bool IsLocked;

        public TestRoomData(string roomName, int currentPlayers, int maxPlayers, bool isLocked)
        {
            RoomName = roomName;
            CurrentPlayers = currentPlayers;
            MaxPlayers = maxPlayers;
            IsLocked = isLocked;
        }
    }
}