using UnityEngine;

/// <summary>
/// Put this on the map wall's collider. Left-clicking it (via
/// PlayerInteractor, using your normal crosshair interaction) enters map
/// view - see MapViewController.
/// </summary>
public class MapWall : MonoBehaviour, IInteractable
{
    public void Interact(GameObject interactor)
    {
        if (MapViewController.Instance != null)
        {
            MapViewController.Instance.EnterMapView();
        }
    }
}