using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

/// <summary>
/// Fixed-size, stack-based inventory. Add this to the player. Handles
/// stacking items up to their max stack size across multiple slots,
/// removing items (e.g. for crafting or selling), and querying counts.
/// </summary>
public class Inventory : MonoBehaviour
{
    [SerializeField] private int slotCount = 20;

    [Header("Debug")]
    [Tooltip("Shows a simple on-screen list of slot usage and item counts while playing, for testing.")]
    [SerializeField] private bool showDebugGUI = true;

    private InventorySlot[] slots;

    /// <summary>Fired whenever slot contents change, so UI can refresh.</summary>
    public event Action OnInventoryChanged;

    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = new InventorySlot();
        }
    }

    /// <summary>
    /// Adds up to 'amount' of the item, stacking into existing partial
    /// stacks first, then filling empty slots. Returns how many were
    /// actually added (may be less than requested if the inventory is full).
    /// </summary>
    public int AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;

        // Fill existing partial stacks of the same item first.
        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (slot.item == item && slot.quantity < item.maxStackSize)
            {
                int space = item.maxStackSize - slot.quantity;
                int toAdd = Mathf.Min(space, remaining);
                slot.quantity += toAdd;
                remaining -= toAdd;
            }
        }

        // Then use empty slots for whatever's left.
        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (slot.IsEmpty)
            {
                int toAdd = Mathf.Min(item.maxStackSize, remaining);
                slot.item = item;
                slot.quantity = toAdd;
                remaining -= toAdd;
            }
        }

        int added = amount - remaining;
        if (added > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        return added;
    }

    /// <summary>
    /// Removes up to 'amount' of the item, across as many slots as needed.
    /// Returns true only if the full amount was available and removed.
    /// </summary>
    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        if (GetItemCount(item) < amount)
        {
            return false;
        }

        int remaining = amount;
        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (slot.item == item)
            {
                int toRemove = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= toRemove;
                remaining -= toRemove;

                if (slot.quantity <= 0)
                {
                    slot.Clear();
                }
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetItemCount(ItemData item)
    {
        int total = 0;
        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item)
            {
                total += slot.quantity;
            }
        }
        return total;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    public bool HasFreeSpaceFor(ItemData item, int amount)
    {
        int spaceAvailable = 0;

        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item && slot.quantity < item.maxStackSize)
            {
                spaceAvailable += item.maxStackSize - slot.quantity;
            }
            else if (slot.IsEmpty)
            {
                spaceAvailable += item.maxStackSize;
            }

            if (spaceAvailable >= amount)
            {
                return true;
            }
        }

        return spaceAvailable >= amount;
    }

    private void OnGUI()
    {
        if (!showDebugGUI || slots == null)
        {
            return;
        }

        int usedSlots = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty)
            {
                usedSlots++;
            }
        }

        float width = 220f;
        float lineHeight = 20f;
        float height = lineHeight * (usedSlots + 2) + 10f;

        GUI.Box(new Rect(10, 10, width, height), "");
        GUILayout.BeginArea(new Rect(20, 15, width - 20, height - 10));

        GUILayout.Label($"Inventory: {usedSlots}/{slotCount} slots used");

        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty)
            {
                GUILayout.Label($"  {slot.item.itemName}: {slot.quantity}/{slot.item.maxStackSize}");
            }
        }

        GUILayout.EndArea();
    }
}