using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Like CraftingRecipe, but for stations that need MULTIPLE different
/// items placed before crafting can start (e.g. the merge table needing
/// a blade, cross guard, and grip). Create via Assets > Create > Crafting
/// > Merge Recipe.
/// </summary>
[CreateAssetMenu(fileName = "New Merge Recipe", menuName = "Crafting/Merge Recipe")]
public class MergeRecipe : ScriptableObject
{
    [Header("Required Parts")]
    [Tooltip("Every item in this list must be placed (one of each) before merging can start. Order here should match the order of Item Slots on MergeTable.")]
    public List<ItemData> requiredItems = new List<ItemData>();

    [Header("Output")]
    public ItemData outputItem;
    public int outputAmount = 1;

    [Header("Timing")]
    [Tooltip("Seconds of holding left-click needed to complete the merge, once all parts are placed.")]
    public float craftTime = 3f;
}