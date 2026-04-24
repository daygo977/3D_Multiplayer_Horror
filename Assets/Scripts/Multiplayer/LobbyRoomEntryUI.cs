using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controls one room row in the browse list.
/// This row only shows the lobby name and handles the click action.
/// </summary>
public class LobbyRoomEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private Button joinButton;

    public void Setup(string lobbyName, UnityAction onJoinClicked)
    {
        if (lobbyNameText != null)
            lobbyNameText.text = lobbyName;

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(onJoinClicked);
        }
    }
}