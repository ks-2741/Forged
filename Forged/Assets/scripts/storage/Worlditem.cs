using UnityEngine;

/// <summary>
/// Put this on the physical world prefab for an item (e.g. a single ore
/// chunk), alongside a Rigidbody and a normal (non-trigger) Collider so it
/// physically falls and rests on the pallet. The player looks at it and
/// left-clicks (via PlayerInteractor) to pick it up - no need to walk into
/// it. Each WorldItem represents one dropped piece.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public ItemData Item => item;
    public int Amount => amount;

    /// <summary>Called by ItemSpawnPoint right after Instantiate to configure a freshly spawned piece.</summary>
    public void Initialize(ItemData newItem, int newAmount)
    {
        item = newItem;
        amount = newAmount;
    }

    public void Interact(GameObject interactor)
    {
        if (item == null)
        {
            if (debugLogging) Debug.LogWarning($"[WorldItem] '{name}' has no ItemData assigned - nothing to collect.");
            return;
        }

        Inventory playerInventory = interactor.GetComponent<Inventory>();
        if (playerInventory == null)
        {
            if (debugLogging) Debug.LogWarning($"[WorldItem] '{interactor.name}' has no Inventory component - can't collect.");
            return;
        }

        int added = playerInventory.AddItem(item, amount);
        if (debugLogging) Debug.Log($"[WorldItem] Picked up {added}/{amount}x '{item.itemName}'.");

        if (added >= amount)
        {
            Destroy(gameObject);
        }
        else
        {
            // Inventory couldn't fit all of it - shrink to whatever's left
            // rather than destroying (rare with amount usually being 1).
            amount -= added;
        }
    }
}