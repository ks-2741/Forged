using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent singleton tracking which Blueprints have been learned.
/// SellerStation checks IsUnlocked (via a ShopOffer's Required Blueprint)
/// before allowing a purchase. BlueprintUI calls TryUnlock when the player
/// clicks to learn one.
/// </summary>
public class BlueprintManager : MonoBehaviour
{
    public static BlueprintManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private readonly HashSet<Blueprint> unlocked = new HashSet<Blueprint>();

    private void Awake()
    {
        Instance = this;
        if (debugLogging) Debug.Log($"[BlueprintManager] Awake - this instance ID = {GetInstanceID()}.");
    }

    public bool IsUnlocked(Blueprint blueprint)
    {
        return blueprint != null && unlocked.Contains(blueprint);
    }

    /// <summary>
    /// Attempts to learn a blueprint: checks the crafted-item requirement,
    /// then spends the gold cost. Returns false (and spends nothing) if
    /// either requirement isn't met, or it's already learned.
    /// </summary>
    public bool TryUnlock(Blueprint blueprint, Currency currency, CraftingStatsTracker stats)
    {
        if (blueprint == null || IsUnlocked(blueprint))
        {
            return false;
        }

        int crafted = stats != null ? stats.GetCraftedCount(blueprint.requiredCraftedItem) : 0;
        if (crafted < blueprint.requiredCraftedAmount)
        {
            if (debugLogging) Debug.Log($"[BlueprintManager] Not enough crafted for '{blueprint.blueprintName}': {crafted}/{blueprint.requiredCraftedAmount}.");
            return false;
        }

        if (currency == null || !currency.TrySpend(blueprint.goldCost))
        {
            if (debugLogging) Debug.Log($"[BlueprintManager] Can't afford '{blueprint.blueprintName}' ({blueprint.goldCost}g).");
            return false;
        }

        unlocked.Add(blueprint);
        if (debugLogging) Debug.Log($"[BlueprintManager] Learned '{blueprint.blueprintName}' (instance ID {blueprint.GetInstanceID()}) on manager instance {GetInstanceID()}. Total learned: {unlocked.Count}.");
        return true;
    }
}