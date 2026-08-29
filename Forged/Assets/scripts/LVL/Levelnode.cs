using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on each level cube (the forge placeholders on the map wall).
/// Continuously colors itself based on GameSession.BestResults - green if
/// this level has been passed, red otherwise. Clicked via
/// MapViewController's mouse-position raycast while map view is open.
///
/// Click flow is now two-step: the FIRST click on a node arms it (via
/// MapViewController.ArmNode) and shows LevelPreviewUI with the level's
/// info. The SECOND click on that same already-armed node actually loads
/// it (LoadLevel sets GameSession.CurrentLevel and reloads the workshop).
/// Clicking a different node re-arms to that one instead of loading
/// immediately - see MapViewController for how arming/disarming works.
/// </summary>
public class LevelNode : MonoBehaviour, IInteractable
{
    [Header("Level")]
    [SerializeField] private LevelDefinition level;

    [Header("Visuals")]
    [SerializeField] private Renderer nodeRenderer;
    [SerializeField] private Color notCompletedColor = Color.red;
    [SerializeField] private Color completedColor = Color.green;

    [Header("Scene Flow")]
    [Tooltip("Leave EMPTY to just reload whatever scene this map/workshop currently lives in (recommended if map and workshop are the same scene, as they currently are). Set a name only if you later split them into separate scenes.")]
    [SerializeField] private string workshopSceneNameOverride = "";

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public LevelDefinition Level => level;

    private void Update()
    {
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (nodeRenderer == null || level == null)
        {
            return;
        }

        bool passed = GameSession.BestResults.TryGetValue(level, out LevelResult result) && result.passed;
        nodeRenderer.material.color = passed ? completedColor : notCompletedColor;
    }

    public void Interact(GameObject interactor)
    {
        if (level == null)
        {
            if (debugLogging) Debug.LogWarning($"[LevelNode] '{name}' has no Level assigned.");
            return;
        }

        bool alreadyArmed = MapViewController.Instance != null && MapViewController.Instance.ArmedNode == this;

        if (alreadyArmed)
        {
            LoadLevel();
            return;
        }

        if (debugLogging) Debug.Log($"[LevelNode] '{name}' selected - showing preview (click again to load).");

        MapViewController.Instance?.ArmNode(this);

        if (LevelPreviewUI.Instance != null)
        {
            LevelPreviewUI.Instance.Show(level, this);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[LevelNode] No LevelPreviewUI found in the scene - preview skipped, but node is still armed.");
        }
    }

    /// <summary>
    /// Actually commits to this level - called either by a second click on
    /// this same node (see Interact above) or by LevelPreviewUI's Play
    /// button.
    /// </summary>
    public void LoadLevel()
    {
        GameSession.CurrentLevel = level;

        string targetScene = string.IsNullOrEmpty(workshopSceneNameOverride)
            ? SceneManager.GetActiveScene().name
            : workshopSceneNameOverride;

        if (debugLogging) Debug.Log($"[LevelNode] Loading '{level.levelName}' via scene '{targetScene}'.");

        SceneManager.LoadScene(targetScene);
    }
}