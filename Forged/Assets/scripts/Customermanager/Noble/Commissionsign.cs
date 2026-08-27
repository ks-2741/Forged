using UnityEngine;
using TMPro;

/// <summary>
/// Put this on a sign/board object in the world. Unlike BlueprintBook,
/// there's nothing to click - it just continuously shows every currently
/// active Noble commission (what's still needed, days remaining) so the
/// player can check progress anytime just by walking up and reading it,
/// same as ClockDisplay always shows the current time without needing to
/// be clicked.
///
/// Display Text can be either a UI TextMeshProUGUI (if the sign is a
/// Canvas element) or a 3D TextMeshPro (if it's a mesh/world-space text
/// object) - both inherit from TMP_Text, so either works here.
/// </summary>
public class CommissionSign : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text displayText;

    [Header("Display")]
    [SerializeField] private string noOrdersMessage = "No active commissions.";
    [Tooltip("How often (in seconds) the text rebuilds. 0 = every frame - fine for the handful of orders this game expects, raise it if you ever have many.")]
    [SerializeField] private float refreshInterval = 0f;

    private float refreshTimer;

    private void Update()
    {
        if (displayText == null)
        {
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = refreshInterval;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        var orders = NobleOrderManager.Instance != null ? NobleOrderManager.Instance.ActiveOrders : null;

        if (orders == null || orders.Count == 0)
        {
            displayText.text = noOrdersMessage;
            return;
        }

        var sb = new System.Text.StringBuilder();

        for (int o = 0; o < orders.Count; o++)
        {
            NobleOrder order = orders[o];
            sb.Append($"Commission #{order.id}\n");

            for (int i = 0; i < order.lines.Count; i++)
            {
                NobleOrderLine line = order.lines[i];
                if (line.item == null)
                {
                    continue;
                }

                int remaining = line.amount - order.delivered[i];
                sb.Append($"  {remaining}/{line.amount} {line.item.itemName}\n");
            }

            sb.Append(order.readyForDelivery
                ? "  The noble is here to collect it!"
                : $"  Due in {order.daysRemaining} day(s)");

            if (o < orders.Count - 1)
            {
                sb.Append("\n\n");
            }
        }

        displayText.text = sb.ToString();
    }
}