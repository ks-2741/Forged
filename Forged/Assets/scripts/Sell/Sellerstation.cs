using UnityEngine;

/// <summary>
/// Put this on the seller alongside Placeable. Left-clicking it (via
/// PlayerInteractor, only works at night) toggles YOUR existing Shop Panel
/// open/closed. Wire your UI buttons directly to this component's public
/// methods in their OnClick() list:
///   - Buy buttons: call BuyItem(offer), with the specific ShopOffer asset
///     assigned in the Inspector per-button.
///   - A Sell button: call SellHeldItem() (no argument needed) - sells
///     whatever the player is currently holding.
/// </summary>
public class SellerStation : MonoBehaviour, IInteractable
{
    /// <summary>So PlayerInteractor/BuildModeController can check if the shop is open, same pattern as other UI panels.</summary>
    public static SellerStation Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [Tooltip("Your existing shop UI panel - shown/hidden by this script.")]
    [SerializeField] private GameObject shopPanel;
    [Tooltip("Where purchased items physically appear (e.g. a small delivery tray next to the seller).")]
    [SerializeField] private ItemSpawnPoint deliveryPoint;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private Currency activeCurrency;
    private Inventory activeHand;

    public bool IsShopOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (dayNightCycle == null || !dayNightCycle.IsNight)
        {
            if (debugLogging) Debug.Log("[SellerStation] The seller isn't here during the day.");
            return;
        }

        if (IsShopOpen)
        {
            CloseShop();
            return;
        }

        activeCurrency = interactor.GetComponent<Currency>();
        activeHand = interactor.GetComponent<Inventory>();

        OpenShop();
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
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

        if (activeCurrency == null)
        {
            if (debugLogging) Debug.LogWarning("[SellerStation] No Currency found on the player.");
            return;
        }

        if (!activeCurrency.TrySpend(offer.price))
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
        if (activeHand == null || !activeHand.IsHolding)
        {
            if (debugLogging) Debug.Log("[SellerStation] Not holding anything to sell.");
            return;
        }

        ItemData item = activeHand.HeldItem;
        if (item.sellValue <= 0)
        {
            if (debugLogging) Debug.Log($"[SellerStation] '{item.itemName}' can't be sold here.");
            return;
        }

        activeHand.ConsumeHeld();

        if (activeCurrency != null)
        {
            activeCurrency.Add(item.sellValue);
        }

        if (debugLogging) Debug.Log($"[SellerStation] Sold '{item.itemName}' for {item.sellValue}g.");
    }

    private void SetCursorUnlocked(bool unlocked)
    {
        Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlocked;
    }
}