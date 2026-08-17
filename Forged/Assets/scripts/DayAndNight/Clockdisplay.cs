using UnityEngine;

/// <summary>
/// Clock display connected to the DayNightCycle.
/// The hand rotates 360 degrees around the Y axis over one full day/night cycle.
/// Clicking the clock skips time forward.
/// </summary>
public class ClockDisplay : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;

    [Tooltip("The hand's Transform.")]
    [SerializeField] private Transform hourHand;

    [Tooltip("Empty child Transform ON THE HAND marking its rotation base/pivot.")]
    [SerializeField] private Transform handAnchor;

    [Tooltip("The fixed point on the clock face the hand should visually rotate around.")]
    [SerializeField] private Transform clockCenter;

    [Header("Hand Rotation")]
    [Tooltip("Starting Y rotation of the clock hand.")]
    [SerializeField] private float startingYRotation = 0f;

    [Header("Skip")]
    [Tooltip("How many 'hours' one click skips (out of 24 per full cycle).")]
    [SerializeField] private float hoursPerClick = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private void Update()
    {
        if (dayNightCycle == null || hourHand == null)
        {
            return;
        }

        // Get the current progress of the day/night cycle.
        float angle = dayNightCycle.CycleProgress01 * 360f;

        // Rotate ONLY on the Y axis.
        // X and Z are always 0.
        hourHand.localRotation = Quaternion.Euler(
            0f,
            startingYRotation - angle,
            0f
        );

        // Keep the hand's anchor locked to the clock centre.
        if (handAnchor != null && clockCenter != null)
        {
            Vector3 correction = clockCenter.position - handAnchor.position;
            hourHand.position += correction;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (dayNightCycle == null)
        {
            Debug.LogWarning("[ClockDisplay] No Day Night Cycle assigned.");
            return;
        }

        float secondsPerHour = dayNightCycle.TotalCycleDuration / 24f;

        dayNightCycle.AdvanceTime(secondsPerHour * hoursPerClick);

        if (debugLogging)
        {
            Debug.Log($"[ClockDisplay] Skipped forward {hoursPerClick} hour(s).");
        }
    }
}