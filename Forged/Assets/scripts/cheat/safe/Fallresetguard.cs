using UnityEngine;

/// <summary>
/// Safety net for physics objects that can fall off a counter/table edge
/// and end up stuck somewhere unreachable (behind geometry, off the map,
/// etc). Instead of periodically checking a Y position, this relies on a
/// trigger volume - place a BoxCollider (Is Trigger ON) somewhere well
/// below your floor level, spanning the whole play area, and tag it to
/// match Fall Zone Tag below (default "FallResetZone" - add that tag via
/// the Tag Manager if it doesn't exist yet). The moment this object's own
/// collider touches that trigger, it teleports back to Reset Point (or
/// wherever it started, if Reset Point is left empty) and zeroes out its
/// Rigidbody's velocity so it doesn't immediately fall again from
/// leftover momentum.
///
/// Works on ANY physics object with a Rigidbody - MoneyPickup coin piles,
/// dropped ore, finished weapons, etc. Just add this component alongside
/// whatever's already on the prefab. No change needed to that object's
/// own collider - it can stay a normal (non-trigger) physics collider,
/// Unity still fires OnTriggerEnter as long as the OTHER collider (the
/// fall zone) is a trigger.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FallResetGuard : MonoBehaviour
{
    [Header("Reset")]
    [Tooltip("Where this object teleports back to if it falls into the Fall Zone. Leave EMPTY to use wherever it started (its position on Awake) - the natural choice for something like a MoneyPickup that's already spawned exactly where it should be.")]
    [SerializeField] private Transform resetPoint;
    [Tooltip("Tag on the BoxCollider (trigger) placed below the map that catches fallen objects. Only a collider with this exact tag triggers a reset.")]
    [SerializeField] private string fallZoneTag = "FallResetZone";

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private Rigidbody rb;
    private Vector3 fallbackPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fallbackPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(fallZoneTag))
        {
            return;
        }

        ResetPosition();
    }

    private void ResetPosition()
    {
        Vector3 target = resetPoint != null ? resetPoint.position : fallbackPosition;

        transform.position = target;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (debugLogging) Debug.Log($"[FallResetGuard] '{name}' entered the fall zone - reset to {target}.");
    }
}