using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityLobbyManager : MonoBehaviour
{
    public static UnityLobbyManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string lobbyBrowseSceneName = "LobbyBrowse";
    [SerializeField] private string lobbyRoomSceneName = "LobbyRoom";

    [Header("Lobby Settings")]
    [SerializeField] private int maxPlayersPerLobby = 4;

    [Header("Polling / Heartbeat")]
    [SerializeField] private float lobbyPollIntervalSeconds = 2f;
    [SerializeField] private float lobbyHeartbeatIntervalSeconds = 15f;

    public Lobby CurrentLobby { get; private set; }
    public List<Lobby> AvailableLobbies { get; private set; } = new();

    public bool IsHost =>
        CurrentLobby != null &&
        AuthenticationService.Instance.IsSignedIn &&
        CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;

    public event Action<List<Lobby>> OnAvailableLobbiesChanged;
    public event Action<Lobby> OnCurrentLobbyChanged;
    public event Action OnLeftLobby;

    private Coroutine pollLobbyCoroutine;
    private Coroutine heartbeatCoroutine;

    private void Awake()
    {
        //singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateLobbyAsync(string playerName, string lobbyName, string password)
    {
        //signed in check
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player not signed in");
            return false;
        }

        try
        {
            //host player data
            var hostPlayer = new Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject>
                {
                    {
                        "DisplayName",
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                    }
                });

            //lobby create options
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Password = string.IsNullOrWhiteSpace(password) ? null : password,
                Player = hostPlayer,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "HasPassword",
                        new DataObject(
                            DataObject.VisibilityOptions.Public,
                            string.IsNullOrWhiteSpace(password) ? "0" : "1"
                        )
                    }
                }
            };

            //create lobby
            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayersPerLobby, options);

            //start polling and heartbeat
            StartLobbyMaintenance();
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);

            //move to room scene
            SceneManager.LoadScene(lobbyRoomSceneName);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"CreateLobbyAsync failed: {e}");
            return false;
        }
    }

    public async Task RefreshAvailableLobbiesAsync()
    {
        try
        {
            //lobby browser query
            var options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0"
                    )
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(
                        asc: false,
                        field: QueryOrder.FieldOptions.Created
                    )
                }
            };

            //query lobbies
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            //update cached list
            AvailableLobbies = response.Results ?? new List<Lobby>();
            OnAvailableLobbiesChanged?.Invoke(AvailableLobbies);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"RefreshAvailableLobbiesAsync failed: {e}");
        }
    }

    public async Task<bool> JoinLobbyAsync(string lobbyId, string playerName, string password = null)
    {
        //signed in check
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player not signed in");
            return false;
        }

        try
        {
            //join options
            var options = new JoinLobbyByIdOptions
            {
                Password = string.IsNullOrWhiteSpace(password) ? null : password,
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: new Dictionary<string, PlayerDataObject>
                    {
                        {
                            "DisplayName",
                            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                        }
                    })
            };

            //join lobby
            CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

            //start polling and heartbeat if needed
            StartLobbyMaintenance();
            OnCurrentLobbyChanged?.Invoke(CurrentLobby);

            //move to room scene
            SceneManager.LoadScene(lobbyRoomSceneName);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"JoinLobbyAsync failed: {e}");
            return false;
        }
    }

    public async Task LeaveLobbyAsync()
    {
        //no lobby guard
        if (CurrentLobby == null)
            return;

        try
        {
            //remove local player from lobby
            await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"LeaveLobbyAsync failed: {e}");
        }
        finally
        {
            //clear local state
            StopLobbyMaintenance();
            CurrentLobby = null;

            OnLeftLobby?.Invoke();
            OnCurrentLobbyChanged?.Invoke(null);

            //move back to browser scene
            SceneManager.LoadScene(lobbyBrowseSceneName);
        }
    }

    public List<LobbyPlayerViewData> GetCurrentLobbyPlayers()
    {
        //build player list for room ui
        var result = new List<LobbyPlayerViewData>();

        if (CurrentLobby?.Players == null)
            return result;

        for (int i = 0; i < CurrentLobby.Players.Count; i++)
        {
            Player player = CurrentLobby.Players[i];
            string displayName = player.Id;

            //try display name first
            if (player.Data != null &&
                player.Data.TryGetValue("DisplayName", out PlayerDataObject displayNameData) &&
                !string.IsNullOrWhiteSpace(displayNameData.Value))
            {
                displayName = displayNameData.Value;
            }

            result.Add(new LobbyPlayerViewData
            {
                PlayerId = player.Id,
                DisplayName = displayName,
                IsHost = player.Id == CurrentLobby.HostId,
                JoinOrder = i
            });
        }

        //host first then join order
        return result
            .OrderByDescending(p => p.IsHost)
            .ThenBy(p => p.JoinOrder)
            .ToList();
    }

    public bool LobbyRequiresPassword(Lobby lobby)
    {
        //check public password flag
        if (lobby?.Data == null)
            return false;

        return lobby.Data.TryGetValue("HasPassword", out DataObject hasPasswordData) &&
               hasPasswordData.Value == "1";
    }

    private void StartLobbyMaintenance()
    {
        //restart polling and heartbeat
        StopLobbyMaintenance();

        pollLobbyCoroutine = StartCoroutine(PollLobbyCoroutine());
        SyncHeartbeatState();
    }

    private void StopLobbyMaintenance()
    {
        //stop polling
        if (pollLobbyCoroutine != null)
        {
            StopCoroutine(pollLobbyCoroutine);
            pollLobbyCoroutine = null;
        }

        //stop heartbeat
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }

    private void SyncHeartbeatState()
    {
        //host keeps lobby alive
        if (CurrentLobby != null && IsHost)
        {
            if (heartbeatCoroutine == null)
                heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine());
        }
        else
        {
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }
        }
    }

    private IEnumerator PollLobbyCoroutine()
    {
        while (CurrentLobby != null)
        {
            //refresh lobby state
            Task task = RefreshCurrentLobbyAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            yield return new WaitForSeconds(lobbyPollIntervalSeconds);
        }
    }

    private IEnumerator HeartbeatLobbyCoroutine()
    {
        while (CurrentLobby != null && IsHost)
        {
            //send heartbeat
            Task task = SendHeartbeatAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            yield return new WaitForSeconds(lobbyHeartbeatIntervalSeconds);
        }

        heartbeatCoroutine = null;
    }

    private async Task RefreshCurrentLobbyAsync()
    {
        if (CurrentLobby == null)
            return;

        try
        {
            //get latest lobby state
            CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

            //host may have changed
            SyncHeartbeatState();

            OnCurrentLobbyChanged?.Invoke(CurrentLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"RefreshCurrentLobbyAsync failed: {e}");
        }
    }

    private async Task SendHeartbeatAsync()
    {
        if (CurrentLobby == null || !IsHost)
            return;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"SendHeartbeatAsync failed: {e}");
        }
    }

    [Serializable]
    public class LobbyPlayerViewData
    {
        public string PlayerId;
        public string DisplayName;
        public bool IsHost;
        public int JoinOrder;
    }
}