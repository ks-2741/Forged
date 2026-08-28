using UnityEngine;

/// <summary>
/// Data-only definition of one level (e.g. "Knights - Level 3"). Create one
/// asset per level via Assets > Create > Levels > Level Definition. The
/// Workshop scene reads GameSession.CurrentLevel on Start to configure
/// itself; LevelManager uses the fields here to decide when the level ends
/// and how it's scored.
/// </summary>
[CreateAssetMenu(fileName = "New Level", menuName = "Levels/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Info")]
    public string levelName = "New Level";
    [Tooltip("Which tier this level belongs to (Knights, Samurai, etc.) - descriptive for now, useful once tier-specific customer/noble content is wired in.")]
    public string tier = "Knights";

    [Header("Duration")]
    [Tooltip("How many in-game days this level runs before it automatically ends.")]
    public int dayLimit = 5;

    [Header("Requirements to Advance")]
    [Tooltip("Minimum stars (out of 5) required to unlock the next level.")]
    [Range(0, 5)] public int starsRequiredToAdvance = 4;
    [Tooltip("Minimum gold earned THIS level required to unlock the next level, in addition to the star requirement.")]
    public int goldRequiredToAdvance = 100;

    [Header("Star Rating - Par Values")]
    [Tooltip("Gold earned considered a solid result for this level. Earning more/less shifts the gold component of the star score up/down from there.")]
    public int goldPar = 100;
    [Tooltip("Weapons crafted considered a solid result.")]
    public int weaponsPar = 10;
    [Tooltip("Time (seconds) considered a solid result - finishing faster than this scores better on this axis, slower scores worse.")]
    public float timeParSeconds = 600f;

    [Header("Next Level")]
    [Tooltip("Optional - the level this unlocks once passed. Leave empty for the last level in a tier.")]
    public LevelDefinition nextLevel;
}