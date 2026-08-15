using UnityEngine;

/// <summary>
/// Put this on a persistent object (e.g. the Canvas, or an empty manager
/// object that's never destroyed). Wire your shop UI buttons to THIS
/// component's methods instead of directly to a SellerStation instance.
///
/// Why: SellerSpawner destroys and re-instantiates the seller every
/// night/day cycle, so a button pointed directly at one specific seller
/// GameObject breaks the moment that seller despawns. ShopUIManager never
/// gets destroyed, so your button wiring stays intact forever - it just
/// forwards each call to whichever seller is currently alive
/// (SellerStation.Instance, which SellerStation keeps up to date itself).
/// </summary>
public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Wire Buy buttons here (with the ShopOffer assigned per-button) instead of directly to SellerStation.</summary>
    public void BuyItem(ShopOffer offer)
    {
        if (SellerStation.Instance == null)
        {
            if (debugLogging) Debug.Log("[ShopUIManager] No seller here right now.");
            return;
        }

        SellerStation.Instance.BuyItem(offer);
    }

    /// <summary>Wire your Sell button here instead of directly to SellerStation.</summary>
    public void SellHeldItem()
    {
        if (SellerStation.Instance == null)
        {
            if (debugLogging) Debug.Log("[ShopUIManager] No seller here right now.");
            return;
        }

        SellerStation.Instance.SellHeldItem();
    }

    /// <summary>Optional - wire a Close button here if you have one.</summary>
    public void CloseShop()
    {
        if (SellerStation.Instance != null)
        {
            SellerStation.Instance.CloseShop();
        }
    }
}