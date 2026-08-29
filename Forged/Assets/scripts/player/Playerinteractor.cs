using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this alongside PlayerController and BuildModeController. Left-click
/// interacts with whatever IInteractable is under the crosshair - taking
/// from storage, handing an item to a customer, etc. Automatically does
/// nothing while Build Mode is active, since left-click is reserved for
/// pickup/placement there.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactableLayer;
    [Tooltip("Used to disable interaction while Build Mode is active.")]
    [SerializeField] private BuildModeController buildModeController;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (buildModeController != null && buildModeController.IsBuildModeActive)
        {
            return;
        }

        // Don't raycast into the world while a UI panel with cursor unlocked
        // is open - clicks should hit UI, not re-trigger world interactions
        // behind it.
        if (SellerStation.Instance != null && SellerStation.Instance.IsShopOpen)
        {
            return;
        }

        if (BlueprintBook.Instance != null && BlueprintBook.Instance.IsOpen)
        {
            return;
        }

        // While the map is open, MapViewController owns clicks with its own
        // mouse-position raycast - this crosshair-centered one must stay
        // out of the way, or a single click could fire both at once.
        if (MapViewController.Instance != null && MapViewController.Instance.IsOpen)
        {
            return;
        }

        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (cameraTransform == null)
        {
            if (debugLogging) Debug.LogWarning("[Interact] Aborted: Camera Transform is not assigned.");
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (debugLogging)
        {
            Debug.Log($"[Interact] Raycasting from {ray.origin} forward, range {interactRange}, mask {LayerMaskToString(interactableLayer)}");
            Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.cyan, 2f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            if (debugLogging) Debug.Log($"[Interact] Raycast hit '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}' at distance {hit.distance:F2}");

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                if (debugLogging) Debug.Log($"[Interact] Found IInteractable on '{hit.collider.name}', calling Interact().");
                interactable.Interact(gameObject);
            }
            else if (debugLogging)
            {
                Debug.LogWarning($"[Interact] Hit '{hit.collider.name}' but it (and its parents) has no component implementing IInteractable.");
            }
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[Interact] Raycast hit nothing within range on Interactable Layer. Either you're not looking directly at it, it's out of Interact Range, or its GameObject's layer isn't included in the Interactable Layer mask.");
        }
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