using UnityEngine;

/// <summary>
/// Put this above wherever items should physically drop (e.g. hovering
/// over a pallet). Call SpawnItem from your buy/sell system whenever the
/// player purchases something - it spawns that many SEPARATE individual
/// pieces (buy 3 iron ore = 3 separate ore chunks drop), each scattered
/// slightly so they don't spawn perfectly stacked, and lets physics +
/// gravity settle them onto the pallet.
/// </summary>
public class ItemSpawnPoint : MonoBehaviour
{
    [Tooltip("Where items spawn from. Position this above the pallet/container so dropped items fall onto it.")]
    [SerializeField] private Transform dropPoint;

    [Tooltip("Random horizontal spread so multiple items don't spawn stacked exactly on top of each other.")]
    [SerializeField] private float scatterRadius = 0.3f;

    [Tooltip("Small random height variance per piece, on top of Drop Point's height, so simultaneous spawns don't all start overlapping at the exact same Y.")]
    [SerializeField] private float heightJitter = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private void Reset()
    {
        if (dropPoint == null)
        {
            dropPoint = transform;
        }
    }

    /// <summary>
    /// Spawns 'amount' separate individual pieces of the given item (e.g.
    /// SpawnItem(ironOre, 5) drops 5 distinct ore chunks, not one stack of 5).
    /// </summary>
    public void SpawnItem(ItemData item, int amount)
    {
        if (item == null)
        {
            if (debugLogging) Debug.LogWarning("[ItemSpawnPoint] SpawnItem called with a null item.");
            return;
        }

        if (item.worldPrefab == null)
        {
            Debug.LogWarning($"[ItemSpawnPoint] '{item.itemName}' has no World Prefab assigned - can't spawn it physically. Set one on the ItemData asset.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            SpawnSinglePiece(item);
        }

        if (debugLogging) Debug.Log($"[ItemSpawnPoint] Spawned {amount}x separate '{item.itemName}' pieces.");
    }

    private void SpawnSinglePiece(ItemData item)
    {
        Transform origin = dropPoint != null ? dropPoint : transform;

        Vector2 scatter = Random.insideUnitCircle * scatterRadius;
        float yJitter = Random.Range(0f, heightJitter);
        Vector3 spawnPos = origin.position + new Vector3(scatter.x, yJitter, scatter.y);
        Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject instance = Instantiate(item.worldPrefab, spawnPos, spawnRot);

        WorldItem worldItem = instance.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.Initialize(item, 1);
        }
        else if (debugLogging)
        {
            Debug.LogWarning($"[ItemSpawnPoint] '{item.itemName}''s World Prefab has no WorldItem component - it'll spawn but won't be collectible.");
        }
    }
}