using UnityEngine;

/// <summary>
/// One purchasable listing in the seller's shop. Create via Assets >
/// Create > Shop > Offer (e.g. "Buy Grip", "Buy Cross Guard").
/// </summary>
[CreateAssetMenu(fileName = "New Offer", menuName = "Shop/Offer")]
public class ShopOffer : ScriptableObject
{
    public ItemData item;
    [Tooltip("Cost in gold to buy one of this item.")]
    public int price = 1;

    [Tooltip("If set, this offer can't be bought until this Blueprint is learned. Leave empty to always be purchasable.")]
    public Blueprint requiredBlueprint;
}