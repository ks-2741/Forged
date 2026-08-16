using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on the blueprint book object in the scene. Left-clicking it
/// (via PlayerInteractor) opens the Blueprint UI listing every entry in
/// Blueprints, showing crafted progress and cost, letting the player learn
/// new tiers.
/// </summary>
public class BlueprintBook : MonoBehaviour, IInteractable
{
    public static BlueprintBook Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject blueprintPanel;
    [SerializeField] private List<Blueprint> blueprints = new List<Blueprint>();

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (blueprintPanel != null)
        {
            blueprintPanel.SetActive(false);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        Open(interactor);
    }

    public void Open(GameObject interactor)
    {
        if (blueprintPanel != null)
        {
            blueprintPanel.SetActive(true);
        }

        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (BlueprintUI.Instance != null)
        {
            BlueprintUI.Instance.Populate(blueprints, interactor);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[BlueprintBook] No BlueprintUI found in the scene.");
        }
    }

    public void Close()
    {
        if (blueprintPanel != null)
        {
            blueprintPanel.SetActive(false);
        }

        IsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}