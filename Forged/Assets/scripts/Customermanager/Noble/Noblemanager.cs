using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on an empty GameObject, similar to CustomerManager, but for
/// special multi-item Noble commissions. Supports several commissions
/// active at once - one per entry in Stand Points. Periodically spawns a
/// noble at any free stand (day-only, same gating as regular customers)
/// who places a multi-item order via NobleOrderManager, then leaves. Once
/// that order's day countdown hits 0 (NobleOrderManager.OnOrderReadyForDelivery),
/// this automatically spawns the noble back at THEIR SPECIFIC stand to
/// collect the finished items and pay out - that stand stays reserved for
/// their order the whole time, even while they're away.
/// </summary>
public class NobleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private NobleOrderManager orderManager;
    [SerializeField] private GameObject noblePrefab;
    [Tooltip("Where nobles walk in from.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Where nobles walk off to before being destroyed.")]
    [SerializeField] private Transform despawnPoint;

    [System.Serializable]
    public class NobleStandPoint
    {
        [Tooltip("Where the noble stands - both to place the order and later to collect it.")]
        public Transform standSlot;
        [Tooltip("Where this stand's payment coin pile appears once its order is fully delivered.")]
        public Transform moneyDropPoint;
    }

    [Tooltip("One entry per concurrent commission this shop can have active at once - each stand stays reserved for its own order for the order's whole lifetime, even while the noble who placed it is away.")]
    [SerializeField] private NobleStandPoint[] standPoints;

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
    [Tooltip("Possible multi-item commissions - one is picked at random whenever a new noble visit spawns, filtered to templates that are both unlocked AND short enough to realistically finish before the level's day limit.")]
    [SerializeField] private List<NobleOrderTemplate> orderTemplates = new List<NobleOrderTemplate>();

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private float spawnTimer;
    private NobleOrder[] ordersByStand;

    private void Awake()
    {
        ordersByStand = new NobleOrder[standPoints != null ? standPoints.Length : 0];
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
        int standIndex = FindFreeStand();
        if (standIndex < 0)
        {
            if (debugLogging) Debug.Log("[NobleManager] Every stand already has an active commission - skipping spawn.");
            return;
        }

        NobleOrderTemplate template = PickAvailableTemplate();
        if (template == null)
        {
            if (debugLogging) Debug.Log("[NobleManager] No Order Template is both unlocked and short enough for the days remaining in this level - skipping noble spawn.");
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
        ordersByStand[standIndex] = order;

        SpawnNoble(NobleCustomer.Mode.PlacingOrder, order, standIndex);
    }

    private void HandleOrderReadyForDelivery(NobleOrder order)
    {
        int standIndex = FindStandForOrder(order);
        if (standIndex < 0)
        {
            if (debugLogging) Debug.LogWarning($"[NobleManager] Order #{order.id} is due but no stand is tracking it - can't spawn the collecting noble.");
            return;
        }

        if (debugLogging) Debug.Log($"[NobleManager] Order #{order.id} is due - spawning noble to collect at stand {standIndex}.");
        SpawnNoble(NobleCustomer.Mode.CollectingDelivery, order, standIndex);
    }

    private void SpawnNoble(NobleCustomer.Mode mode, NobleOrder order, int standIndex)
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

        NobleStandPoint stand = standPoints[standIndex];
        noble.Initialize(this, stand.standSlot, despawnPoint, stand.moneyDropPoint, order, mode, standIndex);
    }

    /// <summary>Called by NobleCustomer once an order is finished (completed or abandoned) to free its stand back up.</summary>
    public void ReleaseStand(int standIndex, NobleOrder order)
    {
        if (ordersByStand == null || standIndex < 0 || standIndex >= ordersByStand.Length)
        {
            return;
        }

        if (ordersByStand[standIndex] == order)
        {
            ordersByStand[standIndex] = null;
        }
    }

    private int FindFreeStand()
    {
        for (int i = 0; i < ordersByStand.Length; i++)
        {
            if (ordersByStand[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindStandForOrder(NobleOrder order)
    {
        for (int i = 0; i < ordersByStand.Length; i++)
        {
            if (ordersByStand[i] == order)
            {
                return i;
            }
        }
        return -1;
    }

    private NobleOrderTemplate PickAvailableTemplate()
    {
        // Unbounded if there's no LevelManager (e.g. testing this scene
        // standalone) so template selection isn't wrongly blocked.
        int daysRemaining = LevelManager.Instance != null ? LevelManager.Instance.DaysRemaining : int.MaxValue;

        List<NobleOrderTemplate> available = new List<NobleOrderTemplate>();
        foreach (NobleOrderTemplate template in orderTemplates)
        {
            if (template == null || template.lines == null || template.lines.Count == 0)
            {
                continue;
            }

            bool unlocked = template.requiredBlueprint == null
                || (BlueprintManager.Instance != null && BlueprintManager.Instance.IsUnlocked(template.requiredBlueprint));

            if (!unlocked)
            {
                continue;
            }

            // Leave at least 1 day of buffer AFTER the order becomes due,
            // so the returning noble actually has time to stand there and
            // collect delivery before the level itself ends and the scene
            // reloads out from under them.
            bool fitsInRemainingTime = template.daysToComplete <= daysRemaining - 1;
            if (!fitsInRemainingTime)
            {
                continue;
            }

            available.Add(template);
        }

        if (available.Count == 0)
        {
            return null;
        }

        return available[Random.Range(0, available.Count)];
    }

    private bool ValidateReferences()
    {
        if (noblePrefab == null || spawnPoint == null || despawnPoint == null)
        {
            Debug.LogWarning("[NobleManager] Missing Noble Prefab, Spawn Point, or Despawn Point.");
            return false;
        }

        if (standPoints == null || standPoints.Length == 0)
        {
            Debug.LogWarning("[NobleManager] No Stand Points configured.");
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