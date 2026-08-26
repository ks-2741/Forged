using UnityEngine;

/// <summary>
/// Put this on the noble prefab. Spawned twice per commission by
/// NobleManager: once in PlacingOrder mode (walks in, announces a
/// multi-item order, leaves), and once in CollectingDelivery mode (walks
/// in days later, waits at the stand while the player hands over each
/// required item one at a time via repeated left-clicks - only one item
/// can be held at once, same as everywhere else in the game - then pays
/// out and leaves once every line is fully delivered).
/// </summary>
public class NobleCustomer : MonoBehaviour, IInteractable
{
    public enum Mode
    {
        PlacingOrder,
        CollectingDelivery
    }

    private enum State
    {
        WalkingToSlot,
        Presenting,
        Waiting,
        Leaving
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("Order Display")]
    [Tooltip("Child TextMesh positioned above the noble's head.")]
    [SerializeField] private TextMesh orderText;
    [Tooltip("How long the noble stands and shows the order text before leaving, when Placing an order.")]
    [SerializeField] private float presentDuration = 4f;
    [Tooltip("How long the noble waits at the stand for delivery before giving up, when Collecting.")]
    [SerializeField] private float patienceSeconds = 60f;

    [Header("Payment")]
    [Tooltip("Physical coin pile prefab (needs a MoneyPickup component) spawned once the full order is delivered.")]
    [SerializeField] private GameObject moneyPickupPrefab;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private NobleManager manager;
    private Transform standSlot;
    private Transform despawnPoint;
    private Transform moneyDropPoint;
    private NobleOrder order;
    private Mode mode;
    private State state;
    private float timer;
    private Camera mainCamera;

    public NobleOrder Order => order;

    /// <summary>Called by NobleManager right after Instantiate, for BOTH visits.</summary>
    public void Initialize(NobleManager owningManager, Transform slot, Transform leavePoint, Transform dropPoint, NobleOrder activeOrder, Mode startMode)
    {
        manager = owningManager;
        standSlot = slot;
        despawnPoint = leavePoint;
        moneyDropPoint = dropPoint;
        order = activeOrder;
        mode = startMode;
        state = State.WalkingToSlot;
        mainCamera = Camera.main;

        if (orderText != null)
        {
            orderText.text = "";
        }
    }

    private void Update()
    {
        if (orderText != null && mainCamera != null)
        {
            orderText.transform.forward = orderText.transform.position - mainCamera.transform.position;
        }

        switch (state)
        {
            case State.WalkingToSlot:
                MoveTowards(standSlot.position);
                if (HasArrived(standSlot.position))
                {
                    Arrive();
                }
                break;

            case State.Presenting:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (debugLogging) Debug.Log($"[NobleCustomer] Order #{order.id} placed - leaving until it's due.");
                    Leave();
                }
                break;

            case State.Waiting:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (debugLogging) Debug.Log($"[NobleCustomer] Ran out of patience waiting on order #{order.id}.");
                    if (NobleOrderManager.Instance != null)
                    {
                        NobleOrderManager.Instance.AbandonOrder(order);
                    }
                    Leave();
                }
                break;

            case State.Leaving:
                MoveTowards(despawnPoint.position);
                if (HasArrived(despawnPoint.position))
                {
                    Despawn();
                }
                break;
        }
    }

    private void MoveTowards(Vector3 target)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);

        Vector3 direction = flatTarget - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private bool HasArrived(Vector3 target)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        return Vector3.Distance(transform.position, flatTarget) <= arriveThreshold;
    }

    private void Arrive()
    {
        if (mode == Mode.PlacingOrder)
        {
            state = State.Presenting;
            timer = presentDuration;
            RefreshOrderText();
            if (debugLogging) Debug.Log($"[NobleCustomer] Presenting order #{order.id}.");
        }
        else
        {
            state = State.Waiting;
            timer = patienceSeconds;
            RefreshOrderText();
            if (debugLogging) Debug.Log($"[NobleCustomer] Here to collect order #{order.id}.");
        }
    }

    private void RefreshOrderText()
    {
        if (orderText == null || order == null)
        {
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append(mode == Mode.PlacingOrder ? "Commission:\n" : "I need:\n");

        for (int i = 0; i < order.lines.Count; i++)
        {
            NobleOrderLine line = order.lines[i];
            if (line.item == null)
            {
                continue;
            }

            int remaining = line.amount - order.delivered[i];
            if (mode == Mode.PlacingOrder || remaining > 0)
            {
                sb.Append($"{remaining}x {line.item.itemName}\n");
            }
        }

        if (mode == Mode.PlacingOrder)
        {
            sb.Append($"Back in {order.daysRemaining} day(s)");
        }

        orderText.text = sb.ToString();
    }

    public void Interact(GameObject interactor)
    {
        if (mode != Mode.CollectingDelivery || state != State.Waiting)
        {
            if (debugLogging) Debug.Log("[NobleCustomer] Not currently accepting delivery.");
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[NobleCustomer] You need to be holding one of the required items.");
            return;
        }

        int lineIndex = FindOpenLineFor(playerHand.HeldItem);
        if (lineIndex < 0)
        {
            if (debugLogging) Debug.Log($"[NobleCustomer] '{playerHand.HeldItem.itemName}' isn't needed for this order (or it's already fully delivered).");
            return;
        }

        playerHand.ConsumeHeld();
        order.delivered[lineIndex]++;
        if (debugLogging) Debug.Log($"[NobleCustomer] Delivered {order.delivered[lineIndex]}/{order.lines[lineIndex].amount} '{order.lines[lineIndex].item.itemName}' for order #{order.id}.");

        RefreshOrderText();

        if (order.IsFullyDelivered())
        {
            CompleteDelivery();
        }
    }

    private int FindOpenLineFor(ItemData item)
    {
        for (int i = 0; i < order.lines.Count; i++)
        {
            if (order.lines[i].item == item && order.delivered[i] < order.lines[i].amount)
            {
                return i;
            }
        }
        return -1;
    }

    private void CompleteDelivery()
    {
        if (debugLogging) Debug.Log($"[NobleCustomer] Order #{order.id} fully delivered - paying {order.totalPayout}g.");

        SpawnMoneyDrop(order.totalPayout);

        if (NobleOrderManager.Instance != null)
        {
            NobleOrderManager.Instance.CompleteOrder(order);
        }

        if (orderText != null)
        {
            orderText.text = "Excellent work!";
        }

        Leave();
    }

    private void SpawnMoneyDrop(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (moneyPickupPrefab == null)
        {
            if (debugLogging) Debug.LogWarning("[NobleCustomer] No Money Pickup Prefab assigned - order completed but no money dropped.");
            return;
        }

        Transform origin = moneyDropPoint != null ? moneyDropPoint : transform;
        GameObject instance = Instantiate(moneyPickupPrefab, origin.position, origin.rotation);

        MoneyPickup pickup = instance.GetComponent<MoneyPickup>();
        if (pickup != null)
        {
            pickup.Initialize(amount);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[NobleCustomer] Money Pickup Prefab has no MoneyPickup component.");
        }
    }

    /// <summary>Sends the noble walking off to despawn, regardless of current state (unless already leaving).</summary>
    public void Leave()
    {
        if (state == State.Leaving)
        {
            return;
        }

        state = State.Leaving;
    }

    private void Despawn()
    {
        if (manager != null)
        {
            manager.ReleaseSlot();
        }
        Destroy(gameObject);
    }
}