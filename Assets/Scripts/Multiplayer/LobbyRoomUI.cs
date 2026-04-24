using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Settings")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private float autoRefreshInterval = 10f;

    private bool isCountdownRunning = false;
    private Coroutine countdownCoroutine;
    private Coroutine autoRefreshCoroutine;

    private void Start()
    {
        //button listeners
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.AddListener(OnLeaveLobbyPressed);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartOrCancelPressed);

        if (confirmLeaveButton != null)
            confirmLeaveButton.onClick.AddListener(OnConfirmLeavePressed);

        if (cancelLeaveButton != null)
            cancelLeaveButton.onClick.AddListener(OnCancelLeavePressed);

        //popup starts hidden
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(false);

        //countdown text starts hidden
        if (gameStartCountdownText != null)
            gameStartCountdownText.gameObject.SetActive(false);

        //set initial start button state
        RefreshStartButtonState();

        //subscribe to lobby updates
        if (UnityLobbyManager.Instance != null)
        {
            UnityLobbyManager.Instance.OnCurrentLobbyChanged += HandleLobbyChanged;
            HandleLobbyChanged(UnityLobbyManager.Instance.CurrentLobby);
        }
        else
        {
            Debug.LogWarning("UnityLobbyManager not found");
        }

        //visual auto refresh text
        autoRefreshCoroutine = StartCoroutine(AutoRefreshTextRoutine());
    }

    private void OnDestroy()
    {
        //remove button listeners
        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.RemoveListener(OnLeaveLobbyPressed);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartOrCancelPressed);

        if (confirmLeaveButton != null)
            confirmLeaveButton.onClick.RemoveListener(OnConfirmLeavePressed);

        if (cancelLeaveButton != null)
            cancelLeaveButton.onClick.RemoveListener(OnCancelLeavePressed);

        //unsubscribe from lobby updates
        if (UnityLobbyManager.Instance != null)
            UnityLobbyManager.Instance.OnCurrentLobbyChanged -= HandleLobbyChanged;

        //stop coroutines
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        if (autoRefreshCoroutine != null)
            StopCoroutine(autoRefreshCoroutine);
    }

    private void HandleLobbyChanged(Lobby lobby)
    {
        //clear old player rows
        ClearPlayerList();

        if (UnityLobbyManager.Instance == null || lobby == null)
            return;

        //update host/client button state
        RefreshStartButtonState();

        //get real players from manager
        List<UnityLobbyManager.LobbyPlayerViewData> players =
            UnityLobbyManager.Instance.GetCurrentLobbyPlayers();

        int count = Mathf.Min(players.Count, maxPlayers);

        //rebuild player list
        for (int i = 0; i < count; i++)
        {
            CreatePlayerEntry(players[i].DisplayName);
        }
    }

    private void RefreshStartButtonState()
    {
        if (startGameButton == null)
            return;

        //only host can use start/cancel button
        bool isHost = UnityLobbyManager.Instance != null && UnityLobbyManager.Instance.IsHost;
        startGameButton.interactable = isHost;

        //update button text
        TMP_Text buttonText = startGameButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
            buttonText.text = isCountdownRunning ? "Cancel" : "Start Game";
    }

    private void OnLeaveLobbyPressed()
    {
        //show leave confirm popup
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(true);
    }

    private void OnCancelLeavePressed()
    {
        //hide leave confirm popup
        if (leaveConfirmScreen != null)
            leaveConfirmScreen.SetActive(false);
    }

    private async void OnConfirmLeavePressed()
    {
        //leave real lobby
        if (UnityLobbyManager.Instance != null)
            await UnityLobbyManager.Instance.LeaveLobbyAsync();
    }

    private void OnStartOrCancelPressed()
    {
        //host only
        if (UnityLobbyManager.Instance == null || !UnityLobbyManager.Instance.IsHost)
        {
            Debug.Log("Only host can start or cancel countdown");
            return;
        }

        //toggle countdown
        if (isCountdownRunning)
            CancelStartCountdown();
        else
            StartCountdown();
    }

    private void StartCountdown()
    {
        //stop old countdown if somehow still active
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        isCountdownRunning = true;
        RefreshStartButtonState();

        //show countdown text
        if (gameStartCountdownText != null)
            gameStartCountdownText.gameObject.SetActive(true);

        //start countdown coroutine
        countdownCoroutine = StartCoroutine(StartGameCountdownRoutine());
    }

    private void CancelStartCountdown()
    {
        //stop active countdown
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        isCountdownRunning = false;

        //clear countdown text
        if (gameStartCountdownText != null)
        {
            gameStartCountdownText.text = "";
            gameStartCountdownText.gameObject.SetActive(false);
        }

        RefreshStartButtonState();
        Debug.Log("Game start countdown cancelled");
    }

    private IEnumerator StartGameCountdownRoutine()
    {
        //10 second countdown
        for (int seconds = 10; seconds > 0; seconds--)
        {
            if (gameStartCountdownText != null)
                gameStartCountdownText.text = $"Starting Game in {seconds}...";

            yield return new WaitForSeconds(1f);
        }

        //final start message
        if (gameStartCountdownText != null)
            gameStartCountdownText.text = "Starting Game...";

        Debug.Log("Game would start here");

        /*
         * Later hook real game start here
         * Could also scale difficulty based on player count
         * Example:
         * - more players = stronger enemies
         * - maybe lower player health a bit
         */

        yield return new WaitForSeconds(1f);

        //reset countdown state
        isCountdownRunning = false;
        countdownCoroutine = null;

        if (gameStartCountdownText != null)
        {
            gameStartCountdownText.text = "";
            gameStartCountdownText.gameObject.SetActive(false);
        }

        RefreshStartButtonState();
    }

    private IEnumerator AutoRefreshTextRoutine()
    {
        while (true)
        {
            float timer = autoRefreshInterval;

            //visual timer text
            while (timer > 0f)
            {
                if (autoRefreshText != null)
                    autoRefreshText.text = $"Auto-Refresh: {Mathf.CeilToInt(timer)}s";

                timer -= Time.deltaTime;
                yield return null;
            }
        }
    }

    private void ClearPlayerList()
    {
        //guard content ref
        if (playerListContent == null)
        {
            Debug.LogWarning("PlayerListContent is missing");
            return;
        }

        //destroy old rows
        for (int i = playerListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(playerListContent.GetChild(i).gameObject);
        }
    }

    private void CreatePlayerEntry(string playerName)
    {
        //guard refs
        if (playerPanelPrefab == null || playerListContent == null)
        {
            Debug.LogWarning("PlayerPanelPrefab or PlayerListContent is missing");
            return;
        }

        //spawn player row
        GameObject entry = Instantiate(playerPanelPrefab, playerListContent);

        //assign player name
        LobbyPlayerEntryUI entryUI = entry.GetComponent<LobbyPlayerEntryUI>();
        if (entryUI != null)
        {
            entryUI.Setup(playerName);
        }
        else
        {
            Debug.LogWarning("Player panel prefab is missing LobbyPlayerEntryUI");
        }
    }
}