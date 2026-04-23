using TMPro;
using UnityEngine;

/// <summary>
/// Controls one player row in lobby player list.
/// This row shows player's display name.
/// </summary>
public class LobbyPlayerEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;

    /// <summary>
    /// Sets visible player name on this row.
    /// </summary>
    public void Setup(string playerName)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;
    }
}