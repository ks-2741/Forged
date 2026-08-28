using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent (for the duration of this level - it resets on scene reload
/// like everything else in the Workshop scene) tracker for how many of
/// each item the player has crafted THIS level, never decreasing even if
/// the item is later sold/used/lost. MergeTable reports here the moment a
/// finished sword is produced.
/// </summary>
public class CraftingStatsTracker : MonoBehaviour
{
    public static CraftingStatsTracker Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private readonly Dictionary<ItemData, int> craftedCounts = new Dictionary<ItemData, int>();

    private void Awake()
    {
        Instance = this;
    }

    public void RecordCrafted(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return;
        }

        if (!craftedCounts.ContainsKey(item))
        {
            craftedCounts[item] = 0;
        }

        craftedCounts[item] += amount;

        if (debugLogging) Debug.Log($"[CraftingStats] '{item.itemName}' crafted total this level: {craftedCounts[item]}");
    }

    public int GetCraftedCount(ItemData item)
    {
        if (item == null)
        {
            return 0;
        }

        return craftedCounts.TryGetValue(item, out int count) ? count : 0;
    }

    /// <summary>Sum of every item's crafted count this level - used for the end-of-level "weapons made" stat.</summary>
    public int GetTotalCraftedCount()
    {
        int total = 0;
        foreach (int count in craftedCounts.Values)
        {
            total += count;
        }
        return total;
    }
}