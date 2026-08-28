using UnityEngine;

/// <summary>
/// Put this on an empty GameObject in the Workshop scene. Reads
/// GameSession.CurrentLevel on Start to know this level's day limit and
/// scoring pars. Counts in-game days via DayNightCycle.onDayStart; once
/// Day Limit is reached, computes the final score (gold earned, weapons
/// made, time taken, star rating), banks the player's current gold into
/// GameSession.BankedGold, records the result, and shows the Scorecard UI.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private Currency playerCurrency;
    [Tooltip("Used if GameSession.CurrentLevel wasn't set - e.g. testing this scene directly in the editor without going through the map first.")]
    [SerializeField] private LevelDefinition testLevelFallback;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private LevelDefinition level;
    private int daysElapsed;
    private bool levelEnded;

    public LevelDefinition Level => level;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        level = GameSession.CurrentLevel != null ? GameSession.CurrentLevel : testLevelFallback;

        if (level == null)
        {
            Debug.LogWarning("[LevelManager] No current level set (GameSession.CurrentLevel is empty and no Test Level Fallback assigned) - the day-limit end-of-level check is disabled.");
        }
        else if (debugLogging)
        {
            Debug.Log($"[LevelManager] Playing '{level.levelName}' - day limit {level.dayLimit}.");
        }
    }

    private void OnEnable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.onDayStart.AddListener(HandleDayStart);
        }
    }

    private void OnDisable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.onDayStart.RemoveListener(HandleDayStart);
        }
    }

    private void HandleDayStart()
    {
        if (levelEnded || level == null)
        {
            return;
        }

        daysElapsed++;
        if (debugLogging) Debug.Log($"[LevelManager] Day {daysElapsed}/{level.dayLimit}.");

        if (daysElapsed >= level.dayLimit)
        {
            EndLevel();
        }
    }

    private void EndLevel()
    {
        levelEnded = true;

        int goldEarned = playerCurrency != null ? playerCurrency.Balance : 0;
        int weaponsMade = CraftingStatsTracker.Instance != null ? CraftingStatsTracker.Instance.GetTotalCraftedCount() : 0;
        float timeSeconds = Time.timeSinceLevelLoad;

        int stars = ComputeStars(goldEarned, weaponsMade, timeSeconds);
        bool passed = stars >= level.starsRequiredToAdvance && goldEarned >= level.goldRequiredToAdvance;

        LevelResult result = new LevelResult
        {
            weaponsMade = weaponsMade,
            goldEarned = goldEarned,
            timeSeconds = timeSeconds,
            stars = stars,
            passed = passed
        };

        GameSession.BankedGold += goldEarned;
        GameSession.RecordResult(level, result);

        if (debugLogging) Debug.Log($"[LevelManager] Level ended - {stars} stars, {goldEarned}g earned (now banked, total bank {GameSession.BankedGold}g), {weaponsMade} weapons, {timeSeconds:F0}s. Passed: {passed}.");

        if (ScorecardUI.Instance != null)
        {
            ScorecardUI.Instance.Show(level, result);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[LevelManager] No ScorecardUI found in the scene - result computed but nothing to display.");
        }
    }

    /// <summary>
    /// Averages three 0-1 scores (gold vs Gold Par, weapons vs Weapons Par,
    /// time vs Time Par Seconds - faster than par scores higher) into a 0-5
    /// star rating. Par values live on LevelDefinition so each level can be
    /// tuned independently through playtesting.
    /// </summary>
    private int ComputeStars(int goldEarned, int weaponsMade, float timeSeconds)
    {
        float goldScore = level.goldPar > 0 ? Mathf.Clamp01((float)goldEarned / level.goldPar) : 1f;
        float weaponsScore = level.weaponsPar > 0 ? Mathf.Clamp01((float)weaponsMade / level.weaponsPar) : 1f;
        float timeScore = level.timeParSeconds > 0 ? Mathf.Clamp01(level.timeParSeconds / Mathf.Max(1f, timeSeconds)) : 1f;

        float average = (goldScore + weaponsScore + timeScore) / 3f;
        return Mathf.Clamp(Mathf.RoundToInt(average * 5f), 0, 5);
    }
}