using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controls one room entry in the scroll list.
/// This simple version only shows the lobby name on a button.
/// </summary>
public class LobbyRoomEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private Button joinButton;

    public void Setup(string lobbyName, UnityAction onJoinClicked)
    {
        lobbyNameText.text = lobbyName;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(onJoinClicked);
    }
}