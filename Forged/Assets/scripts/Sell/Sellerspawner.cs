using UnityEngine;

/// <summary>
/// Put this on an empty GameObject. Spawns the seller prefab at Spawn
/// Point (A) the moment night starts, sends it walking to Stand Point (B)
/// via SellerStation, and does nothing at day start - SellerStation
/// handles walking itself off to Despawn Point (C) and destroying itself
/// automatically when day starts.
/// </summary>
public class SellerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private GameObject sellerPrefab;
    [Tooltip("Point A - where the seller walks in from.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Point B - where the seller stands and does business.")]
    [SerializeField] private Transform standPoint;
    [Tooltip("Point C - off-screen point the seller walks to before being destroyed.")]
    [SerializeField] private Transform despawnPoint;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

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
        if (SellerStation.Instance != null)
        {
            // A seller is already present (e.g. leftover from a fast day/night
            // toggle during testing) - don't spawn a second one.
            if (debugLogging) Debug.Log("[SellerSpawner] A seller is already present - skipping spawn.");
            return;
        }

        if (sellerPrefab == null || spawnPoint == null || standPoint == null || despawnPoint == null)
        {
            Debug.LogWarning("[SellerSpawner] Missing Seller Prefab, Spawn Point, Stand Point, or Despawn Point.");
            return;
        }

        GameObject obj = Instantiate(sellerPrefab, spawnPoint.position, spawnPoint.rotation);
        SellerStation seller = obj.GetComponent<SellerStation>();
        if (seller == null)
        {
            Debug.LogWarning("[SellerSpawner] Seller Prefab has no SellerStation component.");
            Destroy(obj);
            return;
        }

        seller.Initialize(standPoint, despawnPoint);

        if (debugLogging) Debug.Log("[SellerSpawner] Night started - seller spawned.");
    }
}