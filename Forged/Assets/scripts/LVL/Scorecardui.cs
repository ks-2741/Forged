using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Put this on the scorecard panel in the Workshop scene's UI. Shown by
/// LevelManager once a level's day limit is reached. Displays the final
/// stats and lets the player return to the map.
/// </summary>
public class ScorecardUI : MonoBehaviour
{
    public static ScorecardUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text levelNameLabel;
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text weaponsLabel;
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text starsLabel;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Scene Flow")]
    [Tooltip("The map scene to load when the player presses Continue. Must exactly match a scene name that's been added to File > Build Settings.")]
    [SerializeField] private string mapSceneName = "Map";

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    /// <summary>True while the scorecard is on screen - PlayerController checks this to stop movement while it's up.</summary>
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Show(LevelDefinition level, LevelResult result)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (levelNameLabel != null)
        {
            levelNameLabel.text = level != null ? level.levelName : "Level Complete";
        }

        if (timeLabel != null)
        {
            timeLabel.text = $"Time Played: {FormatTime(result.timeSeconds)}";
        }

        if (weaponsLabel != null)
        {
            weaponsLabel.text = $"Weapons Made: {result.weaponsMade}";
        }

        if (goldLabel != null)
        {
            goldLabel.text = $"Gold Earned: {result.goldEarned}g";
        }

        if (starsLabel != null)
        {
            starsLabel.text = $"{result.stars} / 5 Stars";
        }

        if (resultLabel != null)
        {
            resultLabel.text = result.passed ? "Level Complete!" : "Requirements Not Met";
        }
    }

    /// <summary>Hook this up to the panel's Continue button.</summary>
    public void ContinueToMap()
    {
        if (debugLogging) Debug.Log($"[ScorecardUI] Continue pressed - loading Map Scene Name '{mapSceneName}'.");

        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogWarning("[ScorecardUI] Map Scene Name is empty - can't load anything.");
            return;
        }

        IsOpen = false;
        SceneManager.LoadScene(mapSceneName);
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.RoundToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes}m {remainingSeconds}s";
    }
}