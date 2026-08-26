using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on an empty GameObject, similar to CustomerManager, but for
/// special multi-item Noble commissions. Periodically spawns a noble
/// (day-only, same gating as regular customers) who places a multi-item
/// order via NobleOrderManager, then leaves. Once that order's day
/// countdown hits 0 (NobleOrderManager.OnOrderReadyForDelivery), this
/// automatically spawns the noble back at the stand to collect the
/// finished items and pay out.
///
/// Only ONE noble order is active at a time in this version - a new
/// order-placing visit won't spawn while one is still pending or awaiting
/// delivery.
/// </summary>
public class NobleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private NobleOrderManager orderManager;
    [SerializeField] private GameObject noblePrefab;
    [Tooltip("Where the noble walks in from.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Where the noble stands - both to place the order and later to collect it.")]
    [SerializeField] private Transform standSlot;
    [Tooltip("Where the noble's payment coin pile appears once the order is fully delivered.")]
    [SerializeField] private Transform moneyDropPoint;
    [Tooltip("Where the noble walks off to before being destroyed.")]
    [SerializeField] private Transform despawnPoint;

    [Header("Spawning (placing a NEW order)")]
    [SerializeField] private float minSpawnInterval = 120f;
    [SerializeField] private float maxSpawnInterval = 240f;

    [System.Serializable]
    public class NobleOrderTemplate
    {
        public List<NobleOrderLine> lines = new List<NobleOrderLine>();
        [Tooltip("How many in-game days the player gets before the noble returns.")]
        public int daysToComplete = 5;
        public int payout = 100;
        [Tooltip("Leave EMPTY for a template that's always available. If set, this template only gets picked once that Blueprint is unlocked - e.g. don't offer an all-iron order before Iron Sword is learned.")]
        public Blueprint requiredBlueprint;
    }

    [Header("Order Templates")]
    [Tooltip("Possible multi-item commissions - one is picked at random (filtered to unlocked-only) whenever a new noble visit spawns.")]
    [SerializeField] private List<NobleOrderTemplate> orderTemplates = new List<NobleOrderTemplate>();

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private float spawnTimer;
    private bool slotOccupied;

    private void Awake()
    {
        ResetSpawnTimer();
    }

    private void OnEnable()
    {
        if (orderManager != null)
        {
            orderManager.OnOrderReadyForDelivery += HandleOrderReadyForDelivery;
        }
    }

    private void OnDisable()
    {
        if (orderManager != null)
        {
            orderManager.OnOrderReadyForDelivery -= HandleOrderReadyForDelivery;
        }
    }

    private void Update()
    {
        if (dayNightCycle == null || !dayNightCycle.IsDay)
        {
            return;
        }

        if (slotOccupied || orderManager == null || orderManager.HasActiveOrder())
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            TrySpawnOrderVisit();
            ResetSpawnTimer();
        }
    }

    private void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void TrySpawnOrderVisit()
    {
        NobleOrderTemplate template = PickAvailableTemplate();
        if (template == null)
        {
            if (debugLogging) Debug.Log("[NobleManager] No unlocked Order Templates available - skipping noble spawn.");
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        // Deep-copy the template's lines so different orders over the
        // course of the game don't all share (and mutate) the same list.
        List<NobleOrderLine> lines = new List<NobleOrderLine>();
        foreach (NobleOrderLine line in template.lines)
        {
            lines.Add(new NobleOrderLine { item = line.item, amount = line.amount });
        }

        NobleOrder order = orderManager.CreateOrder(lines, template.daysToComplete, template.payout);

        SpawnNoble(NobleCustomer.Mode.PlacingOrder, order);
    }

    private void HandleOrderReadyForDelivery(NobleOrder order)
    {
        if (debugLogging) Debug.Log($"[NobleManager] Order #{order.id} is due - spawning noble to collect.");
        SpawnNoble(NobleCustomer.Mode.CollectingDelivery, order);
    }

    private void SpawnNoble(NobleCustomer.Mode mode, NobleOrder order)
    {
        if (!ValidateReferences())
        {
            return;
        }

        GameObject obj = Instantiate(noblePrefab, spawnPoint.position, spawnPoint.rotation);
        NobleCustomer noble = obj.GetComponent<NobleCustomer>();
        if (noble == null)
        {
            Debug.LogWarning("[NobleManager] Noble Prefab has no NobleCustomer component.");
            Destroy(obj);
            return;
        }

        slotOccupied = true;
        noble.Initialize(this, standSlot, despawnPoint, moneyDropPoint, order, mode);
    }

    /// <summary>Called by NobleCustomer once it's despawned, whichever mode it was in.</summary>
    public void ReleaseSlot()
    {
        slotOccupied = false;
    }

    private NobleOrderTemplate PickAvailableTemplate()
    {
        List<NobleOrderTemplate> available = new List<NobleOrderTemplate>();
        foreach (NobleOrderTemplate template in orderTemplates)
        {
            if (template == null || template.lines == null || template.lines.Count == 0)
            {
                continue;
            }

            bool unlocked = template.requiredBlueprint == null
                || (BlueprintManager.Instance != null && BlueprintManager.Instance.IsUnlocked(template.requiredBlueprint));

            if (unlocked)
            {
                available.Add(template);
            }
        }

        if (available.Count == 0)
        {
            return null;
        }

        return available[Random.Range(0, available.Count)];
    }

    private bool ValidateReferences()
    {
        if (noblePrefab == null || spawnPoint == null || standSlot == null || despawnPoint == null)
        {
            Debug.LogWarning("[NobleManager] Missing Noble Prefab, Spawn Point, Stand Slot, or Despawn Point.");
            return false;
        }

        if (orderManager == null)
        {
            Debug.LogWarning("[NobleManager] No Order Manager assigned.");
            return false;
        }

        return true;
    }
}