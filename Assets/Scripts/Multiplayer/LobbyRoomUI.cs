using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls LobbyRoom scene UI.
///
/// - Shows leave confirmation popup
/// - Enables Start/Cancel only for the host
/// - Displays a 10 second countdown before game start
/// - Lets host cancel countdown
/// - Populates player list
/// - Keeps host at top, then others by join order
///
/// Later on this can connect to real Unity Lobby / Relay / NGO data.
/// </summary>
public class LobbyRoomUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button confirmLeaveButton;
    [SerializeField] private Button cancelLeaveButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text gameStartCountdownText;
    [SerializeField] private TMP_Text autoRefreshText;

    [Header("Popup")]
    [SerializeField] private GameObject leaveConfirmScreen;

    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerPanelPrefab;

    [Header("Scene Settings")]
    [SerializeField] private string lobbyBrowseSceneName = "LobbyBrowse";
    [SerializeField] private int maxPlayers = 4;

    [Header("Debug / Temporary")]
    [SerializeField] private bool isHost = true;
    [SerializeField] private float autoRefreshInterval = 10f;

    //Tracks whether start countdown is active
    private bool isCountdownRunning = false;

    //Holds active countdown coroutine so it can be cancelled
    private Coroutine countdownCoroutine;

    //Holds active auto-refresh coroutine
    private Coroutine autoRefreshCoroutine;

    private void Start()
    {
        //Hook up button listeners
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.AddListener(OnLeaveLobbyPressed);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartOrCancelPressed);

        if (confirmLeaveButton != null)
            confirmLeaveButton.onClick.AddListener(OnConfirmLeavePressed);

        if (cancelLeaveButton != null)
            cancelLeaveButton.onClick.AddListener(OnCancelLeavePressed);

        //Hide popup on scene start
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(false);

        //Hide countdown text on scene start
        if (gameStartCountdownText != null)
            gameStartCountdownText.gameObject.SetActive(false);

        //Set correct state for host/client start button
        RefreshStartButtonState();

        //Temp test players for UI testing
        PopulateTestPlayers();

        //Start auto-refresh text cycle
        autoRefreshCoroutine = StartCoroutine(AutoRefreshRoutine());
    }

    private void OnDestroy()
    {
        //Remove button listeners
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.RemoveListener(OnLeaveLobbyPressed);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartOrCancelPressed);

        if (confirmLeaveButton != null)
            confirmLeaveButton.onClick.RemoveListener(OnConfirmLeavePressed);

        if (cancelLeaveButton != null)
            cancelLeaveButton.onClick.RemoveListener(OnCancelLeavePressed);
    }

    /// <summary>
    /// Updates  Start Game button based on whether the local player is host,
    /// and whether a countdown is currently running.
    /// </summary>
    private void RefreshStartButtonState()
    {
        if (startGameButton == null)
            return;

        // Only host can interact with this button
        startGameButton.interactable = isHost;

        // Update button label depending on current countdown state
        TMP_Text buttonText = startGameButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isCountdownRunning ? "Cancel" : "Start Game";
        }
    }

    /// <summary>
    /// Shows leave confirmation popup
    /// </summary>
    private void OnLeaveLobbyPressed()
    {
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(true);
    }

    /// <summary>
    /// Hides leave confirmation popup
    /// </summary>
    private void OnCancelLeavePressed()
    {
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(false);
    }

    /// <summary>
    /// Leaves lobby and returns to the lobby browse scene.
    /// Later this should call real lobby leave logic.
    /// </summary>
    private void OnConfirmLeavePressed()
    {
        Debug.Log("Leaving lobby and returning to LobbyBrowse.");

        //Later:
        //await UnityLobbyManager.Instance.LeaveLobbyAsync();

        SceneManager.LoadScene(lobbyBrowseSceneName);
    }

    /// <summary>
    /// Handles Start Game button.
    ///
    /// If no countdown is running:
    /// - starts countdown
    ///
    /// If countdown is already running:
    /// - cancels countdown
    /// </summary>
    private void OnStartOrCancelPressed()
    {
        if (!isHost)
        {
            Debug.Log("Only the host can start or cancel the game countdown.");
            return;
        }

        if (isCountdownRunning)
        {
            CancelStartCountdown();
        }
        else
        {
            StartCountdown();
        }
    }

    /// <summary>
    /// Starts game countdown and updates UI
    /// </summary>
    private void StartCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        isCountdownRunning = true;
        RefreshStartButtonState();

        if (gameStartCountdownText != null)
            gameStartCountdownText.gameObject.SetActive(true);

        countdownCoroutine = StartCoroutine(StartGameCountdownRoutine());
    }

    /// <summary>
    /// Cancels active countdown and restores the UI.
    /// </summary>
    private void CancelStartCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        isCountdownRunning = false;
        RefreshStartButtonState();

        if (gameStartCountdownText != null)
        {
            gameStartCountdownText.text = "";
            gameStartCountdownText.gameObject.SetActive(false);
        }

        Debug.Log("Game start countdown cancelled.");
    }

    /// <summary>
    /// Runs 10 second start countdown.
    /// After the countdown finishes, the game will start.
    /// </summary>
    private IEnumerator StartGameCountdownRoutine()
    {
        for (int seconds = 10; seconds > 0; seconds--)
        {
            if (gameStartCountdownText != null)
                gameStartCountdownText.text = $"Starting Game in {seconds}...";

            yield return new WaitForSeconds(1f);
        }

        if (gameStartCountdownText != null)
            gameStartCountdownText.text = "Starting Game...";

        Debug.Log("Game would start here.");

        //TODO:
        //Scale game difficulty when players join.
        //Example idea:
        //- Increase enemy stats for each extra player
        //- Possibly lower player health slightly
        //Implement once the real gameplay scene and systems exist.

        yield return new WaitForSeconds(1f);

        isCountdownRunning = false;
        countdownCoroutine = null;

        if (gameStartCountdownText != null)
        {
            gameStartCountdownText.text = "";
            gameStartCountdownText.gameObject.SetActive(false);
        }

        RefreshStartButtonState();
    }

    /// <summary>
    /// Updates the auto-refresh label and refreshes the player list on timer.
    ///
    /// Right now this only refreshes test list.
    /// Later should poll real lobby data.
    /// </summary>
    private IEnumerator AutoRefreshRoutine()
    {
        while (true)
        {
            float timer = autoRefreshInterval;

            while (timer > 0f)
            {
                if (autoRefreshText != null)
                    autoRefreshText.text = $"Auto-Refresh: {Mathf.CeilToInt(timer)}s";

                timer -= Time.deltaTime;
                yield return null;
            }

            RefreshPlayerListFromSource();
        }
    }

    /// <summary>
    /// Temp refresh method for UI testing.
    /// Later should fetch real players from the lobby manager.
    /// </summary>
    private void RefreshPlayerListFromSource()
    {
        Debug.Log("Refreshing player list...");

        PopulateTestPlayers();

        //Later:
        //var players = UnityLobbyManager.Instance.GetLobbyPlayers();
        //RefreshPlayerList(players);
    }

    /// <summary>
    /// Temp fake player data for testing lobby list UI.
    /// </summary>
    private void PopulateTestPlayers()
    {
        List<PlayerListEntryData> players = new List<PlayerListEntryData>()
        {
            new PlayerListEntryData("HostPlayer", true, 0),
            new PlayerListEntryData("ClientOne", false, 1),
            new PlayerListEntryData("ClientTwo", false, 2),
            new PlayerListEntryData("ClientThree", false, 3)
        };

        RefreshPlayerList(players);
    }

    /// <summary>
    /// Rebuilds player list UI.
    /// Host is always shown first, followed by clients in join order.
    /// Maximum displayed players is capped at maxPlayers.
    /// </summary>
    public void RefreshPlayerList(List<PlayerListEntryData> players)
    {
        ClearPlayerList();

        if (players == null)
            return;

        //Sort players:
        //1. Host first
        //2. Then by join order
        players.Sort((a, b) =>
        {
            if (a.IsHost && !b.IsHost) return -1;
            if (!a.IsHost && b.IsHost) return 1;
            return a.JoinOrder.CompareTo(b.JoinOrder);
        });

        //Enforce room cap.
        int count = Mathf.Min(players.Count, maxPlayers);

        for (int i = 0; i < count; i++)
        {
            CreatePlayerEntry(players[i]);
        }
    }

    /// <summary>
    /// Clears all spawned player rows from the scroll list.
    /// </summary>
    private void ClearPlayerList()
    {
        if (playerListContent == null)
            return;

        for (int i = playerListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(playerListContent.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Instantiates one player panel row and fills it with the player's name.
    /// </summary>
    private void CreatePlayerEntry(PlayerListEntryData playerData)
    {
        if (playerPanelPrefab == null || playerListContent == null)
        {
            Debug.LogWarning("PlayerPanelPrefab or PlayerListContent is missing.");
            return;
        }

        GameObject entry = Instantiate(playerPanelPrefab, playerListContent);

        LobbyPlayerEntryUI entryUI = entry.GetComponent<LobbyPlayerEntryUI>();
        if (entryUI != null)
        {
            entryUI.Setup(playerData.PlayerName);
        }
        else
        {
            Debug.LogWarning("Player panel prefab is missing LobbyPlayerEntryUI.");
        }
    }

    /// <summary>
    /// Simple local data model for one player in the lobby.
    /// </summary>
    [System.Serializable]
    public class PlayerListEntryData
    {
        public string PlayerName;
        public bool IsHost;
        public int JoinOrder;

        public PlayerListEntryData(string playerName, bool isHost, int joinOrder)
        {
            PlayerName = playerName;
            IsHost = isHost;
            JoinOrder = joinOrder;
        }
    }
}