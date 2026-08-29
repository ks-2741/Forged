using UnityEngine;

/// <summary>
/// Put this on each skill button in the skill tree UI/wall. Same
/// interaction model as everything else - left-click via PlayerInteractor
/// (or MapViewController's mouse raycast, if the skill tree lives on the
/// map wall) calls Interact(), which unlocks the skill if the player can
/// afford it and its prerequisite (the tier below it in the same path) is
/// already unlocked.
///
/// Colors itself like LevelNode does: gray if locked, yellow if
/// affordable but not yet bought, green once unlocked.
/// </summary>
public class SkillNode : MonoBehaviour, IInteractable
{
    [Header("Skill")]
    [SerializeField] private SkillDefinition skill;
    [Tooltip("The node directly before this one in its path - must already be unlocked before this one can be. Leave empty for the first node in a path.")]
    [SerializeField] private SkillNode prerequisite;

    [Header("Visuals")]
    [SerializeField] private Renderer nodeRenderer;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color affordableColor = Color.yellow;
    [SerializeField] private Color unlockedColor = Color.green;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public SkillDefinition Skill => skill;
    public bool IsUnlocked => SkillManager.IsUnlocked(skill);

    private void Update()
    {
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (nodeRenderer == null || skill == null)
        {
            return;
        }

        Color color = IsUnlocked ? unlockedColor : (CanAfford() ? affordableColor : lockedColor);
        nodeRenderer.material.color = color;
    }

    private bool PrerequisiteMet()
    {
        return prerequisite == null || prerequisite.IsUnlocked;
    }

    private bool CanAfford()
    {
        return PrerequisiteMet() && GameSession.BankedGold >= skill.goldCost;
    }

    public void Interact(GameObject interactor)
    {
        if (skill == null)
        {
            if (debugLogging) Debug.LogWarning($"[SkillNode] '{name}' has no Skill assigned.");
            return;
        }

        if (IsUnlocked)
        {
            if (debugLogging) Debug.Log($"[SkillNode] '{skill.skillName}' is already unlocked.");
            return;
        }

        if (!PrerequisiteMet())
        {
            if (debugLogging) Debug.Log($"[SkillNode] '{skill.skillName}' is locked - unlock '{prerequisite.Skill.skillName}' first.");
            return;
        }

        if (GameSession.BankedGold < skill.goldCost)
        {
            if (debugLogging) Debug.Log($"[SkillNode] Not enough gold for '{skill.skillName}' - need {skill.goldCost}g, have {GameSession.BankedGold}g.");
            return;
        }

        GameSession.BankedGold -= skill.goldCost;
        SkillManager.Unlock(skill);

        if (debugLogging) Debug.Log($"[SkillNode] Unlocked '{skill.skillName}' for {skill.goldCost}g (bank now {GameSession.BankedGold}g).");
    }
}