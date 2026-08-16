using UnityEngine;

/// <summary>
/// Defines one learnable tier in the blueprint book (e.g. "Iron Sword").
/// Create via Assets > Create > Blueprints > Blueprint.
/// </summary>
[CreateAssetMenu(fileName = "New Blueprint", menuName = "Blueprints/Blueprint")]
public class Blueprint : ScriptableObject
{
    [Header("Info")]
    public string blueprintName = "New Blueprint";
    [TextArea] public string description;

    [Header("Requirements")]
    [Tooltip("The item the player must have crafted (lifetime total) at the merge table.")]
    public ItemData requiredCraftedItem;
    public int requiredCraftedAmount = 15;
    [Tooltip("Gold cost to learn, spent the moment it's unlocked.")]
    public int goldCost = 50;
}