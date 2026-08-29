using UnityEngine;
using TMPro;

/// <summary>
/// Put this on the level preview panel in the Map scene's UI. Shown by
/// LevelNode the FIRST time a node is clicked (before committing to load
/// it) - displays the level's name, tier, requirements, and best result so
/// far. Hidden again when the player clicks elsewhere, exits map view, or
/// commits to loading the level.
///
/// Two ways to actually load the armed level:
///   1) Click the same LevelNode a second time (handled in LevelNode.Interact).
///   2) Wire this panel's Play button to PlayArmedLevel() below.
/// Both end up calling LevelNode.LoadLevel() on whichever node is armed.
/// </summary>
public class LevelPreviewUI : MonoBehaviour
{
    public static LevelPreviewUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text levelNameLabel;
    [SerializeField] private TMP_Text tierLabel;
    [SerializeField] private TMP_Text dayLimitLabel;
    [SerializeField] private TMP_Text requirementsLabel;
    [SerializeField] private TMP_Text bestResultLabel;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    /// <summary>The node currently armed/previewed, so the Play button knows what to load.</summary>
    private LevelNode currentNode;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Show(LevelDefinition level, LevelNode node)
    {
        if (level == null)
        {
            return;
        }

        currentNode = node;
        IsOpen = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (levelNameLabel != null) levelNameLabel.text = level.levelName;
        if (tierLabel != null) tierLabel.text = level.tier;
        if (dayLimitLabel != null) dayLimitLabel.text = $"{level.dayLimit} Days";
        if (requirementsLabel != null) requirementsLabel.text = $"Requires {level.starsRequiredToAdvance}\u2605 + {level.goldRequiredToAdvance}g to advance";

        if (bestResultLabel != null)
        {
            bestResultLabel.text = GameSession.BestResults.TryGetValue(level, out LevelResult best)
                ? $"Best: {best.stars}/5 stars, {best.goldEarned}g"
                : "Not yet attempted";
        }

        if (debugLogging) Debug.Log($"[LevelPreviewUI] Showing preview for '{level.levelName}'.");
    }

    public void Hide()
    {
        if (!IsOpen)
        {
            return;
        }

        currentNode = null;
        IsOpen = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (debugLogging) Debug.Log("[LevelPreviewUI] Preview hidden.");
    }

    /// <summary>Hook this up to the panel's Play button as an alternative to clicking the node again.</summary>
    public void PlayArmedLevel()
    {
        if (currentNode == null)
        {
            if (debugLogging) Debug.LogWarning("[LevelPreviewUI] Play pressed but no node is armed.");
            return;
        }

        currentNode.LoadLevel();
    }
}