using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the blueprint book's UI panel/Canvas. Dynamically builds
/// one button per Blueprint, showing crafted progress and gold cost, and
/// greying out anything not yet affordable/qualified. Clicking a button
/// attempts to learn it via BlueprintManager.
/// </summary>
public class BlueprintUI : MonoBehaviour
{
    public static BlueprintUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button blueprintButtonPrefab;

    private List<Blueprint> currentBlueprints;
    private GameObject currentInteractor;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void Populate(List<Blueprint> blueprints, GameObject interactor)
    {
        currentBlueprints = blueprints;
        currentInteractor = interactor;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (GameObject go in spawnedButtons)
        {
            Destroy(go);
        }
        spawnedButtons.Clear();

        if (currentBlueprints == null || currentInteractor == null || blueprintButtonPrefab == null || buttonContainer == null)
        {
            return;
        }

        Currency currency = currentInteractor.GetComponent<Currency>();
        CraftingStatsTracker stats = CraftingStatsTracker.Instance;

        foreach (Blueprint blueprint in currentBlueprints)
        {
            if (blueprint == null || blueprint.requiredCraftedItem == null)
            {
                continue;
            }

            Button button = Instantiate(blueprintButtonPrefab, buttonContainer);
            bool unlocked = BlueprintManager.Instance != null && BlueprintManager.Instance.IsUnlocked(blueprint);
            int crafted = stats != null ? stats.GetCraftedCount(blueprint.requiredCraftedItem) : 0;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = unlocked
                    ? $"{blueprint.blueprintName}\n(Learned)"
                    : $"{blueprint.blueprintName}\nCrafted {crafted}/{blueprint.requiredCraftedAmount} {blueprint.requiredCraftedItem.itemName}\nCost: {blueprint.goldCost}g";
            }

            bool canAfford = currency != null && currency.CanAfford(blueprint.goldCost);
            bool meetsCraftReq = crafted >= blueprint.requiredCraftedAmount;
            button.interactable = !unlocked && canAfford && meetsCraftReq;

            Blueprint captured = blueprint;
            button.onClick.AddListener(() => OnBlueprintClicked(captured));

            spawnedButtons.Add(button.gameObject);
        }
    }

    private void OnBlueprintClicked(Blueprint blueprint)
    {
        if (BlueprintManager.Instance == null || currentInteractor == null)
        {
            return;
        }

        Currency currency = currentInteractor.GetComponent<Currency>();
        BlueprintManager.Instance.TryUnlock(blueprint, currency, CraftingStatsTracker.Instance);

        // Refresh regardless of success, so progress/afford state stays current.
        Rebuild();
    }
}