using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Lobby Browse scene UI.
///
/// - Reads player/lobby/password input fields
/// - Handles button clicks
/// - Saves/loads player's name locally
/// - Populates scrollable room list
/// - Shows a password popup when trying to join a locked room
///
/// Right now version uses fake test rooms to finish UI first.
/// Later, same script can call UnityLobbyManager for real lobby data.
/// </summary>
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
    
    //Stores room player is currently trying to join from popup.
    private TestRoomData pendingRoomToJoin;

    private void Start()
    {
        //Hook up button events
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

        if (enterPasswordButton != null)
            enterPasswordButton.onClick.RemoveListener(OnEnterPasswordPressed);

        if (cancelPasswordButton != null)
            cancelPasswordButton.onClick.RemoveListener(OnCancelPasswordPressed);
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

    /// <summary>
    /// Called when Enter is pressed on password popup.
    /// Checks entered password against pending test room.
    /// Later should call real join-lobby code.
    /// </summary>
    private void OnEnterPasswordPressed()
    {
        if (pendingRoomToJoin == null)
        {
            Debug.LogWarning("No pending room to join.");
            ClosePasswordPanel();
            return;
        }

        string enteredPassword = inputPasswordField != null ? inputPasswordField.text.Trim() : "";

        if (enteredPassword == pendingRoomToJoin.RoomPassword)
        {
            Debug.Log($"Correct password entered for room: {pendingRoomToJoin.RoomName}");
            ClosePasswordPanel();

            //Later:
            //await UnityLobbyManager.Instance.JoinLobbyAsync(pendingRoomToJoin.LobbyId, enteredPassword);

            Debug.Log($"Joining locked room: {pendingRoomToJoin.RoomName}");
        }
        else
        {
            Debug.LogWarning("Incorrect room password.");
        }
    }

    /// <summary>
    /// Called when Cancel is pressed on password popup.
    /// </summary>
    private void OnCancelPasswordPressed()
    {
        ClosePasswordPanel();
    }

    /// <summary>
    /// Opens password popup for a locked room.
    /// Clears any previously entered password.
    /// </summary>
    private void OpenPasswordPanel(TestRoomData roomData)
    {
        pendingRoomToJoin = roomData;

        if (inputPasswordField != null)
            inputPasswordField.text = "";

        if (passwordPanel != null)
            passwordPanel.SetActive(true);
    }

    /// <summary>
    /// Closes password popup and clear pending room.
    /// </summary>
    private void ClosePasswordPanel()
    {
        pendingRoomToJoin = null;

        if (inputPasswordField != null)
            inputPasswordField.text = "";

        if (passwordPanel != null)
            passwordPanel.SetActive(false);
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
            new TestRoomData("HauntedHouse01", false, ""),
            new TestRoomData("BasementRun", true, "1234"),
            new TestRoomData("NightShift", false, ""),
            new TestRoomData("HospitalEscape", true, "ghost"),
            new TestRoomData("SchoolCorridor", false, "")
        };

        foreach (TestRoomData room in fakeRooms)
        {
            CreateRoomEntry(room);
        }
    }

    /// <summary>
    /// Clears all current room entries inside the scroll view content.
    /// </summary>
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
    
    /// <summary>
    /// Instantiates one room entry prefab and fills it with room data.
    /// </summary>
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

        if (roomData.HasPassword)
        {
            Debug.Log("This room is password protected.");
            OpenPasswordPanel(roomData);
            return;
        }

        Debug.Log($"Joining open room: {roomData.RoomName}");

        // Later:
        // await UnityLobbyManager.Instance.JoinLobbyAsync(roomData.LobbyId);
    }

    /// <summary>
    /// Temp local data class used only for fake UI testing.
    /// Later will likely be replaced by real Unity Lobby data models.
    /// </summary>
    private class TestRoomData
    {
        public string RoomName;
        public bool HasPassword;
        public string RoomPassword;

        public TestRoomData(string roomName, bool hasPassword, string roomPassword)
        {
            RoomName = roomName;
            HasPassword = hasPassword;
            RoomPassword = roomPassword;
        }
    }
}