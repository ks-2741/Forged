using UnityEngine;

/// <summary>
/// Put this on the physical world prefab for an item (e.g. a single ore
/// chunk or ingot), alongside a Rigidbody and a normal (non-trigger)
/// Collider so it physically falls and rests on a surface. The player
/// looks at it and left-clicks (via PlayerInteractor) to pick it up into
/// their hand - this object itself gets re-parented there by Inventory,
/// it is NOT destroyed on pickup. Fails if the player is already holding
/// something.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;

    [Header("Hand Alignment")]
    [Tooltip("Empty child Transform, placed inside THIS item's own prefab, marking the exact spot that should align to the player's hand anchor when picked up (e.g. the grip of a sword's handle). Fixes positioning when the mesh's import pivot isn't centered. Leave empty to just snap this object's own root to the hand anchor.")]
    [SerializeField] private Transform gripPoint;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public ItemData Item => item;
    public int Amount => amount;
    public Transform GripPoint => gripPoint;

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
            if (debugLogging) Debug.LogWarning($"[WorldItem] '{name}' has no ItemData assigned - nothing to pick up.");
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null)
        {
            if (debugLogging) Debug.LogWarning($"[WorldItem] '{interactor.name}' has no Inventory (hand tracker) component.");
            return;
        }

        if (playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log($"[WorldItem] Can't pick up '{item.itemName}' - already holding '{playerHand.HeldItem.itemName}'. Only one item at a time.");
            return;
        }

        bool success = playerHand.TryPickUp(item, gameObject);
        if (debugLogging) Debug.Log(success
            ? $"[WorldItem] Picked up '{item.itemName}' into hand."
            : $"[WorldItem] Failed to pick up '{item.itemName}' - check Inventory's Hand Anchor is assigned.");
    }
}