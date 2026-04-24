using TMPro;
using UnityEngine;

/// <summary>
/// Controls one player row in the lobby room player list.
/// </summary>
public class LobbyPlayerEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;

    public void Setup(string playerName)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;
    }
}