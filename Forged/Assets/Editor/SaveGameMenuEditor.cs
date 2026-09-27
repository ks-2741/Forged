#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds the editable save menu to gameplay scenes while working in the
/// editor. The scene is marked dirty so the user can save the placement.
/// </summary>
[InitializeOnLoad]
public static class SaveGameMenuEditor
{
    static SaveGameMenuEditor()
    {
        EditorApplication.delayCall += EnsureMenuInOpenGameplayScene;
    }

    [MenuItem("Forged/Setup Editable Save Menu")]
    private static void SetupFromMenu()
    {
        EnsureMenuInOpenGameplayScene(false);
    }

    private static void EnsureMenuInOpenGameplayScene()
    {
        EnsureMenuInOpenGameplayScene(true);
    }

    private static void EnsureMenuInOpenGameplayScene(bool requirePlayer)
    {
        EditorApplication.delayCall -= EnsureMenuInOpenGameplayScene;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return;
        if (FindMenu(scene) != null) return;
        if (requirePlayer && Object.FindFirstObjectByType<PlayerController>() == null) return;

        GameObject menuObject = new GameObject("Save Game Menu", typeof(RectTransform), typeof(SaveGameMenu));
        SceneManager.MoveGameObjectToScene(menuObject, scene);
        SaveGameMenu menu = menuObject.GetComponent<SaveGameMenu>();
        menu.BuildEditableLayout();
        Undo.RegisterCreatedObjectUndo(menuObject, "Add Editable Save Menu");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = menuObject;
    }

    private static SaveGameMenu FindMenu(Scene scene)
    {
        SaveGameMenu[] menus = Resources.FindObjectsOfTypeAll<SaveGameMenu>();
        foreach (SaveGameMenu menu in menus)
        {
            if (menu != null && menu.gameObject.scene == scene) return menu;
        }
        return null;
    }
}
#endif
