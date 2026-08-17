using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MergeTable : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Merge Table";
    [SerializeField] private List<MergeRecipe> recipes = new List<MergeRecipe>();

    [System.Serializable]
    public class ItemSlot
    {
        public Transform slot;
        public ItemData requiredItem;
    }

    [Header("Slots")]
    [SerializeField] private ItemSlot[] itemSlots;

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

        if (Mouse.current.leftButton.isPressed)
        {
            IsMerging = true;

            progress += Time.deltaTime /
                        Mathf.Max(0.01f, matchedRecipe.craftTime);

            progress = Mathf.Clamp01(progress);

            if (progress >= 1f)
                StartCoroutine(CompleteMerge());
        }
        else
        {
            IsMerging = false;
        }
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
                Debug.Log($"[MergeTable] No slot for {heldItem.itemName}.");

            return;
        }

        if (placedObjects[slotIndex] != null)
        {
            if (debugLogging)
                Debug.Log($"[MergeTable] {heldItem.itemName} slot is occupied.");

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
            Debug.Log($"[MergeTable] Placed {heldItem.itemName}.");

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

    private int FindSlot(ItemData item)
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].requiredItem == item)
                return i;
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