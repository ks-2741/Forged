using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Testing-only cheat menu. Press P to spawn Iron Ore, O to spawn Copper
/// Ore, I to spawn Cross Guard, U to spawn Grip, Y to spawn Sharp Blade -
/// via the assigned ItemSpawnPoint (e.g. the one above your pallet). Press
/// 2 to add gold directly to GameSession.BankedGold, for testing the skill
/// tree without having to actually earn/bank it first.
/// Remove this component (or leave it disabled) before shipping.
/// </summary>
public class CheatMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemSpawnPoint spawnPoint;

    [Header("Items")]
    [SerializeField] private ItemData ironOre;
    [SerializeField] private ItemData copperOre;
    [SerializeField] private ItemData crossGuard;
    [SerializeField] private ItemData grip;
    [SerializeField] private ItemData sharpBlade;

    [Header("Settings")]
    [Tooltip("How many pieces to spawn per key press.")]
    [SerializeField] private int spawnAmount = 1;

    [Header("Gold Cheat")]
    [Tooltip("How much gold Press 2 adds to GameSession.BankedGold per press.")]
    [SerializeField] private int goldAmount = 100;

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

        if (keyboard.iKey.wasPressedThisFrame)
        {
            SpawnItem(crossGuard);
        }

        if (keyboard.uKey.wasPressedThisFrame)
        {
            SpawnItem(grip);
        }

        if (keyboard.yKey.wasPressedThisFrame)
        {
            SpawnItem(sharpBlade);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            AddGold();
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

    private void AddGold()
    {
        GameSession.BankedGold += goldAmount;
        Debug.Log($"[CheatMenu] Added {goldAmount}g - bank now {GameSession.BankedGold}g.");
    }
}