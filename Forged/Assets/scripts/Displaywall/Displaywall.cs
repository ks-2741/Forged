using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on the display wall's collider. Left-clicking it (via
/// PlayerInteractor) while holding an item mounts it in the first free
/// Slot - if every slot is already full, nothing happens (the item stays
/// in hand). Each mounted item becomes independently clickable (via an
/// auto-added DisplayedItem) so the player can retrieve it later by
/// clicking the SWORD itself rather than the wall.
/// </summary>
public class DisplayWall : MonoBehaviour, IInteractable
{
    [Tooltip("Every position an item can be displayed at.")]
    [SerializeField] private Transform[] slots;

    [Tooltip("Only items in this list can be displayed here. Leave empty to accept anything.")]
    [SerializeField] private List<ItemData> requiredItems = new List<ItemData>();

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private GameObject[] slotObjects;

    private void Awake()
    {
        EnsureSlotArray();
    }

    /// <summary>
    /// Keeps the internal tracking array in sync with the Slots array,
    /// even if Slots was resized after Awake already ran (e.g. edited in
    /// the Inspector mid-Play-mode). Preserves any already-tracked slots.
    /// </summary>
    private void EnsureSlotArray()
    {
        int expected = slots != null ? slots.Length : 0;

        if (slotObjects != null && slotObjects.Length == expected)
        {
            return;
        }

        GameObject[] resized = new GameObject[expected];
        if (slotObjects != null)
        {
            for (int i = 0; i < Mathf.Min(slotObjects.Length, expected); i++)
            {
                resized[i] = slotObjects[i];
            }
        }
        slotObjects = resized;
    }

    public void Interact(GameObject interactor)
    {
        EnsureSlotArray();
        if (debugLogging) DumpSlotState("Interact (wall clicked) - state BEFORE");

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[DisplayWall] You need to be holding something to display it here.");
            return;
        }

        if (requiredItems != null && requiredItems.Count > 0 && !requiredItems.Contains(playerHand.HeldItem))
        {
            if (debugLogging) Debug.Log($"[DisplayWall] '{playerHand.HeldItem.itemName}' can't go here - this display doesn't accept it.");
            return;
        }

        int slotIndex = FindFreeSlot();
        if (slotIndex < 0)
        {
            if (debugLogging) Debug.Log("[DisplayWall] Every slot is full.");
            return;
        }

        GameObject placed = playerHand.ReleaseHeldTo(slots[slotIndex]);
        if (placed == null)
        {
            return;
        }

        // Unlike the anvil/grinder (which keep a placed item non-interactive
        // while processing), a displayed item should be fully clickable
        // again immediately.
        foreach (Collider col in placed.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        DisplayedItem displayed = placed.GetComponent<DisplayedItem>();
        if (displayed == null)
        {
            displayed = placed.AddComponent<DisplayedItem>();
        }
        displayed.Setup(this, slotIndex);

        // Make WorldItem forward clicks to DisplayedItem instead of doing
        // its own pickup - without this, WorldItem (already on the prefab)
        // would silently win over DisplayedItem for every click, since only
        // one IInteractable can ever be resolved on a given object.
        WorldItem placedWorldItem = placed.GetComponent<WorldItem>();
        if (placedWorldItem != null)
        {
            placedWorldItem.SetMountedHandler(displayed);
        }

        slotObjects[slotIndex] = placed;

        if (debugLogging)
        {
            Debug.Log($"[DisplayWall] Displayed '{placed.name}' (instance ID {placed.GetInstanceID()}) in slot {slotIndex}.");
            DumpSlotState("Interact (wall clicked) - state AFTER");
        }
    }

    /// <summary>Called by DisplayedItem when its slot's item is clicked. Returns true if successfully picked back up.</summary>
    public bool RetrieveFromSlot(int index, GameObject interactor)
    {
        EnsureSlotArray();
        if (debugLogging) Debug.Log($"[DisplayWall] RetrieveFromSlot called with index {index}.");

        if (index < 0 || index >= slotObjects.Length || slotObjects[index] == null)
        {
            if (debugLogging) Debug.LogWarning($"[DisplayWall] RetrieveFromSlot({index}) aborted - out of range or already null.");
            return false;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[DisplayWall] Your hands are full - can't retrieve.");
            return false;
        }

        GameObject obj = slotObjects[index];
        WorldItem worldItem = obj.GetComponent<WorldItem>();
        ItemData item = worldItem != null ? worldItem.Item : null;

        if (item == null)
        {
            Debug.LogWarning("[DisplayWall] Displayed object has no WorldItem/ItemData - can't retrieve it properly.");
            return false;
        }

        bool success = playerHand.TryPickUp(item, obj);
        if (success)
        {
            if (debugLogging) Debug.Log($"[DisplayWall] Clearing slot {index} (was '{obj.name}', instance ID {obj.GetInstanceID()}).");
            slotObjects[index] = null;

            worldItem.SetMountedHandler(null);

            DisplayedItem displayed = obj.GetComponent<DisplayedItem>();
            if (displayed != null)
            {
                Destroy(displayed);
            }
        }
        else if (debugLogging)
        {
            Debug.LogWarning($"[DisplayWall] TryPickUp failed for slot {index}.");
        }

        if (debugLogging) DumpSlotState("RetrieveFromSlot - state AFTER");

        return success;
    }

    private void DumpSlotState(string label)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"[DisplayWall] {label}: ");
        for (int i = 0; i < slotObjects.Length; i++)
        {
            sb.Append($"[{i}]={(slotObjects[i] == null ? "empty" : slotObjects[i].name + "#" + slotObjects[i].GetInstanceID())} ");
        }
        Debug.Log(sb.ToString());
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < slotObjects.Length; i++)
        {
            if (slotObjects[i] == null)
            {
                return i;
            }
        }
        return -1;
    }
}