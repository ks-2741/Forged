using UnityEngine;

/// <summary>Which column in the skill tree this skill belongs to.</summary>
public enum SkillPath
{
    Technique,
    Efficiency,
    Multitask
}

/// <summary>
/// Data-only definition of one skill tree node. Create one asset per
/// button via Assets > Create > Skills > Skill Definition.
///
/// Effect Value's meaning depends on Path:
///   Technique  - sell price multiplier bonus (e.g. 0.1 = +10% sell price)
///   Efficiency - packed as two uses on the same tier: Furnace Slot Bonus
///                and Craft Speed Multiplier below (Effect Value unused)
///   Multitask  - unused, presence of ANY unlocked Multitask skill is the flag
///
/// Tiers within a path unlock in order (SkillNode enforces this via its
/// Prerequisite reference) - Tier is just for SkillManager to find the
/// highest unlocked one quickly, it doesn't drive gating by itself.
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Info")]
    public string skillName = "New Skill";
    [TextArea] public string description;
    public SkillPath path = SkillPath.Technique;
    [Tooltip("Order within this path - 1 is the first/cheapest tier. Used by SkillManager to find the highest unlocked tier in a path.")]
    public int tier = 1;

    [Header("Cost")]
    [Tooltip("Deducted from GameSession.BankedGold when unlocked.")]
    public int goldCost = 100;

    [Header("Technique Effect")]
    [Tooltip("Sell price multiplier bonus at this tier, e.g. 0.1 = weapons sell for +10%. Only read for Technique-path skills.")]
    public float sellPriceBonus = 0.1f;

    [Header("Efficiency Effect")]
    [Tooltip("Extra ore slots added to the furnace at this tier. Only read for Efficiency-path skills.")]
    public int furnaceSlotBonus = 1;
    [Tooltip("Crafting time multiplier at this tier, e.g. 0.9 = 10% faster. Only read for Efficiency-path skills.")]
    public float craftSpeedMultiplier = 0.9f;
}