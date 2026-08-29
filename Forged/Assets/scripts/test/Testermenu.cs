using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple tester utility panel. Press Escape once to open it, Escape again
/// to close it - unlike other panels in the game (shop, blueprint book),
/// this one doesn't need a button or world object to open, just the key.
///
/// While open it unlocks the cursor and freezes player movement the same
/// way every other modal panel does - see the IsOpen check added to
/// PlayerController.IsUIBlockingMovement().
/// </summary>
public class TesterMenu : MonoBehaviour
{
    public static TesterMenu Instance { get; private set; }

    [Header("References")]
    [Tooltip("The panel GameObject to show/hide. Put whatever text or debug info testers need on it.")]
    [SerializeField] private GameObject panel;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        IsOpen = !IsOpen;

        if (panel != null)
        {
            panel.SetActive(IsOpen);
        }

        Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsOpen;

        if (debugLogging) Debug.Log($"[TesterMenu] {(IsOpen ? "Opened" : "Closed")}.");
    }
}