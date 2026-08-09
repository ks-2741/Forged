using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on a Canvas panel in your scene. Shows a button per recipe
/// for whichever CraftingStation was just interacted with, labelled with
/// how much of the input item the player currently has. Clicking a
/// recipe button starts that craft on the station and closes the panel.
/// Only one of these should exist in the scene (singleton via Instance).
/// </summary>
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("The panel GameObject to show/hide. Usually a direct child of this Canvas.")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text titleText;
    [Tooltip("Empty container (with a Vertical Layout Group) that recipe buttons get spawned into.")]
    [SerializeField] private Transform buttonContainer;
    [Tooltip("A Button prefab with a child Text component for the label.")]
    [SerializeField] private Button recipeButtonPrefab;

    private CraftingStation currentStation;
    private Inventory currentInventory;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Open(CraftingStation station, Inventory playerInventory)
    {
        currentStation = station;
        currentInventory = playerInventory;

        if (titleText != null)
        {
            titleText.text = station.StationName;
        }

        RebuildButtons();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        IsOpen = true;
        SetCursorUnlocked(true);
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ClearButtons();
        currentStation = null;
        currentInventory = null;
        IsOpen = false;
        SetCursorUnlocked(false);
    }

    private void RebuildButtons()
    {
        ClearButtons();

        if (currentStation == null || recipeButtonPrefab == null || buttonContainer == null)
        {
            return;
        }

        foreach (CraftingRecipe recipe in currentStation.Recipes)
        {
            if (recipe == null || recipe.inputItem == null || recipe.outputItem == null)
            {
                continue;
            }

            Button button = Instantiate(recipeButtonPrefab, buttonContainer);
            int available = currentInventory != null ? currentInventory.GetItemCount(recipe.inputItem) : 0;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{recipe.inputItem.itemName} ({available}/{recipe.inputAmount}) -> {recipe.outputItem.itemName}";
            }

            button.interactable = available >= recipe.inputAmount;

            CraftingRecipe capturedRecipe = recipe;
            button.onClick.AddListener(() => OnRecipeClicked(capturedRecipe));

            spawnedButtons.Add(button.gameObject);
        }
    }

    private void ClearButtons()
    {
        foreach (GameObject go in spawnedButtons)
        {
            Destroy(go);
        }
        spawnedButtons.Clear();
    }

    private void OnRecipeClicked(CraftingRecipe recipe)
    {
        if (currentStation == null || currentInventory == null)
        {
            return;
        }

        bool started = currentStation.TryStartCraft(recipe, currentInventory);
        if (started)
        {
            Close();
        }
        else
        {
            // Refresh in place so the button availability reflects current counts
            // (e.g. if something changed) rather than silently doing nothing.
            RebuildButtons();
        }
    }

    private void SetCursorUnlocked(bool unlocked)
    {
        Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlocked;
    }
}