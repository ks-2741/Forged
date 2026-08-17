using UnityEngine;

/// <summary>
/// Put this on the seller prefab alongside Placeable. SellerSpawner calls
/// Initialize right after spawning it at night, which sends it walking to
/// its Stand Point before it becomes interactable. On day start it
/// automatically walks off to the despawn point and destroys itself.
/// Left-clicking it (via PlayerInteractor, only once it's arrived at its
/// stand point, and only at night) toggles YOUR existing Shop Panel
/// open/closed. Wire your UI buttons directly to this component's public
/// methods in their OnClick() list:
///   - Buy buttons: call BuyItem(offer), with the specific ShopOffer asset
///     assigned in the Inspector per-button.
///   - A Sell button: call SellHeldItem() (no argument needed) - sells
///     whatever the player is currently holding.
/// </summary>
public class SellerStation : MonoBehaviour, IInteractable
{
    private enum State
    {
        Entering,
        Standing,
        Leaving
    }

    /// <summary>So PlayerInteractor/BuildModeController/PlayerController can check if the shop is open, same pattern as other UI panels.</summary>
    public static SellerStation Instance { get; private set; }

    [Header("References")]
    [Tooltip("Set by SellerSpawner at spawn time - not serialized here, so the prefab itself never stores scene references.")]
    private DayNightCycle dayNightCycle;
    private GameObject shopPanel;
    private ItemSpawnPoint deliveryPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private GameObject activePlayer;
    private Transform standPoint;
    private Transform despawnPoint;
    private State state;

