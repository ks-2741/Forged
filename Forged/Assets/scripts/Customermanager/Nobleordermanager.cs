using  System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent singleton tracking every active noble commission. Listens to
/// DayNightCycle.onDayStart to count down each order's Days Remaining -
/// once an order hits 0, OnOrderReadyForDelivery fires so NobleManager can
/// spawn the noble's return visit. Put this on a persistent manager object
/// alongside your other singletons (BlueprintManager, CraftingStatsTracker).
/// </summary>
public class NobleOrderManager : MonoBehaviour
{
    public static NobleOrderManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Used to count down each order's Days Remaining - one day passes per onDayStart.")]
    [SerializeField] private DayNightCycle dayNightCycle;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private readonly List<NobleOrder> activeOrders = new List<NobleOrder>();
    private int nextOrderId = 1;

    /// <summary>Fired the moment an order's countdown reaches 0 (the day it becomes due).</summary>
    public event System.Action<NobleOrder> OnOrderReadyForDelivery;

    public IReadOnlyList<NobleOrder> ActiveOrders => activeOrders;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.onDayStart.AddListener(HandleDayStart);
        }
    }

    private void OnDisable()
    {
        if (dayNightCycle != null)
        {
            dayNightCycle.onDayStart.RemoveListener(HandleDayStart);
        }
    }

    /// <summary>True if a noble order is currently placed and not yet finished (waiting OR ready for delivery).</summary>
    public bool HasActiveOrder()
    {
        return activeOrders.Count > 0;
    }

    /// <summary>Called by NobleManager the moment a noble places a new commission.</summary>
    public NobleOrder CreateOrder(List<NobleOrderLine> lines, int totalDays, int payout)
    {
        NobleOrder order = new NobleOrder
        {
            id = nextOrderId++,
            lines = lines,
            delivered = new List<int>(new int[lines.Count]),
            daysRemaining = Mathf.Max(1, totalDays),
            totalPayout = payout,
            readyForDelivery = false
        };

        activeOrders.Add(order);

        if (debugLogging) Debug.Log($"[NobleOrderManager] Created order #{order.id} - due in {order.daysRemaining} day(s), payout {payout}g.");

        return order;
    }

    private void HandleDayStart()
    {
        foreach (NobleOrder order in activeOrders)
        {
            if (order.readyForDelivery)
            {
                continue;
            }

            order.daysRemaining--;
            if (debugLogging) Debug.Log($"[NobleOrderManager] Order #{order.id} - {order.daysRemaining} day(s) remaining.");

            if (order.daysRemaining <= 0)
            {
                order.readyForDelivery = true;
                if (debugLogging) Debug.Log($"[NobleOrderManager] Order #{order.id} is now due.");
                OnOrderReadyForDelivery?.Invoke(order);
            }
        }
    }

    /// <summary>Called by NobleCustomer once an order is fully delivered and paid out.</summary>
    public void CompleteOrder(NobleOrder order)
    {
        if (activeOrders.Remove(order) && debugLogging)
        {
            Debug.Log($"[NobleOrderManager] Order #{order.id} completed.");
        }
    }

    /// <summary>Called by NobleCustomer if the noble leaves without full delivery (e.g. ran out of patience).</summary>
    public void AbandonOrder(NobleOrder order)
    {
        if (activeOrders.Remove(order) && debugLogging)
        {
            Debug.Log($"[NobleOrderManager] Order #{order.id} abandoned - not fully delivered.");
        }
    }
}