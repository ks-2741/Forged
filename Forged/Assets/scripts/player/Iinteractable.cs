using UnityEngine;

/// <summary>
/// Implement this on anything the player should be able to left-click to
/// interact with outside of Build Mode - a storage container, a customer
/// to hand items to, a workbench, etc. PlayerInteractor finds and calls
/// this automatically.
/// </summary>
public interface IInteractable
{
    void Interact(GameObject interactor);
}