
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the anvil alongside Placeable. Left-click while holding an
/// ingot places it on Anvil Slot. From then on, holding left-click stretches
/// it along one axis (progress persists if you let go - it just pauses).
/// On completion it flashes white, then the ingot is replaced with the
/// recipe's output item (e.g. a blade) as a normal pickup-able WorldItem.
/// Reuses CraftingRecipe - Craft Time here means seconds of holding needed.
/// </summary>
public class AnvilStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Anvil";
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Placement")]
    [Tooltip("Empty child Transform on the anvil's surface where the ingot sits while being forged.")]
    [SerializeField] private Transform anvilSlot;

    [Header("Forging")]
    [Tooltip("Which local axes stretch as progress increases (1 = stretches, 0 = stays as-is).")]
    [SerializeField] private Vector3 stretchAxisMask = new Vector3(0f, 0f, 1f);
    [Tooltip("How much longer the item gets at 100% progress (1.6 = 60% longer).")]
    [SerializeField] private float targetScaleMultiplier = 1.6f;

    [Header("Completion")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private GameObject placedObject;
    private ItemData placedItem;
    private CraftingRecipe activeRecipe;
    private Vector3 originalScale;
    private float progress;
    private bool isCompleting;

    public bool IsOccupied => placedObject != null;
    public bool IsForging { get; private set; }
    public float Progress01 => progress;

    private void Update()
    {
        if (placedObject == null || isCompleting)
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
            IsForging = true;

            progress += Time.deltaTime / Mathf.Max(0.01f, activeRecipe.craftTime);
            progress = Mathf.Clamp01(progress);

            Vector3 targetScale = new Vector3(
                originalScale.x * (stretchAxisMask.x > 0f ? targetScaleMultiplier : 1f),
                originalScale.y * (stretchAxisMask.y > 0f ? targetScaleMultiplier : 1f),
                originalScale.z * (stretchAxisMask.z > 0f ? targetScaleMultiplier : 1f)
            );

            placedObject.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);

            if (progress >= 1f)
            {
                StartCoroutine(CompleteForging());
            }
        }
        else
        {
            // Released early - progress and scale simply stay where they are.
            IsForging = false;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (IsOccupied)
        {
            if (debugLogging) Debug.Log($"[AnvilStation] '{stationName}' already has something on it.");
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[AnvilStation] You need to be holding an ingot to use the anvil.");
            return;
        }

        CraftingRecipe recipe = FindRecipeFor(playerHand.HeldItem);
        if (recipe == null)
        {
            if (debugLogging) Debug.Log($"[AnvilStation] '{playerHand.HeldItem.itemName}' can't be forged here.");
            return;
        }

        if (anvilSlot == null)
        {
            Debug.LogWarning($"[AnvilStation] '{stationName}' has no Anvil Slot assigned.");
            return;
        }

        GameObject released = playerHand.ReleaseHeldTo(anvilSlot);
        if (released == null)
        {
            return;
        }

        placedObject = released;
        placedItem = recipe.inputItem;
        activeRecipe = recipe;
        originalScale = released.transform.localScale;
        progress = 0f;
        IsForging = false;

        if (debugLogging) Debug.Log($"[AnvilStation] Placed '{placedItem.itemName}' on the anvil. Hold left-click to forge.");
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

    private IEnumerator CompleteForging()
    {
        isCompleting = true;
        IsForging = false;
        if (debugLogging) Debug.Log($"[AnvilStation] Forge complete on '{placedItem.itemName}' - finishing.");

        if (placedObject != null)
        {
            SetObjectColor(placedObject, flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        Vector3 spawnPos = anvilSlot.position;
        Quaternion spawnRot = anvilSlot.rotation;
        CraftingRecipe finishedRecipe = activeRecipe;

        if (placedObject != null)
        {
            Destroy(placedObject);
        }

        if (finishedRecipe != null && finishedRecipe.outputItem != null && finishedRecipe.outputItem.worldPrefab != null)
        {
            GameObject blade = Instantiate(finishedRecipe.outputItem.worldPrefab, spawnPos, spawnRot);
            WorldItem worldItem = blade.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.Initialize(finishedRecipe.outputItem, finishedRecipe.outputAmount);
            }
            else if (debugLogging)
            {
                Debug.LogWarning($"[AnvilStation] '{finishedRecipe.outputItem.itemName}''s World Prefab has no WorldItem component.");
            }
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[AnvilStation] Output item has no World Prefab assigned - nothing will appear.");
        }

        placedObject = null;
        placedItem = null;
        activeRecipe = null;
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