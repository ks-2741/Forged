using UnityEngine;
using TMPro;

/// <summary>
/// Put this on a UI object with a TextMeshProUGUI component (or drag one
/// in). Displays the player's Currency balance and keeps it updated
/// automatically via Currency's OnBalanceChanged event - no polling.
/// </summary>
public class CurrencyDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Currency currency;
    [SerializeField] private TMP_Text label;

    [Tooltip("Use {0} for the number, e.g. 'Gold: {0}' or '{0}g'.")]
    [SerializeField] private string format = "Gold: {0}";

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (currency != null)
        {
            currency.OnBalanceChanged += HandleBalanceChanged;
            Refresh(currency.Balance);
        }
    }

    private void OnDisable()
    {
        if (currency != null)
        {
            currency.OnBalanceChanged -= HandleBalanceChanged;
        }
    }

    private void HandleBalanceChanged(int newBalance)
    {
        Refresh(newBalance);
    }

    private void Refresh(int balance)
    {
        if (label != null)
        {
            label.text = string.Format(format, balance);
        }
    }
}