using UnityEngine;

/// <summary>
/// Tracks the player's money. Add this alongside Inventory on the player.
/// The seller/shop and customer-selling systems will call Add/TrySpend on
/// this to move money in and out.
/// </summary>
public class Currency : MonoBehaviour
{
    [Header("Starting Balance")]
    [SerializeField] private int startingBalance = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private bool debugLogging = true;

    private int balance;

    public int Balance => balance;

    /// <summary>Fired whenever the balance changes, so UI can refresh.</summary>
    public event System.Action<int> OnBalanceChanged;

    private void Awake()
    {
        balance = startingBalance;
    }

    /// <summary>Adds money (e.g. a customer paying for a weapon). Amount must be positive.</summary>
    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        balance += amount;
        if (debugLogging) Debug.Log($"[Currency] +{amount} (balance: {balance})");
        OnBalanceChanged?.Invoke(balance);
    }

    /// <summary>
    /// Attempts to spend money (e.g. buying ore from the seller). Returns
    /// false and spends nothing if the balance is too low.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (balance < amount)
        {
            if (debugLogging) Debug.Log($"[Currency] Can't spend {amount} - only have {balance}.");
            return false;
        }

        balance -= amount;
        if (debugLogging) Debug.Log($"[Currency] -{amount} (balance: {balance})");
        OnBalanceChanged?.Invoke(balance);
        return true;
    }

    /// <summary>True if the player currently has at least this much.</summary>
    public bool CanAfford(int amount)
    {
        return balance >= amount;
    }

    private void OnGUI()
    {
        if (!showDebugGUI)
        {
            return;
        }

        GUI.Box(new Rect(10, 55, 220, 30), "");
        GUILayout.BeginArea(new Rect(20, 58, 200, 24));
        GUILayout.Label($"Gold: {balance}");
        GUILayout.EndArea();
    }
}