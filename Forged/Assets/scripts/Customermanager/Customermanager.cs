using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on an empty GameObject in your shop area. Spawns customers at
/// Spawn Point (A) only while DayNightCycle says it's day, sends them to
/// whichever Stand Slots (B) transform is currently free, and sends any
/// still-present customers to Despawn Point (C) automatically the moment
/// night starts.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private GameObject customerPrefab;
    [Tooltip("Point A - where customers walk in from.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Point B - the available standing positions at the desk. A customer claims one of these until they leave.")]
    [SerializeField] private Transform[] standSlots;
    [Tooltip("Point C - off-screen point customers walk to before being destroyed.")]
    [SerializeField] private Transform despawnPoint;

    [Header("Spawning")]
    [SerializeField] private float minSpawnInterval = 8f;
    [SerializeField] private float maxSpawnInterval = 20f;
    [SerializeField] private int maxActiveCustomers = 3;

    [System.Serializable]
    public class OrderOption
    {
        public ItemData item;
        [Tooltip("Leave EMPTY for base-tier items customers can always order (e.g. copper gear). If set, this item is only offered as an order once BlueprintManager reports it as unlocked.")]
        public Blueprint requiredBlueprint;
    }

    [Header("Orders")]
    [Tooltip("Possible weapons a customer might order. One is picked at random per customer, filtered down to only items whose Required Blueprint (if any) is currently unlocked.")]
    [SerializeField] private List<OrderOption> possibleOrders = new List<OrderOption>();

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private bool[] slotOccupied;
    private readonly List<Customer> activeCustomers = new List<Customer>();
    private readonly List<ItemData> availableOrdersScratch = new List<ItemData>();
    private float spawnTimer;

    private void Awake()
    {
        slotOccupied = new bool[standSlots != null ? standSlots.Length : 0];
        ResetSpawnTimer();
    }

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

    private void Update()
    {
        if (dayNightCycle == null || !dayNightCycle.IsDay)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            TrySpawnCustomer();
            ResetSpawnTimer();
        }
    }

    private void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void TrySpawnCustomer()
    {
        if (activeCustomers.Count >= maxActiveCustomers)
        {
            if (debugLogging) Debug.Log("[CustomerManager] Max active customers reached - skipping spawn.");
            return;
        }

        int slotIndex = FindFreeSlot();
        if (slotIndex < 0)
        {
            if (debugLogging) Debug.Log("[CustomerManager] No free slots - skipping spawn.");
            return;
        }

        if (customerPrefab == null || spawnPoint == null || despawnPoint == null)
        {
            Debug.LogWarning("[CustomerManager] Missing Customer Prefab, Spawn Point, or Despawn Point.");
            return;
        }

        if (possibleOrders == null || possibleOrders.Count == 0)
        {
            Debug.LogWarning("[CustomerManager] No Possible Orders configured - nothing to order.");
            return;
        }

        RefreshAvailableOrders();
        if (availableOrdersScratch.Count == 0)
        {
            if (debugLogging) Debug.Log("[CustomerManager] No unlocked items available to order yet - skipping spawn.");
            return;
        }

        GameObject obj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        Customer customer = obj.GetComponent<Customer>();
        if (customer == null)
        {
            Debug.LogWarning("[CustomerManager] Customer Prefab has no Customer component.");
            Destroy(obj);
            return;
        }

        ItemData order = availableOrdersScratch[Random.Range(0, availableOrdersScratch.Count)];

        slotOccupied[slotIndex] = true;
        activeCustomers.Add(customer);
        customer.Initialize(this, standSlots[slotIndex], despawnPoint, order);

        if (debugLogging) Debug.Log($"[CustomerManager] Spawned customer ordering '{order.itemName}' at slot {slotIndex}.");
    }

    /// <summary>
    /// Rebuilds availableOrdersScratch with only the items from
    /// possibleOrders that are currently orderable: either ungated
    /// (Required Blueprint left empty) or their Required Blueprint is
    /// unlocked according to BlueprintManager.
    /// </summary>
    private void RefreshAvailableOrders()
    {
        availableOrdersScratch.Clear();

        foreach (OrderOption option in possibleOrders)
        {
            if (option == null || option.item == null)
            {
                continue;
            }

            bool unlocked = option.requiredBlueprint == null
                || (BlueprintManager.Instance != null && BlueprintManager.Instance.IsUnlocked(option.requiredBlueprint));

            if (unlocked)
            {
                availableOrdersScratch.Add(option.item);
            }
        }
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < slotOccupied.Length; i++)
        {
            if (!slotOccupied[i])
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Called by Customer once it reaches the despawn point, to free its slot and untrack it.</summary>
    public void ReleaseCustomer(Customer customer, Transform slot)
    {
        int index = System.Array.IndexOf(standSlots, slot);
        if (index >= 0)
        {
            slotOccupied[index] = false;
        }
        activeCustomers.Remove(customer);
    }

    private void HandleNightStart()
    {
        if (debugLogging) Debug.Log("[CustomerManager] Night started - sending all customers away.");

        // Copy the list since Leave() -> eventual Despawn() -> ReleaseCustomer()
        // will modify activeCustomers while we're iterating it.
        foreach (Customer customer in new List<Customer>(activeCustomers))
        {
            customer.Leave();
        }
    }
}