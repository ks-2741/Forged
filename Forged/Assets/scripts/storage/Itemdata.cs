using UnityEngine;



public enum ItemCategory
{
    Ore,
    Ingot,
    CraftingMaterial,
    Tool,
    Equipment,
    Misc
}

/// <summary>
/// Data-only definition of an item type. Create one asset per item via
/// Assets > Create > Inventory > Item (e.g. "Iron Ore", "Copper Ore").
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName = "New Item";
    [TextArea] public string description;
    public Sprite icon;
    public ItemCategory category = ItemCategory.Ore;

    [Header("Stacking")]
    [Tooltip("Max amount of this item allowed in a single inventory slot.")]
    public int maxStackSize = 99;

    [Header("Economy")]
    [Tooltip("Base sell value. Used later by the shop/sell system.")]
    public int sellValue = 1;

    [Header("World Representation")]
    [Tooltip("Physical prefab spawned into the world for this item (e.g. an ore chunk with a WorldItem component, Rigidbody, and Collider). Used by ItemSpawnPoint when the item is bought/dropped physically rather than added straight to an inventory.")]
    public GameObject worldPrefab;
}