using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool lockCursor = true;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector2 moveInput;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        ReadMoveInput();
        HandleMouseLook();
        HandleMovement();
        HandleGravity();
        HandleCursorToggle();
    }

    private void ReadMoveInput()
    {
        moveInput = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
    }

    private void HandleMouseLook()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        // Only rotate while the cursor is locked, so you're not spinning the
        // camera while clicking around a paused/unlocked UI.
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        float yaw = mouseDelta.x * mouseSensitivity;
        float pitchDelta = mouseDelta.y * mouseSensitivity;

        // Yaw rotates the whole player body left/right.
        transform.Rotate(Vector3.up, yaw);

        // Pitch tilts only the camera up/down, clamped so you can't flip over.
        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDirection.magnitude < 0.1f)
        {
            return;
        }

        // In first person, movement follows the player body's own facing,
        // since the camera is a child of the player and shares its yaw.
        Vector3 moveDirection = transform.forward * inputDirection.z + transform.right * inputDirection.x;

        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = groundedStickForce;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCursorToggle()
    {
        // Press Escape to free the cursor (e.g. for a pause menu), click to relock.
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && lockCursor && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}