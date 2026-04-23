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
        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(OnCreateLobbyPressed);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshPressed);

        //Load saved player name if one exists
        LoadSavedPlayerName();
        //Temp: make fake rooms so  to test  UI layout
        PopulateTestRooms();
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackPressed);

        if (createLobbyButton != null)
            createLobbyButton.onClick.RemoveListener(OnCreateLobbyPressed);

        if (refreshButton != null)
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
        //Read all input field values
        string playerName = playerNameField != null ? playerNameField.text.Trim() : "";
        string lobbyName = lobbyNameField != null ? lobbyNameField.text.Trim() : "";
        string password = passwordField != null ? passwordField.text.Trim() : "";

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

        //Save player name locally
        SavePlayerName(playerName);
        //If password is set, then track it
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
        if (playerNameField != null && PlayerPrefs.HasKey(playerNamePrefsKey))
        {
            playerNameField.text = PlayerPrefs.GetString(playerNamePrefsKey);
        }
    }

    private void PopulateTestRooms()
    {
        ClearLobbyList();

        List<TestRoomData> fakeRooms = new List<TestRoomData>()
        {
            new TestRoomData("HauntedHouse01"),
            new TestRoomData("BasementRun"),
            new TestRoomData("NightShift"),
            new TestRoomData("HospitalEscape"),
            new TestRoomData("SchoolCorridor")
        };

        foreach (TestRoomData room in fakeRooms)
        {
            CreateRoomEntry(room);
        }
    }

    private void ClearLobbyList()
    {
        if (lobbyListContent == null)
        {
            Debug.LogWarning("LobbyListContent is missing.");
            return;
        }

        for (int i = lobbyListContent.childCount - 1; i >= 0; i--)
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
            entryUI.Setup(roomData.RoomName, () => OnJoinRoomPressed(roomData));
        }
        else
        {
            Debug.LogWarning("Room entry prefab is missing LobbyRoomEntryUI.");
        }
    }

    private void OnJoinRoomPressed(TestRoomData roomData)
    {
        Debug.Log($"Join clicked for room: {roomData.RoomName}");

        // Later:
        // await UnityLobbyManager.Instance.JoinLobbyAsync(...);
    }

    private class TestRoomData
    {
        public string RoomName;

        public TestRoomData(string roomName)
        {
            RoomName = roomName;
        }
    }
}