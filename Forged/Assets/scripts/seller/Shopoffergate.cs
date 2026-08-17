using UnityEngine;

/// <summary>
/// Put this directly on a Buy button (or a wrapper GameObject around it)
/// in your shop UI. The button/target is completely hidden until the
/// assigned Blueprint has been learned. Re-checks automatically every
/// time the shop panel becomes active (since that re-triggers OnEnable
/// on everything inside it), so it always reflects current unlock state
/// without needing to be wired up manually elsewhere.
/// </summary>
public class ShopOfferGate : MonoBehaviour
{
    [Tooltip("The button stays hidden until this Blueprint is learned. Leave empty to always show it.")]
    [SerializeField] private Blueprint requiredBlueprint;

    [Tooltip("What to show/hide. Leave empty to use this GameObject itself.")]
    [SerializeField] private GameObject target;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private void OnEnable()
    {
        if (debugLogging) Debug.Log($"[ShopOfferGate] '{name}' OnEnable fired - refreshing.");
        Refresh();
    }

    public void Refresh()
    {
        if (requiredBlueprint == null)
        {
            if (debugLogging) Debug.Log($"[ShopOfferGate] '{name}' has no Required Blueprint assigned - always shown.");
            SetActive(true);
            return;
        }

        if (BlueprintManager.Instance == null)
        {
            if (debugLogging) Debug.LogWarning($"[ShopOfferGate] '{name}': BlueprintManager.Instance is NULL. Is there a BlueprintManager in the scene, and only one?");
            SetActive(false);
            return;
        }

        bool unlocked = BlueprintManager.Instance.IsUnlocked(requiredBlueprint);

        if (debugLogging)
        {
            Debug.Log($"[ShopOfferGate] '{name}' checking blueprint '{requiredBlueprint.blueprintName}' " +
                      $"(instance ID {requiredBlueprint.GetInstanceID()}) against BlueprintManager " +
                      $"(instance ID {BlueprintManager.Instance.GetInstanceID()}) -> unlocked = {unlocked}");
        }

        SetActive(unlocked);
    }

    private void SetActive(bool active)
    {
        GameObject toToggle = target != null ? target : gameObject;

        if (debugLogging) Debug.Log($"[ShopOfferGate] '{name}' setting '{toToggle.name}' active = {active}.");

        toToggle.SetActive(active);
    }
}