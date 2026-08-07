using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this alongside PlayerController. Lets the player pick up a Placeable
/// object (like the anvil), move it around as a preview that follows a
/// raycast against the ground/walls, shows red/green depending on whether
/// the spot is clear, rotate it, and confirm or cancel placement.
/// </summary>
public class BuildModeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Interaction")]
    [SerializeField] private float pickupRange = 5f;
    [SerializeField] private LayerMask placeableLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Placement Check")]
    [Tooltip("Layers considered obstacles when checking if a spot is clear (walls, props, other Placeables, etc). Also used to stop the placement raycast so the preview can't pass through solid geometry.")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float surfaceOffset = 0.02f;

    [Header("Rotation")]
    [SerializeField] private float rotationStep = 15f;

    [Header("Floor Only")]
    [Tooltip("Max angle (degrees) between the surface normal and world up for a spot to count as 'floor'. 0 = perfectly flat only, higher allows gentle slopes.")]
    [SerializeField] private float maxFloorAngle = 45f;

    private Placeable heldObject;
    private Transform heldTransform;
    private bool isOnValidFloor;

    public bool IsInBuildMode => heldObject != null;

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null)
        {
            return;
        }

        if (!IsInBuildMode)
        {
            // Not holding anything: E picks up an already-placed Placeable
            // you're looking at.
            if (keyboard.eKey.wasPressedThisFrame)
            {
                TryPickUp();
            }
        }
        else
        {
            HandleRotationInput(keyboard);
            UpdatePreviewPosition();

            bool isValid = isOnValidFloor && CheckPlacementValid();
            heldObject.SetPreviewValid(isValid);

            if (mouse.leftButton.wasPressedThisFrame && isValid)
            {
                ConfirmPlacement();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }
    }

    private void HandleRotationInput(Keyboard keyboard)
    {
        if (keyboard.qKey.wasPressedThisFrame)
        {
            heldTransform.Rotate(Vector3.up, -rotationStep, Space.World);
        }
        else if (keyboard.eKey.wasPressedThisFrame)
        {
            heldTransform.Rotate(Vector3.up, rotationStep, Space.World);
        }
    }

    private void TryPickUp()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, placeableLayer))
        {
            Placeable placeable = hit.collider.GetComponentInParent<Placeable>();
            if (placeable != null)
            {
                heldObject = placeable;
                heldTransform = placeable.transform;
                heldObject.BeginPreview();
            }
        }
    }

    private void UpdatePreviewPosition()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // Raycast against ground AND obstacles together, so aiming at a wall
        // stops the preview at that wall's surface instead of letting the
        // ray pass through it to whatever's behind.
        LayerMask surfaceMask = groundLayer | obstacleLayers;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange * 2f, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 extents = heldObject.GetBoundsExtents();
            Vector3 centerOffset = heldObject.GetBoundsCenterOffset();

            bool hitIsGround = IsInLayerMask(hit.collider.gameObject.layer, groundLayer);
            float angleFromUp = Vector3.Angle(hit.normal, Vector3.up);
            isOnValidFloor = hitIsGround && angleFromUp <= maxFloorAngle;

            Vector3 targetCenter;
            if (hitIsGround)
            {
                // Rest the object's bounds on top of the ground point,
                // regardless of angle - the angle check above is what
                // actually decides if this counts as valid floor.
                targetCenter = hit.point + Vector3.up * (extents.y + surfaceOffset);
            }
            else
            {
                // Hit a wall or other non-ground obstacle: stop just in
                // front of its surface along the hit normal so the preview
                // doesn't sink into it. It'll still show invalid, since
                // isOnValidFloor is false here.
                float pushOut = Mathf.Abs(extents.x * hit.normal.x)
                               + Mathf.Abs(extents.y * hit.normal.y)
                               + Mathf.Abs(extents.z * hit.normal.z)
                               + surfaceOffset;
                targetCenter = hit.point + hit.normal * pushOut;
            }

            heldTransform.position = targetCenter - centerOffset;
        }
        else
        {
            isOnValidFloor = false;
        }
    }

    private bool CheckPlacementValid()
    {
        Vector3 extents = heldObject.GetBoundsExtents();
        Vector3 centerOffset = heldObject.GetBoundsCenterOffset();
        Vector3 checkCenter = heldTransform.position + centerOffset;

        Collider[] overlaps = Physics.OverlapBox(
            checkCenter,
            extents,
            heldTransform.rotation,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        return overlaps.Length == 0;
    }

    private void ConfirmPlacement()
    {
        heldObject.EndPreview();
        heldObject = null;
        heldTransform = null;
    }

    private void CancelPlacement()
    {
        // Simplest cancel behavior: just drop it where it currently is,
        // as long as that spot happens to be valid floor. Swap this out for
        // "return to original position" or "destroy" if you'd rather.
        if (isOnValidFloor && CheckPlacementValid())
        {
            heldObject.EndPreview();
        }
        else
        {
            heldObject.SetPreviewValid(true); // fallback: force-restore visuals before ending
            heldObject.EndPreview();
        }

        heldObject = null;
        heldTransform = null;
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}