using System.Collections.Generic;

/// <summary>
/// Static, NOT a MonoBehaviour - holds whatever needs to survive a scene
/// reload/change for the whole play session: which level is currently
/// being played, the player's permanent Money Bank (spent on the skill
/// tree/cosmetics later), which Blueprints have been permanently learned,
/// which skill tree nodes have been unlocked, and the best result recorded
/// so far for each level (for the map to show stars per level and gate
/// which levels are unlocked).
///
/// Static fields persist automatically across SceneManager.LoadScene calls
/// for as long as the game is running - no DontDestroyOnLoad object needed.
/// They only reset on a full app restart (or exiting Play Mode in editor).
/// </summary>
public static class GameSession
{
    public static LevelDefinition CurrentLevel;

    public static int BankedGold;

    public static readonly HashSet<Blueprint> UnlockedBlueprints = new HashSet<Blueprint>();

    public static readonly HashSet<SkillDefinition> UnlockedSkills = new HashSet<SkillDefinition>();

    public static readonly Dictionary<LevelDefinition, LevelResult> BestResults = new Dictionary<LevelDefinition, LevelResult>();

    /// <summary>Records a level's result, keeping only the BEST star count seen for that level across attempts.</summary>
    public static void RecordResult(LevelDefinition level, LevelResult result)
    {
        if (level == null)
        {
            return;
        }

        if (!BestResults.TryGetValue(level, out LevelResult existing) || result.stars > existing.stars)
        {
            BestResults[level] = result;
        }
    }
}

/// <summary>Result of a single level attempt - used for the scorecard now, and later for the map's per-level star display.</summary>
[System.Serializable]
public class LevelResult
{
    public int weaponsMade;
    public int goldEarned;
    public float timeSeconds;
    public int stars;
    public bool passed;
}