using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Merge time (matchedRecipe.craftTime) is scaled by
/// SkillManager.CraftSpeedMultiplier (Efficiency skill path), same as the
/// furnace/anvil/grinder.
///
/// Progressing requires the player to actually be looking at THIS merge
/// table while holding left-click - see AnvilStation for why. That "all at
/// once" behavior only becomes allowed once the Multitask skill is
/// unlocked (SkillManager.IsMultitaskUnlocked).
/// </summary>
public class MergeTable : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Merge Table";
    [SerializeField] private List<MergeRecipe> recipes = new List<MergeRecipe>();

    [System.Serializable]
    public class ItemSlot
    {
        public Transform slot;
        [Tooltip("Any ONE of these items can go in this slot (e.g. Copper Sharp Blade OR Iron Sharp Blade in the same 'blade' slot). Usually just one entry, but can be more when a slot should accept multiple tiers.")]
        public List<ItemData> acceptedItems = new List<ItemData>();
    }

    [Header("Slots")]
    [SerializeField] private ItemSlot[] itemSlots;

    [Header("Multitask Gating")]
    [Tooltip("How far the look-check reaches when deciding if the player is aimed at this merge table. Only matters until the Multitask skill is unlocked - after that, this station always progresses while left-click is held, regardless of where the player is looking.")]
    [SerializeField] private float lookRange = 8f;

    [Header("Completion")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private ItemData[] placedItems;
    private GameObject[] placedObjects;

    private MergeRecipe matchedRecipe;
    private float progress;
    private int filledCount;
    private bool isCompleting;

    public bool IsMerging { get; private set; }
    public float Progress01 => progress;
    public bool AllSlotsFilled => IsSetUp && AreAllSlotsOccupied();

    private bool IsSetUp =>
        recipes != null && recipes.Count > 0 &&
        itemSlots != null && itemSlots.Length > 0;

    private void Awake()
    {
        int count = itemSlots != null ? itemSlots.Length : 0;
        placedItems = new ItemData[count];
        placedObjects = new GameObject[count];
    }

    private void Update()
    {
        if (!AllSlotsFilled || matchedRecipe == null || isCompleting)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.isPressed && (SkillManager.IsMultitaskUnlocked || IsPlayerLookingAtThis()))
        {
            IsMerging = true;

            float craftTime = Mathf.Max(0.01f, matchedRecipe.craftTime * SkillManager.CraftSpeedMultiplier);
            progress += Time.deltaTime / craftTime;

            progress = Mathf.Clamp01(progress);

            if (progress >= 1f)
                StartCoroutine(CompleteMerge());
        }
        else
        {
            IsMerging = false;
        }
    }

    private bool IsPlayerLookingAtThis()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, lookRange) && hit.collider.transform.IsChildOf(transform);
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSetUp)
        {
            Debug.LogWarning(
                $"[MergeTable] '{stationName}' is not configured correctly.");
            return;
        }

        if (AllSlotsFilled)
        {
            if (matchedRecipe == null)
                ReturnAllParts();

            return;
        }

        Inventory inventory = interactor.GetComponent<Inventory>();

        if (inventory == null || !inventory.IsHolding)
            return;

        ItemData heldItem = inventory.HeldItem;

        if (heldItem == null)
            return;

        int slotIndex = FindSlot(heldItem);

        if (slotIndex == -1)
        {
            if (debugLogging)
                Debug.Log($"[MergeTable] No slot accepts {heldItem.itemName}.");

            return;
        }

        if (placedObjects[slotIndex] != null)
        {
            if (debugLogging)
                Debug.Log($"[MergeTable] That slot is already occupied.");

            return;
        }

        GameObject released =
            inventory.ReleaseHeldTo(itemSlots[slotIndex].slot);

        if (released == null)
            return;

        placedItems[slotIndex] = heldItem;
        placedObjects[slotIndex] = released;
        filledCount++;

        if (debugLogging)
            Debug.Log($"[MergeTable] Placed {heldItem.itemName} in slot {slotIndex}.");

        if (AllSlotsFilled)
        {
            matchedRecipe = FindMatchingRecipe();

            if (matchedRecipe != null && debugLogging)
            {
                Debug.Log(
                    $"[MergeTable] Recipe matched: {matchedRecipe.outputItem.itemName}");
            }
            else if (debugLogging)
            {
                Debug.Log("[MergeTable] No matching recipe.");
            }
        }
    }

    /// <summary>
    /// Finds the first EMPTY slot whose accepted-items list contains this
    /// item - not just any slot that could theoretically accept it, so two
    /// slots both accepting the same item won't collide with each other.
    /// </summary>
    private int FindSlot(ItemData item)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (placedObjects[i] != null)
            {
                continue; // already occupied, skip
            }

            if (itemSlots[i].acceptedItems != null && itemSlots[i].acceptedItems.Contains(item))
            {
                return i;
            }
        }

        return -1;
    }

    private bool AreAllSlotsOccupied()
    {
        for (int i = 0; i < placedObjects.Length; i++)
        {
            if (placedObjects[i] == null)
                return false;
        }

        return true;
    }

    private MergeRecipe FindMatchingRecipe()
    {
        foreach (MergeRecipe recipe in recipes)
        {
            if (recipe == null ||
                recipe.requiredItems == null ||
                recipe.requiredItems.Count != placedItems.Length)
                continue;

            List<ItemData> remaining =
                new List<ItemData>(recipe.requiredItems);

            bool match = true;

            foreach (ItemData placed in placedItems)
            {
                int index = remaining.IndexOf(placed);

                if (index == -1)
                {
                    match = false;
                    break;
                }

                remaining.RemoveAt(index);
            }

            if (match && remaining.Count == 0)
                return recipe;
        }

        return null;
    }

    private void ReturnAllParts()
    {
        for (int i = 0; i < placedObjects.Length; i++)
        {
            if (placedObjects[i] != null)
            {
                GameObject obj = placedObjects[i];
                obj.transform.SetParent(null);

                Rigidbody rb = obj.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                foreach (Collider col in
                    obj.GetComponentsInChildren<Collider>())
                {
                    col.enabled = true;
                }
            }

            placedObjects[i] = null;
            placedItems[i] = null;
        }

        filledCount = 0;
        matchedRecipe = null;
        progress = 0f;
        IsMerging = false;
    }

    private IEnumerator CompleteMerge()
    {
        isCompleting = true;
        IsMerging = false;

        MergeRecipe recipe = matchedRecipe;

        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                SetObjectColor(obj, flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        Vector3 spawnPos = itemSlots[0].slot.position;
        Quaternion spawnRot = itemSlots[0].slot.rotation;

        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        for (int i = 0; i < placedObjects.Length; i++)
        {
            placedObjects[i] = null;
            placedItems[i] = null;
        }

        if (recipe.outputItem != null &&
            recipe.outputItem.worldPrefab != null)
        {
            GameObject result = Instantiate(
                recipe.outputItem.worldPrefab,
                spawnPos,
                spawnRot
            );

            WorldItem worldItem =
                result.GetComponent<WorldItem>();

            if (worldItem != null)
            {
                worldItem.Initialize(
                    recipe.outputItem,
                    recipe.outputAmount
                );
            }

            if (CraftingStatsTracker.Instance != null)
            {
                CraftingStatsTracker.Instance.RecordCrafted(
                    recipe.outputItem,
                    recipe.outputAmount
                );
            }
        }

        filledCount = 0;
        matchedRecipe = null;
        progress = 0f;
        isCompleting = false;
    }

    private static void SetObjectColor(GameObject obj, Color color)
    {
        foreach (Renderer renderer in
            obj.GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
            }
        }
    }
}