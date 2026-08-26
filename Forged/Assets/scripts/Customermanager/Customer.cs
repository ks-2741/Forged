using UnityEngine;

/// <summary>
/// Put this on your customer prefab. CustomerManager calls Initialize
/// right after spawning. Handles walking to its assigned stand slot,
/// popping up order text once it arrives, waiting (leaving early if it
/// runs out of patience or night falls), and walking off to despawn.
/// </summary>
public class Customer : MonoBehaviour, IInteractable
{
    private enum State
    {
        WalkingToSlot,
        Waiting,
        Leaving
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("Order Display")]
    [Tooltip("Child TextMesh positioned above the customer's head, showing what they want.")]
    [SerializeField] private TextMesh orderText;
    [Tooltip("How long a customer waits at the slot before giving up and leaving.")]
    [SerializeField] private float patienceSeconds = 20f;

    [Header("Payment")]
    [Tooltip("Physical coin pile prefab (needs a MoneyPickup component) spawned onto the counter on a successful sale, instead of adding gold straight to the player's balance.")]
    [SerializeField] private GameObject moneyPickupPrefab;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private CustomerManager manager;
    private Transform standSlot;
    private Transform despawnPoint;
    private Transform moneyDropPoint;
    private ItemData desiredItem;
    private State state;
    private float patienceTimer;
    private Camera mainCamera;

    public ItemData DesiredItem => desiredItem;
    public bool IsWaitingForOrder => state == State.Waiting;

    /// <summary>
    /// Called by CustomerManager right after Instantiate. dropPoint is this
    /// specific stand's money drop point (see CustomerManager.StandPoint) -
    /// it's passed in per-spawn rather than fixed on the prefab, since
    /// which stand a customer lands on (and therefore where their coins
    /// should appear) is only known once CustomerManager assigns a slot.
    /// </summary>
    public void Initialize(CustomerManager owningManager, Transform slot, Transform leavePoint, ItemData order, Transform dropPoint)
    {
        manager = owningManager;
        standSlot = slot;
        despawnPoint = leavePoint;
        moneyDropPoint = dropPoint;
        desiredItem = order;
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
            // Simple billboard so the text always faces the camera.
            orderText.transform.forward = orderText.transform.position - mainCamera.transform.position;
        }

        switch (state)
        {
            case State.WalkingToSlot:
                MoveTowards(standSlot.position);
                if (HasArrived(standSlot.position))
                {
                    PlaceOrder();
                }
                break;

            case State.Waiting:
                patienceTimer -= Time.deltaTime;
                if (patienceTimer <= 0f)
                {
                    if (debugLogging) Debug.Log($"[Customer] Ran out of patience waiting for '{desiredItem.itemName}'.");
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

    private void PlaceOrder()
    {
        state = State.Waiting;
        patienceTimer = patienceSeconds;

        if (orderText != null)
        {
            orderText.text = desiredItem != null ? $"I need:\n{desiredItem.itemName}" : "...";
        }

        if (debugLogging) Debug.Log($"[Customer] Placed order for '{desiredItem.itemName}'.");
    }

    /// <summary>
    /// Hook point for the future selling system: call this when the player
    /// hands over the correct item, and the customer will say thanks and leave.
    /// </summary>
    public void FulfillOrder()
    {
        if (orderText != null)
        {
            orderText.text = "Thank you!";
        }

        if (debugLogging) Debug.Log($"[Customer] Order for '{desiredItem.itemName}' fulfilled.");
        Leave();
    }

    public void Interact(GameObject interactor)
    {
        if (!IsWaitingForOrder)
        {
            if (debugLogging) Debug.Log("[Customer] Not currently waiting on an order.");
            return;
        }

        Inventory playerHand = interactor.GetComponent<Inventory>();
        if (playerHand == null || !playerHand.IsHolding)
        {
            if (debugLogging) Debug.Log($"[Customer] You need to be holding '{desiredItem.itemName}' to sell it here.");
            return;
        }

        if (playerHand.HeldItem != desiredItem)
        {
            if (debugLogging) Debug.Log($"[Customer] That's not what I asked for - I need '{desiredItem.itemName}'.");
            return;
        }

        playerHand.ConsumeHeld();

        SpawnMoneyDrop(desiredItem.sellValue);

        FulfillOrder();
    }

    /// <summary>
    /// Spawns a physical coin pile worth 'amount' at this customer's
    /// assigned Money Drop Point (or this customer's own position if none
    /// was given) for the player to walk up and click, rather than adding
    /// gold straight to their balance.
    /// </summary>
    private void SpawnMoneyDrop(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (moneyPickupPrefab == null)
        {
            if (debugLogging) Debug.LogWarning("[Customer] No Money Pickup Prefab assigned - sale completed but no money dropped.");
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
            Debug.LogWarning("[Customer] Money Pickup Prefab has no MoneyPickup component - it'll spawn but won't be collectible.");
        }
    }

    /// <summary>Sends the customer walking off to despawn, regardless of current state (unless already leaving).</summary>
    public void Leave()
    {
        if (state == State.Leaving)
        {
            return;
        }

        state = State.Leaving;

        if (orderText != null)
        {
            orderText.text = "";
        }
    }

    private void Despawn()
    {
        if (manager != null)
        {
            manager.ReleaseCustomer(this, standSlot);
        }
        Destroy(gameObject);
    }
}