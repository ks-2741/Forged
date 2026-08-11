using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks the ONE item the player is currently physically holding. When an
/// item is picked up, its actual world GameObject (with mesh, WorldItem,
/// etc.) gets re-parented into Hand Anchor - an empty Transform positioned
/// in front of the camera - rather than being destroyed and tracked as an
/// abstract count. The player can only hold one item at a time.
/// Right-click drops whatever's currently held back into the world.
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("Hand")]
    [Tooltip("Empty child Transform positioned in front of the camera/character. Picked-up items get parented here.")]
    [SerializeField] private Transform handAnchor;

    [Header("Drop")]
    [Tooltip("Used to guard against dropping while carrying a Placeable in Build Mode. Optional.")]
    [SerializeField] private BuildModeController buildModeController;
    [Tooltip("Small forward toss applied to a dropped item, using this Transform's forward direction (usually the camera). Leave empty for no toss.")]
    [SerializeField] private Transform tossDirectionSource;
    [SerializeField] private float tossForce = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    [SerializeField] private bool debugLogging = true;

    private ItemData heldItem;
    private GameObject heldObject;

    public bool IsHolding => heldItem != null;
    public ItemData HeldItem => heldItem;

    /// <summary>Fired whenever what's held changes (picked up, consumed, or dropped).</summary>
    public event System.Action OnHeldChanged;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (!mouse.rightButton.wasPressedThisFrame)
        {
            return;
        }

        // Don't drop the hand item if right-click is currently doing
        // something else (cancelling a Build Mode placement).
        if (buildModeController != null && buildModeController.IsInBuildMode)
        {
            return;
        }

        if (IsHolding)
        {
            DropHeld();
        }
    }

    /// <summary>
    /// Attempts to pick up a physical world item into the player's hand.
    /// Fails (returns false) if already holding something. On success, the
    /// object's physics/colliders are disabled and it's parented into the
    /// hand anchor - the caller (WorldItem) should NOT destroy it itself.
    /// </summary>
    public bool TryPickUp(ItemData item, GameObject worldObject)
    {
        if (IsHolding)
        {
            return false;
        }

        if (item == null || worldObject == null || handAnchor == null)
        {
            return false;
        }

        heldItem = item;
        heldObject = worldObject;

        Rigidbody rb = worldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider col in worldObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        Transform t = worldObject.transform;
        Vector3 worldScaleBefore = t.lossyScale;

        t.SetParent(handAnchor, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        // SetParent keeps localScale's NUMBER unchanged, but since the new
        // parent (hand anchor) may itself have a non-1 scale, the object's
        // actual visual size would otherwise silently shrink/grow every time
        // it's picked up. Explicitly recompute localScale so the object's
        // real-world size stays exactly what it was before pickup.
        Vector3 parentScale = handAnchor.lossyScale;
        t.localScale = new Vector3(
            parentScale.x != 0f ? worldScaleBefore.x / parentScale.x : worldScaleBefore.x,
            parentScale.y != 0f ? worldScaleBefore.y / parentScale.y : worldScaleBefore.y,
            parentScale.z != 0f ? worldScaleBefore.z / parentScale.z : worldScaleBefore.z
        );

        WorldItem worldItemComp = worldObject.GetComponent<WorldItem>();
        Transform grip = worldItemComp != null ? worldItemComp.GripPoint : null;

        if (grip != null)
        {
            // t is now sitting exactly at the hand anchor's position/rotation.
            // Only correct POSITION using the grip point's world-space offset -
            // not rotation, since an unintentionally-oriented grip marker can
            // twist the whole mesh around that point even though the point
            // itself still lands correctly.
            Vector3 posDelta = handAnchor.position - grip.position;
            t.position += posDelta;
        }

        OnHeldChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes whatever's held from the hand WITHOUT destroying it, and
    /// returns its GameObject so the caller can take ownership of it (e.g.
    /// placing it on an anvil). Physics/colliders remain disabled exactly
    /// as they were while held - the caller is responsible for
    /// re-enabling anything it needs.
    /// </summary>
    public GameObject TakeHeldObject()
    {
        if (!IsHolding)
        {
            return null;
        }

        GameObject obj = heldObject;
        heldItem = null;
        heldObject = null;
        OnHeldChanged?.Invoke();
        return obj;
    }

    /// <summary>
    /// Consumes whatever's currently held (e.g. fed into a furnace) -
    /// destroys its physical object and frees the hand. Returns the item
    /// that was consumed, or null if nothing was held.
    /// </summary>
    public ItemData ConsumeHeld()
    {
        if (!IsHolding)
        {
            return null;
        }

        ItemData item = heldItem;

        if (heldObject != null)
        {
            Destroy(heldObject);
        }

        heldItem = null;
        heldObject = null;
        OnHeldChanged?.Invoke();
        return item;
    }

    /// <summary>
    /// Hands off whatever's currently held to another system (e.g. placing
    /// it on the anvil) WITHOUT destroying it or restoring its physics -
    /// the caller takes full ownership and is responsible for what happens
    /// to the returned GameObject next. Returns null if nothing was held.
    /// </summary>
    public GameObject ReleaseHeldObject()
    {
        if (!IsHolding)
        {
            return null;
        }

        GameObject obj = heldObject;
        heldItem = null;
        heldObject = null;
        OnHeldChanged?.Invoke();
        return obj;
    }

    /// <summary>
    /// Moves whatever's currently held onto a target slot Transform (e.g.
    /// an anvil's mount point) - keeps it kinematic/collider-disabled just
    /// like it was in the hand, but parented elsewhere. Frees the hand.
    /// Returns the released GameObject, or null if nothing was held.
    /// </summary>
    public GameObject ReleaseHeldTo(Transform target)
    {
        if (!IsHolding || target == null)
        {
            return null;
        }

        GameObject obj = heldObject;

        if (obj != null)
        {
            Transform t = obj.transform;
            Vector3 worldScaleBefore = t.lossyScale;

            t.SetParent(target, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            Vector3 parentScale = target.lossyScale;
            t.localScale = new Vector3(
                parentScale.x != 0f ? worldScaleBefore.x / parentScale.x : worldScaleBefore.x,
                parentScale.y != 0f ? worldScaleBefore.y / parentScale.y : worldScaleBefore.y,
                parentScale.z != 0f ? worldScaleBefore.z / parentScale.z : worldScaleBefore.z
            );
        }

        heldItem = null;
        heldObject = null;
        OnHeldChanged?.Invoke();
        return obj;
    }

    /// <summary>Drops whatever's held back into the world at the hand position, re-enabling its physics.</summary>
    public void DropHeld()
    {
        if (!IsHolding)
        {
            return;
        }

        if (debugLogging) Debug.Log($"[Inventory] Dropped '{heldItem.itemName}'.");

        Rigidbody rb = null;

        if (heldObject != null)
        {
            heldObject.transform.SetParent(null);

            rb = heldObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            foreach (Collider col in heldObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
            }
        }

        if (rb != null && tossDirectionSource != null && tossForce > 0f)
        {
            rb.AddForce(tossDirectionSource.forward * tossForce, ForceMode.VelocityChange);
        }

        heldItem = null;
        heldObject = null;
        OnHeldChanged?.Invoke();
    }

    private void OnGUI()
    {
        if (!showDebugGUI)
        {
            return;
        }

        GUI.Box(new Rect(10, 10, 220, 40), "");
        GUILayout.BeginArea(new Rect(20, 15, 200, 30));
        GUILayout.Label(IsHolding ? $"Holding: {heldItem.itemName} (Right-click to drop)" : "Hands empty");
        GUILayout.EndArea();
    }
}