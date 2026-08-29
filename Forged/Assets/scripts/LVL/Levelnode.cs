using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on each level cube (the forge placeholders on the map wall).
/// Continuously colors itself based on GameSession.BestResults - green if
/// this level has been passed, red otherwise. Clicked via
/// MapViewController's mouse-position raycast while map view is open;
/// loads this cube's Level by setting GameSession.CurrentLevel and
/// reloading the workshop.
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

        GameSession.CurrentLevel = level;

        string targetScene = string.IsNullOrEmpty(workshopSceneNameOverride)
            ? SceneManager.GetActiveScene().name
            : workshopSceneNameOverride;

        if (debugLogging) Debug.Log($"[LevelNode] Loading '{level.levelName}' via scene '{targetScene}'.");

        SceneManager.LoadScene(targetScene);
    }
}