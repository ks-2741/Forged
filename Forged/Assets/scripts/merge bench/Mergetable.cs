using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the merge table alongside Placeable. Supports MULTIPLE
/// recipes - each left-click while holding a part drops it into the next
/// empty slot (order doesn't matter). Once every slot is filled, the table
/// checks which recipe (if any) matches the exact set of parts placed. If
/// one matches, holding left-click (same as the grinder) progresses the
/// merge - pauses if released, progress is kept. On completion it flashes,
/// all placed parts are destroyed, and the recipe's output prefab appears
/// as a normal pickup-able WorldItem. If the parts placed don't match any
/// recipe, the next click returns them all to the world instead of losing
/// them.
/// </summary>
public class MergeTable : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Merge Table";
    [SerializeField] private List<MergeRecipe> recipes = new List<MergeRecipe>();

    [Header("Placement")]
    [Tooltip("Physical slots parts get placed into, filled in order regardless of which recipe you're going for. Every recipe's Required Items count must match this array's length.")]
    [SerializeField] private Transform[] itemSlots;

    [Header("Completion")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private ItemData[] placedItems;
    private GameObject[] placedObjects;
    private int filledCount;
    private MergeRecipe matchedRecipe;
    private float progress;
    private bool isCompleting;

    public bool IsMerging { get; private set; }
    public float Progress01 => progress;
    public bool AllSlotsFilled => IsSetUp && filledCount >= itemSlots.Length;

    private bool IsSetUp => recipes != null && recipes.Count > 0 && itemSlots != null && itemSlots.Length > 0;

    private void Awake()
    {
        int count = itemSlots != null ? itemSlots.Length : 0;
        placedItems = new ItemData[count];
        placedObjects = new GameObject[count];
    }

    private void Update()
    {
        if (!IsSetUp || !AllSlotsFilled || matchedRecipe == null || isCompleting)
        {
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            IsMerging = true;

            progress += Time.deltaTime / Mathf.Max(0.01f, matchedRecipe.craftTime);
            progress = Mathf.Clamp01(progress);

            if (progress >= 1f)
            {
                StartCoroutine(CompleteMerge());
            }
        }
        else
        {
            // Released early - progress simply stays where it is.
            IsMerging = false;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSetUp)
        {
            Debug.LogWarning($"[MergeTable] '{stationName}' isn't fully configured - check Recipes and Item Slots.");
            return;
        }

        if (AllSlotsFilled)
        {
            if (matchedRecipe != null)
            {
                if (debugLogging) Debug.Log("[MergeTable] All parts placed - hold left-click to merge.");
            }
            else
            {
                // Wrong combination - give the parts back instead of losing them.
                if (debugLogging) Debug.Log("[MergeTable] These parts don't match any recipe - returning them.");
                ReturnAllParts();
            }
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[MergeTable] You need to be holding a part to place it here.");
            return;
        }

        ItemData heldItem = playerHand.HeldItem;
        GameObject released = playerHand.ReleaseHeldTo(itemSlots[filledCount]);
        if (released == null)
        {
            return;
        }

        placedItems[filledCount] = heldItem;
        placedObjects[filledCount] = released;
        filledCount++;

        if (debugLogging) Debug.Log($"[MergeTable] Placed part {filledCount}/{itemSlots.Length}.");

        if (filledCount >= itemSlots.Length)
        {
            matchedRecipe = FindMatchingRecipe();
            if (debugLogging)
            {
                Debug.Log(matchedRecipe != null
                    ? $"[MergeTable] Parts match '{matchedRecipe.outputItem.itemName}' - hold left-click to merge."
                    : "[MergeTable] These parts don't match any recipe. Click again to return them.");
            }
        }
    }

    private MergeRecipe FindMatchingRecipe()
    {
        foreach (MergeRecipe recipe in recipes)
        {
            if (recipe == null || recipe.requiredItems.Count != placedItems.Length)
            {
                continue;
            }

            if (ItemSetsMatch(recipe.requiredItems, placedItems))
            {
                return recipe;
            }
        }
        return null;
    }

    private static bool ItemSetsMatch(List<ItemData> required, ItemData[] placed)
    {
        List<ItemData> remaining = new List<ItemData>(placed);
        foreach (ItemData req in required)
        {
            int index = remaining.IndexOf(req);
            if (index < 0)
            {
                return false;
            }
            remaining.RemoveAt(index);
        }
        return remaining.Count == 0;
    }

    /// <summary>Drops all currently placed parts back into the world with physics restored, and clears the table.</summary>
    private void ReturnAllParts()
    {
        for (int i = 0; i < placedObjects.Length; i++)
        {
            GameObject obj = placedObjects[i];
            if (obj != null)
            {
                obj.transform.SetParent(null);

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                foreach (Collider col in obj.GetComponentsInChildren<Collider>())
                {
                    col.enabled = true;
                }
            }

            placedObjects[i] = null;
            placedItems[i] = null;
        }

        filledCount = 0;
        matchedRecipe = null;
    }

    private IEnumerator CompleteMerge()
    {
        isCompleting = true;
        IsMerging = false;
        MergeRecipe recipe = matchedRecipe;
        if (debugLogging) Debug.Log($"[MergeTable] Merge complete - finishing '{recipe.outputItem.itemName}'.");

        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                SetObjectColor(obj, flashColor);
            }
        }

        yield return new WaitForSeconds(flashDuration);

        Vector3 spawnPos = itemSlots[0].position;
        Quaternion spawnRot = itemSlots[0].rotation;

        for (int i = 0; i < placedObjects.Length; i++)
        {
            if (placedObjects[i] != null)
            {
                Destroy(placedObjects[i]);
            }
            placedObjects[i] = null;
            placedItems[i] = null;
        }

        if (recipe.outputItem != null && recipe.outputItem.worldPrefab != null)
        {
            GameObject result = Instantiate(recipe.outputItem.worldPrefab, spawnPos, spawnRot);
            WorldItem worldItem = result.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.Initialize(recipe.outputItem, recipe.outputAmount);
            }
            else if (debugLogging)
            {
                Debug.LogWarning($"[MergeTable] '{recipe.outputItem.itemName}''s World Prefab has no WorldItem component.");
            }
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[MergeTable] Output item has no World Prefab assigned - nothing will appear.");
        }

        filledCount = 0;
        matchedRecipe = null;
        progress = 0f;
        isCompleting = false;
    }

    private static void SetObjectColor(GameObject obj, Color color)
    {
        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", color);
                }
            }
        }
    }
}