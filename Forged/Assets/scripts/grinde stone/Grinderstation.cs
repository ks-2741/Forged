using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the grinder alongside Placeable. Same interaction pattern as
/// AnvilStation: left-click while holding a blade places it on Grinder
/// Slot, then holding left-click progresses the grind (pauses if released,
/// progress is kept). On completion it flashes, then the flat blade is
/// replaced with the recipe's output (your sharp sword prefab) as a
/// normal pickup-able WorldItem. Reuses CraftingRecipe - Craft Time here
/// means seconds of holding needed, same as the anvil, scaled by
/// SkillManager.CraftSpeedMultiplier (Efficiency skill path).
///
/// Progressing requires the player to actually be looking at THIS grinder
/// while holding left-click - see AnvilStation for why. That "all at once"
/// behavior only becomes allowed once the Multitask skill is unlocked
/// (SkillManager.IsMultitaskUnlocked).
/// </summary>
public class GrinderStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string stationName = "Grinder";
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Placement")]
    [Tooltip("Empty child Transform on the grinder where the blade sits while being sharpened.")]
    [SerializeField] private Transform grinderSlot;

    [Header("Multitask Gating")]
    [Tooltip("How far the look-check reaches when deciding if the player is aimed at this grinder. Only matters until the Multitask skill is unlocked - after that, this station always progresses while left-click is held, regardless of where the player is looking.")]
    [SerializeField] private float lookRange = 8f;

    [Header("Completion")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private GameObject placedObject;
    private ItemData placedItem;
    private CraftingRecipe activeRecipe;
    private float progress;
    private bool isCompleting;

    public bool IsOccupied => placedObject != null;
    public bool IsGrinding { get; private set; }
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

        if (mouse.leftButton.isPressed && (SkillManager.IsMultitaskUnlocked || IsPlayerLookingAtThis()))
        {
            IsGrinding = true;

            float craftTime = Mathf.Max(0.01f, activeRecipe.craftTime * SkillManager.CraftSpeedMultiplier);
            progress += Time.deltaTime / craftTime;
            progress = Mathf.Clamp01(progress);

            if (progress >= 1f)
            {
                StartCoroutine(CompleteGrinding());
            }
        }
        else
        {
            // Released early, or held while not looking (and Multitask isn't
            // unlocked yet) - progress simply stays where it is.
            IsGrinding = false;
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
        if (IsOccupied)
        {
            if (debugLogging) Debug.Log($"[GrinderStation] '{stationName}' already has something on it.");
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[GrinderStation] You need to be holding a blade to use the grinder.");
            return;
        }

        CraftingRecipe recipe = FindRecipeFor(playerHand.HeldItem);
        if (recipe == null)
        {
            if (debugLogging) Debug.Log($"[GrinderStation] '{playerHand.HeldItem.itemName}' can't be sharpened here.");
            return;
        }

        if (grinderSlot == null)
        {
            Debug.LogWarning($"[GrinderStation] '{stationName}' has no Grinder Slot assigned.");
            return;
        }

        GameObject released = playerHand.ReleaseHeldTo(grinderSlot);
        if (released == null)
        {
            return;
        }

        placedObject = released;
        placedItem = recipe.inputItem;
        activeRecipe = recipe;
        progress = 0f;
        IsGrinding = false;

        if (debugLogging) Debug.Log($"[GrinderStation] Placed '{placedItem.itemName}' on the grinder. Hold left-click to sharpen.");
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

    private IEnumerator CompleteGrinding()
    {
        isCompleting = true;
        IsGrinding = false;
        if (debugLogging) Debug.Log($"[GrinderStation] Grinding complete on '{placedItem.itemName}' - finishing.");

        if (placedObject != null)
        {
            SetObjectColor(placedObject, flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        Vector3 spawnPos = grinderSlot.position;
        Quaternion spawnRot = grinderSlot.rotation;
        CraftingRecipe finishedRecipe = activeRecipe;

        if (placedObject != null)
        {
            Destroy(placedObject);
        }

        if (finishedRecipe != null && finishedRecipe.outputItem != null && finishedRecipe.outputItem.worldPrefab != null)
        {
            GameObject sword = Instantiate(finishedRecipe.outputItem.worldPrefab, spawnPos, spawnRot);
            WorldItem worldItem = sword.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.Initialize(finishedRecipe.outputItem, finishedRecipe.outputAmount);
            }
            else if (debugLogging)
            {
                Debug.LogWarning($"[GrinderStation] '{finishedRecipe.outputItem.itemName}''s World Prefab has no WorldItem component.");
            }
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[GrinderStation] Output item has no World Prefab assigned - nothing will appear.");
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