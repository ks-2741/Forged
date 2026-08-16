using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this alongside PlayerController. Press B to toggle Build Mode.
/// While Build Mode is active: left-click picks up a Placeable object
/// you're looking at, then left-click again to confirm placement (shown
/// red/green depending on whether the spot is clear and on valid floor).
/// Right-click or Escape cancels. Left-click is only used for build
/// actions while Build Mode is on - it's free for other interactions
/// (storage, customers, etc.) the rest of the time.
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

    [Header("Floor Only")]
    [Tooltip("Max angle (degrees) between the surface normal and world up for a spot to count as 'floor'. 0 = perfectly flat only, higher allows gentle slopes.")]
    [SerializeField] private float maxFloorAngle = 45f;

    [Header("Rotation")]
    [SerializeField] private float rotationStep = 15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private Placeable heldObject;
    private Transform heldTransform;
    private bool isOnValidFloor;
    private bool buildModeActive;

    /// <summary>True while carrying an object, mid-placement.</summary>
    public bool IsInBuildMode => heldObject != null;

    /// <summary>True while Build Mode is toggled on (whether or not something's currently held).</summary>
    public bool IsBuildModeActive => buildModeActive;

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null)
        {
            return;
        }

        // Don't allow any build mode input while the seller's shop is open.
        if (SellerStation.Instance != null && SellerStation.Instance.IsShopOpen)
        {
            return;
        }

        // Don't allow any build mode input while the blueprint book is open.
        if (BlueprintBook.Instance != null && BlueprintBook.Instance.IsOpen)
        {
            return;
        }

        // Only allow toggling Build Mode when nothing's currently held, so
        // you can't get stuck carrying something with no way to place it.
        if (keyboard.bKey.wasPressedThisFrame && !IsInBuildMode)
        {
            buildModeActive = !buildModeActive;
            if (debugLogging) Debug.Log($"[BuildMode] Toggled: {buildModeActive}");
        }

        if (!buildModeActive)
        {
            return;
        }

        if (!IsInBuildMode)
        {
            if (mouse.leftButton.wasPressedThisFrame)
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
            if (debugLogging) Debug.LogWarning("[BuildMode] TryPickUp aborted: Camera Transform is not assigned.");
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (debugLogging)
        {
            Debug.Log($"[BuildMode] TryPickUp: raycasting from {ray.origin} forward, range {pickupRange}, mask {LayerMaskToString(placeableLayer)}");
            Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.yellow, 2f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, placeableLayer))
        {
            if (debugLogging) Debug.Log($"[BuildMode] Raycast hit '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}' at distance {hit.distance:F2}");

            Placeable placeable = hit.collider.GetComponentInParent<Placeable>();
            if (placeable != null)
            {
                heldObject = placeable;
                heldTransform = placeable.transform;
                heldObject.BeginPreview();
                if (debugLogging) Debug.Log($"[BuildMode] Picked up '{placeable.name}'.");
            }
            else if (debugLogging)
            {
                Debug.LogWarning($"[BuildMode] Hit '{hit.collider.name}' but it (and its parents) has no Placeable component attached.");
            }
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[BuildMode] Raycast hit nothing within range on Placeable Layer. Either you're not looking directly at it, it's out of Pickup Range, or its GameObject's layer isn't included in the Placeable Layer mask.");
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

            if (hitIsGround)
            {
                Transform anchor = heldObject.GroundAnchor;
                if (anchor != null)
                {
                    // Move the object by exactly the difference between where
                    // its anchor currently is and where we want the anchor to
                    // be. This works at any rotation, since the anchor is a
                    // child transform and its world position already reflects
                    // the object's current yaw.
                    Vector3 desiredAnchorPos = hit.point + Vector3.up * surfaceOffset;
                    Vector3 delta = desiredAnchorPos - anchor.position;
                    heldTransform.position += delta;
                }
                else
                {
                    // Fallback: rest the object's collider bounds on top of
                    // the ground point. Only accurate if the mesh is roughly
                    // centered in its collider.
                    Vector3 targetCenter = hit.point + Vector3.up * (extents.y + surfaceOffset);
                    heldTransform.position = targetCenter - centerOffset;
                }
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
                Vector3 targetCenter = hit.point + hit.normal * pushOut;
                heldTransform.position = targetCenter - centerOffset;
            }
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

    private static string LayerMaskToString(LayerMask mask)
    {
        if (mask.value == 0)
        {
            return "(none selected!)";
        }

        var names = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                names.Add(string.IsNullOrEmpty(layerName) ? $"#{i}" : layerName);
            }
        }
        return string.Join(", ", names);
    }
}