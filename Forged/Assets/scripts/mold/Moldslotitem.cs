using UnityEngine;

/// <summary>
/// Automatically added by CraftingStation to a mold the moment it's placed
/// in the mold slot - you don't add this manually. Flashes white while the
/// player looks directly at it, and left-clicking it (via PlayerInteractor)
/// picks it back up into the hand, freeing the mold slot.
/// </summary>
public class MoldSlotItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float lookRange = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private CraftingStation owner;
    private bool isFlashing;

    private struct MatColorRef
    {
        public Material material;
        public string propertyName;
        public Color originalColor;
    }

    private System.Collections.Generic.List<MatColorRef> materialRefs;

    /// <summary>Called by CraftingStation right after this component is added.</summary>
    public void Setup(CraftingStation station)
    {
        owner = station;
        CacheMaterials();
    }

    private void CacheMaterials()
    {
        materialRefs = new System.Collections.Generic.List<MatColorRef>();

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                string prop = mat.HasProperty("_BaseColor") ? "_BaseColor" : (mat.HasProperty("_Color") ? "_Color" : null);
                if (prop != null)
                {
                    materialRefs.Add(new MatColorRef { material = mat, propertyName = prop, originalColor = mat.GetColor(prop) });
                }
            }
        }
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool looking = Physics.Raycast(ray, out RaycastHit hit, lookRange) && hit.collider.transform.IsChildOf(transform);
        SetFlashing(looking);
    }

    private void OnDestroy()
    {
        // If this gets destroyed (e.g. mold retrieved) while mid-flash, make
        // sure the material is left in its ORIGINAL color, not stuck white -
        // Destroy() doesn't otherwise give us a chance to clean this up.
        if (materialRefs == null)
        {
            return;
        }

        foreach (MatColorRef entry in materialRefs)
        {
            if (entry.material != null)
            {
                entry.material.SetColor(entry.propertyName, entry.originalColor);
            }
        }
    }

    private void SetFlashing(bool on)
    {
        if (isFlashing == on || materialRefs == null)
        {
            return;
        }

        isFlashing = on;
        foreach (MatColorRef entry in materialRefs)
        {
            entry.material.SetColor(entry.propertyName, on ? flashColor : entry.originalColor);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (owner == null)
        {
            return;
        }

        bool retrieved = owner.RetrieveMold(interactor);
        if (debugLogging) Debug.Log(retrieved ? "[MoldSlotItem] Retrieved." : "[MoldSlotItem] Couldn't retrieve (hands full?).");
    }
}