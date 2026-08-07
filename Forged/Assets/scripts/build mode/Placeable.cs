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

    [Header("Ground Anchor")]
    [Tooltip("Empty child transform placed at the object's true visual bottom (e.g. the base of the forge's legs). If set, this exact point is rested on the floor instead of relying on collider bounds - fixes clipping on off-center meshes. Leave empty to fall back to bounds-based resting.")]
    [SerializeField] private Transform groundAnchor;

    [Header("Layers")]
    [Tooltip("Layer this object (and its children) switches to once it's actually placed (EndPreview). Create this layer in Tags and Layers, and make sure it's ticked in BuildModeController's Obstacle Layers so placed objects reliably block each other. Leave blank to not change layer on placement.")]
    [SerializeField] private string placedLayerName = "Placed";

    [Tooltip("Layer this object (and its children) switches to while being carried, before it's placed. Usually not needed since the collider is disabled while carried anyway, but useful if other systems (AI vision, interaction prompts) check layer. Leave blank to keep whatever layer it's currently on.")]
    [SerializeField] private string carriedLayerName = "";

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Collider mainCollider;
    private int placedLayer = -1;
    private int carriedLayer = -1;

    public bool IsBeingPlaced { get; private set; }
    public Transform GroundAnchor => groundAnchor;

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

        placedLayer = ResolveLayer(placedLayerName);
        carriedLayer = ResolveLayer(carriedLayerName);
    }

    private int ResolveLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return -1;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"Placeable on '{name}' references layer '{layerName}', which doesn't exist. Create it under Edit > Project Settings > Tags and Layers, or clear the field.", this);
        }

        return layer;
    }

    private void SetLayerRecursively(int layer)
    {
        if (layer < 0)
        {
            return;
        }

        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            t.gameObject.layer = layer;
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
        SetLayerRecursively(carriedLayer);
    }

    public void EndPreview()
    {
        IsBeingPlaced = false;
        if (mainCollider != null)
        {
            mainCollider.enabled = true;
        }
        RestoreOriginalMaterials();
        SetLayerRecursively(placedLayer);
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