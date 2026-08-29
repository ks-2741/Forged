using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on the player (alongside PlayerController/PlayerInteractor).
/// MapWall.Interact() calls EnterMapView() here - it moves the player's
/// camera to a fixed viewpoint facing the map wall, unlocks the cursor,
/// and freezes normal movement/interaction (PlayerController,
/// PlayerInteractor, and BuildModeController all check IsOpen the same
/// way they already check SellerStation/BlueprintBook).
///
/// While open, clicking uses a MOUSE-POSITION raycast (Camera.ScreenPointToRay)
/// rather than PlayerInteractor's crosshair-centered one, since the cursor
/// is now free to roam the screen and needs to hit whichever LevelNode
/// cube it's actually over. Escape or right-click backs out to normal play.
/// </summary>
public class MapViewController : MonoBehaviour
{
    public static MapViewController Instance { get; private set; }

    [Header("References")]
    [Tooltip("The player's normal FPS camera transform - the same one PlayerController/PlayerInteractor use.")]
    [SerializeField] private Transform playerCameraTransform;
    [Tooltip("Empty Transform positioned and rotated to look straight at the map wall.")]
    [SerializeField] private Transform mapViewPoint;
    [Tooltip("Layer(s) LevelNode cubes are on - can reuse your normal Interactable Layer, since PlayerInteractor is blocked while the map is open, so there's no conflict.")]
    [SerializeField] private LayerMask mapInteractableLayer;
    [SerializeField] private float mapClickRange = 20f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public bool IsOpen { get; private set; }

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            ExitMapView();
            return;
        }

        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            ExitMapView();
            return;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            TryClickMap(mouse.position.ReadValue());
        }
    }

    public void EnterMapView()
    {
        if (IsOpen)
        {
            return;
        }

        if (playerCameraTransform == null || mapViewPoint == null)
        {
            Debug.LogWarning("[MapViewController] Can't enter map view - Player Camera Transform or Map View Point is missing.");
            return;
        }

        originalLocalPosition = playerCameraTransform.localPosition;
        originalLocalRotation = playerCameraTransform.localRotation;

        playerCameraTransform.position = mapViewPoint.position;
        playerCameraTransform.rotation = mapViewPoint.rotation;

        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (debugLogging) Debug.Log("[MapViewController] Entered map view.");
    }

    public void ExitMapView()
    {
        if (!IsOpen)
        {
            return;
        }

        if (playerCameraTransform != null)
        {
            playerCameraTransform.localPosition = originalLocalPosition;
            playerCameraTransform.localRotation = originalLocalRotation;
        }

        IsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (debugLogging) Debug.Log("[MapViewController] Exited map view.");
    }

    private void TryClickMap(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, mapClickRange, mapInteractableLayer))
        {
            if (debugLogging) Debug.Log($"[MapViewController] Clicked '{hit.collider.name}'.");

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(gameObject);
            }
            else if (debugLogging)
            {
                Debug.LogWarning($"[MapViewController] Hit '{hit.collider.name}' but it has no IInteractable.");
            }
        }
        else if (debugLogging)
        {
            Debug.Log("[MapViewController] Click missed every map node.");
        }
    }
}