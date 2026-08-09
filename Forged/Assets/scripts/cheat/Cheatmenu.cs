using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Testing-only cheat menu. Press P to spawn Iron Ore, O to spawn Copper
/// Ore, via the assigned ItemSpawnPoint (e.g. the one above your pallet).
/// Remove this component (or leave it disabled) before shipping.
/// </summary>
public class CheatMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemSpawnPoint spawnPoint;

    [Header("Items")]
    [SerializeField] private ItemData ironOre;
    [SerializeField] private ItemData copperOre;

    [Header("Settings")]
    [Tooltip("How many pieces to spawn per key press.")]
    [SerializeField] private int spawnAmount = 1;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.pKey.wasPressedThisFrame)
        {
            SpawnItem(ironOre);
        }

        if (keyboard.oKey.wasPressedThisFrame)
        {
            SpawnItem(copperOre);
        }
    }

    private void SpawnItem(ItemData item)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("[CheatMenu] No Item Spawn Point assigned - can't spawn anything.");
            return;
        }

        if (item == null)
        {
            Debug.LogWarning("[CheatMenu] Tried to spawn an item, but its ItemData field is empty in the Inspector.");
            return;
        }

        spawnPoint.SpawnItem(item, spawnAmount);
        Debug.Log($"[CheatMenu] Spawned {spawnAmount}x '{item.itemName}'.");
    }
}