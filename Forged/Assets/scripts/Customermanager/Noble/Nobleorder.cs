using System.Collections.Generic;

/// <summary>
/// Runtime state for one active noble commission - created by NobleManager
/// via NobleOrderManager.CreateOrder the moment a noble places their order.
/// Not a MonoBehaviour/ScriptableObject - just tracked data, held by
/// NobleOrderManager for as long as the order is active.
/// </summary>
public class NobleOrder
{
    public int id;
    public List<NobleOrderLine> lines;
    public List<int> delivered;
    public int daysRemaining;
    public int totalPayout;
    public bool readyForDelivery;

    public bool IsFullyDelivered()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (delivered[i] < lines[i].amount)
            {
                return false;
            }
        }
        return true;
    }
}