    public bool IsShopOpen { get; private set; }
    public bool HasArrived => state == State.Standing;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (dayNightCycle != null)
        {
            dayNightCycle.onDayStart.RemoveListener(HandleDayStart);
        }
    }

    /// <summary>
    /// Called by SellerSpawner right after Instantiate. Every scene-specific
    /// reference (DayNightCycle, the shop UI panel, the delivery point) is
    /// handed in here rather than serialized on the prefab, so the prefab
    /// asset itself never stores a link to something that only exists in
    /// one particular scene.
    /// </summary>
    public void Initialize(Transform standTarget, Transform despawnTarget, DayNightCycle dayNight, GameObject panel, ItemSpawnPoint delivery)
    {
        standPoint = standTarget;
        despawnPoint = despawnTarget;
        dayNightCycle = dayNight;
        shopPanel = panel;
        deliveryPoint = delivery;
        state = State.Entering;

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (dayNightCycle != null)
        {
            dayNightCycle.onDayStart.AddListener(HandleDayStart);
        }
    }

    private void Update()
    {
        switch (state)
        {
            case State.Entering:
                if (standPoint == null)
                {
                    state = State.Standing;
                    break;
                }
                MoveTowards(standPoint.position);
                if (HasArrivedAt(standPoint.position))
                {
                    state = State.Standing;
                    if (debugLogging) Debug.Log("[SellerStation] Arrived at stand point - open for business.");
                }
                break;

            case State.Leaving:
                if (despawnPoint == null)
                {
                    Despawn();
                    break;
                }
                MoveTowards(despawnPoint.position);
                if (HasArrivedAt(despawnPoint.position))
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

    private bool HasArrivedAt(Vector3 target)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        return Vector3.Distance(transform.position, flatTarget) <= arriveThreshold;
    }

    private void HandleDayStart()
    {
        if (state == State.Leaving)
        {
            return;
        }

        if (debugLogging) Debug.Log("[SellerStation] Day started - packing up and leaving.");

        if (IsShopOpen)
        {
            CloseShop();
        }

        state = State.Leaving;
    }

    private void Despawn()
    {
        if (debugLogging) Debug.Log("[SellerStation] Despawned.");
        Destroy(gameObject);
    }

    public void Interact(GameObject interactor)
    {
        if (dayNightCycle == null || !dayNightCycle.IsNight)
        {
            if (debugLogging) Debug.Log("[SellerStation] The seller isn't here during the day.");
            return;
        }

        if (!HasArrived)
        {
            if (debugLogging) Debug.Log("[SellerStation] The seller hasn't reached their stand yet.");
            return;
        }

        if (IsShopOpen)
        {
            CloseShop();
            return;
        }

        activePlayer = interactor;
        OpenShop();
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);

            // Explicitly refresh every gate, including ones currently hidden.
            // A hidden button's own OnEnable can never fire again on its own
            // once it's inactive (Unity only calls OnEnable when an object's
            // OWN active flag flips false->true, not just when its parent
            // panel reopens) - so the check has to be driven from here.
            ShopOfferGate[] gates = shopPanel.GetComponentsInChildren<ShopOfferGate>(true);
            foreach (ShopOfferGate gate in gates)
            {
                gate.Refresh();
            }

            if (debugLogging) Debug.Log($"[SellerStation] Refreshed {gates.Length} ShopOfferGate(s) on open.");
        }

        IsShopOpen = true;
        SetCursorUnlocked(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        IsShopOpen = false;
        SetCursorUnlocked(false);
    }

    /// <summary>Assign a specific ShopOffer per-button in the Inspector's OnClick() list.</summary>
    public void BuyItem(ShopOffer offer)
    {
        if (offer == null || offer.item == null)
        {
            if (debugLogging) Debug.LogWarning("[SellerStation] BuyItem called with an empty offer.");
            return;
        }

        if (offer.requiredBlueprint != null && (BlueprintManager.Instance == null || !BlueprintManager.Instance.IsUnlocked(offer.requiredBlueprint)))
        {
            if (debugLogging) Debug.Log($"[SellerStation] '{offer.item.itemName}' isn't available yet - learn '{offer.requiredBlueprint.blueprintName}' from the blueprint book first.");
            return;
        }

        if (activePlayer == null)
        {
            if (debugLogging) Debug.LogWarning("[SellerStation] No player reference - shop wasn't opened properly.");
            return;
        }

        Currency currency = activePlayer.GetComponent<Currency>();
        if (currency == null)
        {
            if (debugLogging) Debug.LogWarning("[SellerStation] No Currency found on the player.");
            return;
        }

        if (!currency.TrySpend(offer.price))
        {
            if (debugLogging) Debug.Log($"[SellerStation] Can't afford '{offer.item.itemName}' ({offer.price}g).");
            return;
        }

        if (deliveryPoint != null)
        {
            deliveryPoint.SpawnItem(offer.item, 1);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[SellerStation] No Delivery Point assigned - money spent but nothing spawned.");
        }

        if (debugLogging) Debug.Log($"[SellerStation] Bought '{offer.item.itemName}' for {offer.price}g.");
    }

    /// <summary>Wire a Sell button directly to this - no argument needed, sells whatever's currently held.</summary>
    public void SellHeldItem()
    {
        if (activePlayer == null)
        {
            if (debugLogging) Debug.LogWarning("[SellerStation] No player reference - shop wasn't opened properly.");
            return;
        }

        Inventory hand = activePlayer.GetComponent<Inventory>();
        if (hand == null || !hand.IsHolding)
        {
            if (debugLogging) Debug.Log("[SellerStation] Not holding anything to sell.");
            return;
        }

        ItemData item = hand.HeldItem;
        if (item.sellValue <= 0)
        {
            if (debugLogging) Debug.Log($"[SellerStation] '{item.itemName}' can't be sold here.");
            return;
        }

        hand.ConsumeHeld();

        Currency currency = activePlayer.GetComponent<Currency>();
        if (currency != null)
        {
            currency.Add(item.sellValue);
        }

        if (debugLogging) Debug.Log($"[SellerStation] Sold '{item.itemName}' for {item.sellValue}g.");
    }

    private void SetCursorUnlocked(bool unlocked)
    {
        Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlocked;
    }
}