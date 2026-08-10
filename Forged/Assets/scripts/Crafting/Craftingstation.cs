using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on any crafting equipment (furnace, anvil, etc.) alongside
/// Placeable. Left-clicking it (via PlayerInteractor) checks what the
/// player is currently holding - if it matches a recipe's input, that
/// item is consumed from their hand, a timer runs, and the output is
/// physically spawned onto Output Tray (an ItemSpawnPoint) for the player
/// to walk up and pick up separately. No menu/UI involved.
/// </summary>
public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Furnace";
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Output")]
    [Tooltip("Where finished items physically appear (e.g. positioned above a tray next to the furnace).")]
    [SerializeField] private ItemSpawnPoint outputTray;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public string StationName => stationName;
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;
    public bool IsCrafting { get; private set; }

    /// <summary>0-1 progress of the current craft, for an optional in-world indicator. 0 when idle.</summary>
    public float CraftProgress01 { get; private set; }

    public void Interact(GameObject interactor)
    {
        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null)
        {
            if (debugLogging) Debug.LogWarning($"[CraftingStation] '{interactor.name}' has no Inventory (hand tracker) component.");
            return;
        }

        if (IsCrafting)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] '{stationName}' is already smelting something - wait for it to finish.");
            return;
        }

        if (!playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] You're not holding anything to smelt.");
            return;
        }

        CraftingRecipe matchingRecipe = FindRecipeFor(playerHand.HeldItem);
        if (matchingRecipe == null)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] '{playerHand.HeldItem.itemName}' can't be smelted here.");
            return;
        }

        if (outputTray == null)
        {
            Debug.LogWarning($"[CraftingStation] '{stationName}' has no Output Tray assigned - can't spawn the result.");
            return;
        }

        playerHand.ConsumeHeld();
        StartCoroutine(CraftRoutine(matchingRecipe));
    }

    private CraftingRecipe FindRecipeFor(ItemData heldItem)
    {
        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe != null && recipe.inputItem == heldItem)
            {
                return recipe;
            }
        }
        return null;
    }

    private IEnumerator CraftRoutine(CraftingRecipe recipe)
    {
        IsCrafting = true;
        CraftProgress01 = 0f;
        if (debugLogging) Debug.Log($"[CraftingStation] Smelting '{recipe.inputItem.itemName}' -> '{recipe.outputItem.itemName}' ({recipe.craftTime}s).");

        float elapsed = 0f;
        while (elapsed < recipe.craftTime)
        {
            elapsed += Time.deltaTime;
            CraftProgress01 = Mathf.Clamp01(elapsed / recipe.craftTime);
            yield return null;
        }

        outputTray.SpawnItem(recipe.outputItem, recipe.outputAmount);
        if (debugLogging) Debug.Log($"[CraftingStation] Done - {recipe.outputAmount}x '{recipe.outputItem.itemName}' dropped on the tray.");

        IsCrafting = false;
        CraftProgress01 = 0f;
    }
}