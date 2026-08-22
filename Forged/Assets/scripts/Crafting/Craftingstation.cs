using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on any crafting equipment (furnace, anvil, etc.) alongside
/// Placeable. Left-clicking it while holding ORE checks recipes (filtered
/// by whichever mold is currently placed, if any) - the item is consumed,
/// a timer runs, and the output is physically spawned onto Output Tray.
/// Left-clicking while holding a MOLD (any item in Compatible Molds)
/// places it in the mold slot, making the smelted output depend on it.
/// </summary>
public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Furnace";
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Output")]
    [Tooltip("Where finished items physically appear (e.g. positioned above a tray next to the furnace).")]
    [SerializeField] private ItemSpawnPoint outputTray;

    [Header("Mold")]
    [Tooltip("Which items count as molds this station accepts (e.g. Sword Mold, Spear Mold).")]
    [SerializeField] private List<ItemData> compatibleMolds = new List<ItemData>();
    [Tooltip("Where the mold sits once placed.")]
    [SerializeField] private Transform moldSlot;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public string StationName => stationName;
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;
    public bool IsCrafting { get; private set; }

    /// <summary>0-1 progress of the current craft, for an optional in-world indicator. 0 when idle.</summary>
    public float CraftProgress01 { get; private set; }

    private GameObject placedMoldObject;
    private ItemData currentMold;

    public bool HasMold => currentMold != null;
    public ItemData CurrentMold => currentMold;

    public void Interact(GameObject interactor)
    {
        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null)
        {
            if (debugLogging) Debug.LogWarning($"[CraftingStation] '{interactor.name}' has no Inventory (hand tracker) component.");
            return;
        }

        if (!playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] You're not holding anything.");
            return;
        }

        // Placing a mold takes priority over smelting attempts.
        if (compatibleMolds.Contains(playerHand.HeldItem))
        {
            PlaceMold(playerHand);
            return;
        }

        TrySmelt(playerHand);
    }

    private void PlaceMold(Inventory playerHand)
    {
        if (HasMold)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] '{stationName}' already has a mold - retrieve it first (look at it and click).");
            return;
        }

        if (moldSlot == null)
        {
            Debug.LogWarning($"[CraftingStation] '{stationName}' has no Mold Slot assigned.");
            return;
        }

        ItemData moldItem = playerHand.HeldItem;
        GameObject released = playerHand.ReleaseHeldTo(moldSlot);
        if (released == null)
        {
            return;
        }

        foreach (Collider col in released.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        WorldItem worldItem = released.GetComponent<WorldItem>();
        MoldSlotItem moldSlotItem = released.GetComponent<MoldSlotItem>();
        if (moldSlotItem == null)
        {
            moldSlotItem = released.AddComponent<MoldSlotItem>();
        }
        moldSlotItem.Setup(this);

        // Make WorldItem forward clicks to MoldSlotItem instead of doing
        // its own pickup - same fix as the display wall, avoids WorldItem
        // (already on the prefab) silently winning over MoldSlotItem.
        if (worldItem != null)
        {
            worldItem.SetMountedHandler(moldSlotItem);
        }

        placedMoldObject = released;
        currentMold = moldItem;

        if (debugLogging) Debug.Log($"[CraftingStation] Mold '{moldItem.itemName}' placed.");
    }

    /// <summary>Called by MoldSlotItem when the placed mold is clicked. Returns true if successfully retrieved.</summary>
    public bool RetrieveMold(GameObject interactor)
    {
        if (!HasMold)
        {
            return false;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[CraftingStation] Your hands are full - can't retrieve the mold.");
            return false;
        }

        bool success = playerHand.TryPickUp(currentMold, placedMoldObject);
        if (success)
        {
            WorldItem worldItem = placedMoldObject.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.SetMountedHandler(null);
            }

            MoldSlotItem moldSlotItem = placedMoldObject.GetComponent<MoldSlotItem>();
            if (moldSlotItem != null)
            {
                Destroy(moldSlotItem);
            }

            placedMoldObject = null;
            currentMold = null;
        }

        return success;
    }

    private void TrySmelt(Inventory playerHand)
    {
        if (IsCrafting)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] '{stationName}' is already smelting something - wait for it to finish.");
            return;
        }

        CraftingRecipe matchingRecipe = FindRecipeFor(playerHand.HeldItem);
        if (matchingRecipe == null)
        {
            if (debugLogging) Debug.Log($"[CraftingStation] '{playerHand.HeldItem.itemName}' can't be smelted here (with the current mold).");
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
            if (recipe == null || recipe.inputItem != heldItem)
            {
                continue;
            }

            if (recipe.requiredMold != null && recipe.requiredMold != currentMold)
            {
                continue;
            }

            return recipe;
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