using UnityEngine;

/// <summary>
/// Automatically added by DisplayWall to whatever gets mounted on it -
/// you don't add this manually. Flashes white while the player is looking
/// directly at this specific displayed item, and left-clicking it (via
/// PlayerInteractor) picks it back up into the hand, freeing its slot.
/// </summary>
public class DisplayedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float lookRange = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    private DisplayWall owner;
    private int slotIndex;
    private bool isFlashing;

    private struct MatColorRef
    {
        public Material material;
        public string propertyName;
        public Color originalColor;
    }

    private System.Collections.Generic.List<MatColorRef> materialRefs;

    /// <summary>Called by DisplayWall right after this component is added.</summary>
    public void Setup(DisplayWall wall, int index)
    {
        owner = wall;
        slotIndex = index;
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

        bool retrieved = owner.RetrieveFromSlot(slotIndex, interactor);
        if (debugLogging) Debug.Log(retrieved ? "[DisplayedItem] Retrieved." : "[DisplayedItem] Couldn't retrieve (hands full?).");
    }
}