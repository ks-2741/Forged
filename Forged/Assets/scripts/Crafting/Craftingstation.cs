using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on any crafting equipment (furnace, anvil, etc.) alongside
/// Placeable. Left-clicking it (via PlayerInteractor) opens CraftingUI
/// showing this station's recipe list. Handles removing input items,
/// waiting the craft time, and granting output items - the actual
/// "formula" every piece of equipment can reuse.
/// </summary>
public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Furnace";
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public string StationName => stationName;
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;
    public bool IsCrafting { get; private set; }

    /// <summary>0-1 progress of the current craft, for a UI progress bar. 0 when idle.</summary>
    public float CraftProgress01 { get; private set; }

    private Inventory activeInventory;

    public void Interact(GameObject interactor)
    {
        Inventory playerInventory = interactor.GetComponent<Inventory>();
        if (playerInventory == null)
        {
            if (debugLogging) Debug.LogWarning($"[CraftingStation] '{interactor.name}' has no Inventory component.");
            return;
        }

        if (CraftingUI.Instance == null)
        {
            Debug.LogWarning("[CraftingStation] No CraftingUI found in the scene - can't open the crafting menu.");
            return;
        }

        CraftingUI.Instance.Open(this, playerInventory);
    }

    /// <summary>
    /// Attempts to start crafting the given recipe using the given
    /// inventory (the player who opened this station). Returns false if
    /// already crafting or the player doesn't have enough input.
    /// </summary>
    public bool TryStartCraft(CraftingRecipe recipe, Inventory playerInventory)
    {
        if (IsCrafting)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] '{stationName}' is already crafting - ignoring.");
            return false;
        }

        if (recipe == null || playerInventory == null)
        {
            return false;
        }

        if (!playerInventory.HasItem(recipe.inputItem, recipe.inputAmount))
        {
            if (debugLogging) Debug.Log($"[CraftingStation] Not enough '{recipe.inputItem.itemName}' - need {recipe.inputAmount}, have {playerInventory.GetItemCount(recipe.inputItem)}.");
            return false;
        }

        playerInventory.RemoveItem(recipe.inputItem, recipe.inputAmount);
        activeInventory = playerInventory;
        StartCoroutine(CraftRoutine(recipe));
        return true;
    }

    private IEnumerator CraftRoutine(CraftingRecipe recipe)
    {
        IsCrafting = true;
        CraftProgress01 = 0f;
        if (debugLogging) Debug.Log($"[CraftingStation] Started smelting '{recipe.inputItem.itemName}' -> '{recipe.outputItem.itemName}' ({recipe.craftTime}s).");

        float elapsed = 0f;
        while (elapsed < recipe.craftTime)
        {
            elapsed += Time.deltaTime;
            CraftProgress01 = Mathf.Clamp01(elapsed / recipe.craftTime);
            yield return null;
        }

        if (activeInventory != null)
        {
            int added = activeInventory.AddItem(recipe.outputItem, recipe.outputAmount);
            if (debugLogging) Debug.Log($"[CraftingStation] Finished. Gave {added}x '{recipe.outputItem.itemName}'.");

            if (added < recipe.outputAmount && debugLogging)
            {
                Debug.LogWarning($"[CraftingStation] Player inventory was full - only {added}/{recipe.outputAmount} '{recipe.outputItem.itemName}' fit. The rest was lost.");
            }
        }

        IsCrafting = false;
        CraftProgress01 = 0f;
    }
}