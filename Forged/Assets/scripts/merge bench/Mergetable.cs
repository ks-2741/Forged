using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the merge table alongside Placeable. Each left-click while
/// holding a matching part places it into the next empty slot (Item Slots
/// must be in the same order as the recipe's Required Items). Once every
/// slot is filled, holding left-click (same as the grinder) progresses the
/// merge - pauses if released, progress is kept. On completion it flashes,
/// all placed parts are destroyed, and your finished sword prefab appears
/// as a normal pickup-able WorldItem.
/// </summary>
public class MergeTable : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Merge Table";
    [SerializeField] private MergeRecipe recipe;

    [Header("Placement")]
    [Tooltip("One Transform per required item, in the SAME ORDER as the recipe's Required Items list.")]
    [SerializeField] private Transform[] itemSlots;

    [Header("Completion")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private GameObject[] placedObjects;
    private bool[] slotFilled;
    private float progress;
    private bool isCompleting;

    public bool IsMerging { get; private set; }
    public float Progress01 => progress;
    public bool AllPartsPlaced => IsSetUp && AreAllSlotsFilled();

    private bool IsSetUp => recipe != null && itemSlots != null
        && recipe.requiredItems.Count > 0
        && recipe.requiredItems.Count == itemSlots.Length;

    private void Awake()
    {
        int count = recipe != null ? recipe.requiredItems.Count : 0;
        placedObjects = new GameObject[count];
        slotFilled = new bool[count];
    }

    private void Update()
    {
        if (!IsSetUp || !AreAllSlotsFilled() || isCompleting)
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

            progress += Time.deltaTime / Mathf.Max(0.01f, recipe.craftTime);
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
            Debug.LogWarning($"[MergeTable] '{stationName}' isn't fully configured - check Recipe and Item Slots (must match in count).");
            return;
        }

        if (AreAllSlotsFilled())
        {
            if (debugLogging) Debug.Log($"[MergeTable] All parts placed - hold left-click to merge.");
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[MergeTable] You need to be holding a part to place it here.");
            return;
        }

        int slotIndex = FindMatchingEmptySlot(playerHand.HeldItem);
        if (slotIndex < 0)
        {
            if (debugLogging) Debug.Log($"[MergeTable] '{playerHand.HeldItem.itemName}' isn't needed here (or already placed).");
            return;
        }

        GameObject released = playerHand.ReleaseHeldTo(itemSlots[slotIndex]);
        if (released == null)
        {
            return;
        }

        placedObjects[slotIndex] = released;
        slotFilled[slotIndex] = true;

        int filledCount = CountFilled();
        if (debugLogging) Debug.Log($"[MergeTable] Placed part {filledCount}/{slotFilled.Length}.");
    }

    private int FindMatchingEmptySlot(ItemData item)
    {
        for (int i = 0; i < recipe.requiredItems.Count; i++)
        {
            if (!slotFilled[i] && recipe.requiredItems[i] == item)
            {
                return i;
            }
        }
        return -1;
    }

    private bool AreAllSlotsFilled()
    {
        if (slotFilled.Length == 0)
        {
            return false;
        }

        foreach (bool filled in slotFilled)
        {
            if (!filled)
            {
                return false;
            }
        }
        return true;
    }

    private int CountFilled()
    {
        int count = 0;
        foreach (bool filled in slotFilled)
        {
            if (filled) count++;
        }
        return count;
    }

    private IEnumerator CompleteMerge()
    {
        isCompleting = true;
        IsMerging = false;
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
            slotFilled[i] = false;
        }

        if (recipe.outputItem != null && recipe.outputItem.worldPrefab != null)
        {
            GameObject sword = Instantiate(recipe.outputItem.worldPrefab, spawnPos, spawnRot);
            WorldItem worldItem = sword.GetComponent<WorldItem>();
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