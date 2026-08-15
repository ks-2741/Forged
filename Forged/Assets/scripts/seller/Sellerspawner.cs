using UnityEngine;

/// <summary>
/// Put this on an empty GameObject in your shop scene. Spawns the seller
/// prefab at Spawn Point the moment night starts, walks them to Stand
/// Point, then walks them to Despawn Point and destroys them the moment
/// day starts. All scene-specific references (DayNightCycle, the shop UI
/// panel, the delivery point) live HERE on this scene object and get
/// handed to the seller at spawn time - the seller prefab itself never
/// stores any of them, which is what avoids the prefab "Type mismatch" bug.
/// </summary>
public class SellerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private GameObject sellerPrefab;
    [Tooltip("Where the seller walks in from.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Where the seller stands and can be interacted with.")]
    [SerializeField] private Transform standPoint;
    [Tooltip("Where the seller walks to before being destroyed.")]
    [SerializeField] private Transform despawnPoint;

    [Header("Shop")]
    [Tooltip("Your existing shop UI panel.")]
    [SerializeField] private GameObject shopPanel;
    [Tooltip("Where purchased items physically appear.")]
    [SerializeField] private ItemSpawnPoint deliveryPoint;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private GameObject activeSeller;

    private void OnEnable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.onNightStart.AddListener(HandleNightStart);
        }
    }

    private void OnDisable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.onNightStart.RemoveListener(HandleNightStart);
        }
    }

    private void HandleNightStart()
    {
        if (activeSeller != null)
        {
            return; // already here
        }

        if (sellerPrefab == null || spawnPoint == null || standPoint == null)
        {
            Debug.LogWarning("[SellerSpawner] Missing Seller Prefab, Spawn Point, or Stand Point.");
            return;
        }

        activeSeller = Instantiate(sellerPrefab, spawnPoint.position, spawnPoint.rotation);

        SellerStation station = activeSeller.GetComponent<SellerStation>();
        if (station != null)
        {
            station.Initialize(standPoint, despawnPoint, dayNightCycle, shopPanel, deliveryPoint);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[SellerSpawner] Seller Prefab has no SellerStation component.");
        }

        if (debugLogging) Debug.Log("[SellerSpawner] Night started - seller walking in.");
    }
}