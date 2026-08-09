using UnityEngine;

/// <summary>
/// Data-only recipe: consume 'inputAmount' of inputItem, wait 'craftTime'
/// seconds, produce 'outputAmount' of outputItem. Create one asset per
/// recipe via Assets > Create > Crafting > Recipe (e.g. "Iron Ore -> Iron Ingot").
/// </summary>
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Input")]
    public ItemData inputItem;
    public int inputAmount = 1;

    [Header("Output")]
    public ItemData outputItem;
    public int outputAmount = 1;

    [Header("Timing")]
    [Tooltip("Seconds it takes to complete this craft.")]
    public float craftTime = 3f;
}