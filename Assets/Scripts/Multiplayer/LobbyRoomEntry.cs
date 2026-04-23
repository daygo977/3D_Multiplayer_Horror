using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyRoomEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private Button joinButton;

    public void Setup(string roomName, int currentPlayers, int maxPlayers, bool isLocked, UnityAction onJoinClicked)
    {
        roomNameText.text = roomName;
        playerCountText.text = $"{currentPlayers}/{maxPlayers}";
        lockText.text = isLocked ? "Locked" : "Open";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(onJoinClicked);
    }
}