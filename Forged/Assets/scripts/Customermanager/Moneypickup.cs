using UnityEngine;



/// <summary>
/// Physical pile of coins dropped onto the counter by a Customer after a
/// completed sale. Left-clicking it (via PlayerInteractor) adds Amount to
/// the player's Currency and destroys itself.
///
/// Unlike WorldItem, this does NOT get picked into the hand via Inventory -
/// money isn't something the player carries alongside a sword, it's
/// collected straight into the balance on click.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MoneyPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int amount = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public int Amount => amount;

    /// <summary>Called by Customer right after Instantiate to set how much this pile is worth.</summary>
    public void Initialize(int newAmount)
    {
        amount = newAmount;
    }

    public void Interact(GameObject interactor)
    {
        Currency currency = interactor.GetComponent<Currency>();
        if (currency == null)
        {
            if (debugLogging) Debug.LogWarning($"[MoneyPickup] '{interactor.name}' has no Currency component - can't collect.");
            return;
        }

        currency.Add(amount);
        if (debugLogging) Debug.Log($"[MoneyPickup] Collected {amount}g.");

        Destroy(gameObject);
    }
}