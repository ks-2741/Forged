using UnityEngine;
using TMPro;

/// <summary>
/// Put this on a TMP_Text object in the Hub scene's Canvas only. Since
/// GameSession is static, it doesn't need any scene-checking logic here -
/// this simply won't exist (and so won't show) anywhere you don't add it,
/// like the Workshop scene.
///
/// Displays the player's permanent GameSession.BankedGold total, checking
/// once a frame and only touching the label when the value actually
/// changes (same "poll but cheaply" pattern as LevelNode's color refresh).
/// </summary>
public class BankGoldDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text goldLabel;
    [Tooltip("Text shown before the amount, e.g. 'Bank: ' -> 'Bank: 250g'.")]
    [SerializeField] private string prefix = "Bank: ";
    [SerializeField] private string suffix = "g";

    private int lastShownValue = -1;

    private void Update()
    {
        if (goldLabel == null)
        {
            return;
        }

        if (GameSession.BankedGold != lastShownValue)
        {
            lastShownValue = GameSession.BankedGold;
            goldLabel.text = $"{prefix}{lastShownValue}{suffix}";
        }
    }
}