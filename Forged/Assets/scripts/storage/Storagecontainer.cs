using UnityEngine;

/// <summary>
/// Put this alongside Placeable on a storage object (a crate/shelf). No
/// open/close state - it's always accessible. Left-clicking it (via
/// PlayerInteractor) takes items straight from its Inventory into the
/// player's Inventory.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class StorageContainer : MonoBehaviour, IInteractable
{
    [Tooltip("If set, left-click always takes this specific item first. Leave empty to just take from the first non-empty slot.")]
    [SerializeField] private ItemData preferredItem;
    [SerializeField] private int takeAmountPerClick = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private Inventory storageInventory;

    private void Awake()
    {
        storageInventory = GetComponent<Inventory>();
    }

    public void Interact(GameObject interactor)
    {
        if (debugLogging) Debug.Log($"[Storage] Interact() called by '{interactor.name}'.");

        Inventory playerInventory = interactor.GetComponent<Inventory>();
        if (playerInventory == null)
        {
            if (debugLogging) Debug.LogWarning($"[Storage] '{interactor.name}' has no Inventory component - can't receive items.");
            return;
        }

        ItemData itemToTake = preferredItem;
        if (itemToTake == null)
        {
            foreach (InventorySlot slot in storageInventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    itemToTake = slot.item;
                    break;
                }
            }

            if (debugLogging && itemToTake == null)
            {
                Debug.LogWarning($"[Storage] '{name}' has no Preferred Item set and every slot is empty - nothing to take. Add items to this storage's Inventory to test.");
            }
        }

        if (itemToTake == null)
        {
            return;
        }

        int available = storageInventory.GetItemCount(itemToTake);
        int toTake = Mathf.Min(takeAmountPerClick, available);

        if (debugLogging) Debug.Log($"[Storage] Item '{itemToTake.itemName}': {available} available, attempting to take {toTake}.");

        if (toTake <= 0)
        {
            return;
        }

        if (storageInventory.RemoveItem(itemToTake, toTake))
        {
            int added = playerInventory.AddItem(itemToTake, toTake);
            if (debugLogging) Debug.Log($"[Storage] Gave {added}x '{itemToTake.itemName}' to player.");

            if (added < toTake)
            {
                // Player's inventory couldn't fit all of it - put the rest back.
                storageInventory.AddItem(itemToTake, toTake - added);
                if (debugLogging) Debug.LogWarning($"[Storage] Player inventory was full - returned {toTake - added}x '{itemToTake.itemName}' to storage.");
            }
        }
    }
}