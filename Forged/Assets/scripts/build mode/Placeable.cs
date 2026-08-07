using UnityEngine;

/// <summary>
/// Attach this to any object that should be pickup-able and placeable
/// in build mode (e.g. the anvil). Handles swapping materials to show
/// valid/invalid placement, and toggling its own collider so it doesn't
/// block its own placement check while being carried.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Placeable : MonoBehaviour
{
    [Header("Preview Materials")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Placement Bounds")]
    [Tooltip("Used to size the overlap check when testing if a spot is clear. Defaults to this object's collider bounds if left empty.")]
    [SerializeField] private Collider boundsCollider;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Collider mainCollider;

    public bool IsBeingPlaced { get; private set; }

    private void Awake()
    {
        mainCollider = GetComponent<Collider>();

        if (boundsCollider == null)
        {
            boundsCollider = mainCollider;
        }

        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
    }

    /// <summary>Half-extents of the placement footprint, in world space scale.</summary>
    public Vector3 GetBoundsExtents()
    {
        return Vector3.Scale(boundsCollider.bounds.extents, Vector3.one);
    }

    public Vector3 GetBoundsCenterOffset()
    {
        // Offset between the collider's bounds center and the transform, so
        // overlap checks line up with where the mesh actually sits.
        return boundsCollider.bounds.center - transform.position;
    }

    public void BeginPreview()
    {
        IsBeingPlaced = true;
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
    }

    public void EndPreview()
    {
        IsBeingPlaced = false;
        if (mainCollider != null)
        {
            mainCollider.enabled = true;
        }
        RestoreOriginalMaterials();
    }

    public void SetPreviewValid(bool isValid)
    {
        Material target = isValid ? validMaterial : invalidMaterial;
        if (target == null)
        {
            return;
        }

        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = target;
            }
            r.materials = mats;
        }
    }

    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMaterials[i];
        }
    }
}