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

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        bool unlocked = requiredBlueprint == null
            || (BlueprintManager.Instance != null && BlueprintManager.Instance.IsUnlocked(requiredBlueprint));

        GameObject toToggle = target != null ? target : gameObject;
        toToggle.SetActive(unlocked);
    }
}