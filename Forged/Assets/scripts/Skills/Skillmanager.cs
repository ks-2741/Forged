/// <summary>
/// Static query helper over GameSession.UnlockedSkills - not a
/// MonoBehaviour, same pattern as GameSession itself. SkillNode calls
/// Unlock() when the player successfully buys a node; every other system
/// (selling, furnace, workshop equipment) reads the Get*/Is* helpers below
/// instead of touching GameSession.UnlockedSkills directly.
/// </summary>
public static class SkillManager
{
    public static bool IsUnlocked(SkillDefinition skill)
    {
        return skill != null && GameSession.UnlockedSkills.Contains(skill);
    }

    /// <summary>Called by SkillNode once gold has been deducted and the unlock is confirmed.</summary>
    public static void Unlock(SkillDefinition skill)
    {
        if (skill == null)
        {
            return;
        }

        GameSession.UnlockedSkills.Add(skill);
    }

    /// <summary>Highest-tier unlocked skill in a path, or null if none of that path is unlocked yet.</summary>
    public static SkillDefinition HighestUnlocked(SkillPath path)
    {
        SkillDefinition best = null;

        foreach (SkillDefinition skill in GameSession.UnlockedSkills)
        {
            if (skill.path == path && (best == null || skill.tier > best.tier))
            {
                best = skill;
            }
        }

        return best;
    }

    /// <summary>Current sell price multiplier from the Technique path - 1.0 if nothing unlocked yet. Apply as goldEarned * this.</summary>
    public static float SellPriceMultiplier
    {
        get
        {
            SkillDefinition best = HighestUnlocked(SkillPath.Technique);
            return best != null ? 1f + best.sellPriceBonus : 1f;
        }
    }

    /// <summary>Extra furnace ore slots unlocked from the Efficiency path - 0 if nothing unlocked yet.</summary>
    public static int FurnaceSlotBonus
    {
        get
        {
            SkillDefinition best = HighestUnlocked(SkillPath.Efficiency);
            return best != null ? best.furnaceSlotBonus : 0;
        }
    }

    /// <summary>Crafting time multiplier from the Efficiency path - 1.0 (no change) if nothing unlocked yet. Apply as baseTime * this.</summary>
    public static float CraftSpeedMultiplier
    {
        get
        {
            SkillDefinition best = HighestUnlocked(SkillPath.Efficiency);
            return best != null ? best.craftSpeedMultiplier : 1f;
        }
    }

    /// <summary>True once any Multitask-path skill has been unlocked.</summary>
    public static bool IsMultitaskUnlocked => HighestUnlocked(SkillPath.Multitask) != null;
}