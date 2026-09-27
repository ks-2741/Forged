using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the five rolling save slots and the in-game pause/save menu. It is
/// created automatically so it does not require scene-specific wiring.
/// </summary>
public class SaveGameManager : MonoBehaviour
{
    private const int SlotCount = 5;
    private const float AutoSaveSeconds = 600f;
    private const string NextSlotKey = "Forged.SaveGame.NextSlot";

    public static SaveGameManager Instance { get; private set; }
    public static bool IsMenuOpen { get; private set; }

    private readonly SaveGameData[] cachedSlots = new SaveGameData[SlotCount];
    private GameObject menuRoot;
    private GameObject mainPanel;
    private GameObject loadPanel;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI[] slotLabels;
    private SaveGameMenu editableMenu;
    private float autoSaveTimer;
    private float previousTimeScale = 1f;
    private SaveGameData pendingLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            GameObject managerObject = new GameObject("Save Game Manager");
            managerObject.AddComponent<SaveGameManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSlotIndex();
    }

    private void Start()
    {
        BuildMenu();
        RefreshSlotCache();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void Update()
    {
        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= AutoSaveSeconds)
        {
            autoSaveTimer = 0f;
            SaveGame(false);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || MapViewControllerIsOpen())
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (IsMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (menuRoot == null)
        {
            BuildMenu();
        }

        EnsureEventSystem();
        IsMenuOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        menuRoot.SetActive(true);
        mainPanel.SetActive(true);
        loadPanel.SetActive(false);
        RefreshSlotCache();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        IsMenuOpen = false;
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowLoadPanel()
    {
        RefreshSlotCache();
        mainPanel.SetActive(false);
        loadPanel.SetActive(true);
    }

    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        loadPanel.SetActive(false);
        RefreshSlotCache();
    }

    public void SaveManually()
    {
        SaveGame(true);
    }

    public void LoadSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount || cachedSlots[slot] == null)
        {
            SetStatus("That save slot is empty.");
            return;
        }

        pendingLoad = cachedSlots[slot];
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeScene, pendingLoad.sceneName, StringComparison.Ordinal))
        {
            SceneManager.LoadScene(pendingLoad.sceneName);
        }
        else
        {
            StartCoroutine(ApplyPendingLoadNextFrame());
        }
    }

    private void SaveGame(bool showMessage)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            if (showMessage) SetStatus("No player found in this scene.");
            return;
        }

        int slot = PlayerPrefs.GetInt(NextSlotKey, 0) % SlotCount;
        SaveGameData data = Capture(player);
        string path = GetSlotPath(slot);

        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            PlayerPrefs.SetInt(NextSlotKey, (slot + 1) % SlotCount);
            PlayerPrefs.Save();
            cachedSlots[slot] = data;
            autoSaveTimer = 0f;
            if (showMessage) SetStatus($"Saved to slot {slot + 1}.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveGameManager] Save failed: {exception.Message}");
            if (showMessage) SetStatus("Save failed. Check the player log.");
        }
    }

    private SaveGameData Capture(PlayerController player)
    {
        Inventory inventory = player.GetComponent<Inventory>();
        ItemData heldItem = inventory != null ? inventory.HeldItem : null;

        return new SaveGameData
        {
            sceneName = SceneManager.GetActiveScene().name,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            playerPosition = player.transform.position,
            playerRotation = player.transform.eulerAngles,
            bankedGold = GameSession.BankedGold,
            heldItemName = heldItem != null ? heldItem.itemName : string.Empty,
            currentLevelName = GameSession.CurrentLevel != null ? GameSession.CurrentLevel.levelName : string.Empty
        };
    }

    private IEnumerator ApplyPendingLoadNextFrame()
    {
        yield return null;
        ApplyPendingLoad();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The editable menu belongs to its scene. Rebind to the new scene's
        // menu after a scene change instead of keeping destroyed references.
        if (menuRoot == null)
        {
            editableMenu = null;
            mainPanel = null;
            loadPanel = null;
            statusText = null;
            slotLabels = null;
            BuildMenu();
            RefreshSlotCache();
        }

        if (pendingLoad != null)
        {
            StartCoroutine(ApplyPendingLoadNextFrame());
        }
    }

    private void ApplyPendingLoad()
    {
        if (pendingLoad == null)
        {
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            SetStatus("The saved scene has no player.");
            pendingLoad = null;
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.SetPositionAndRotation(pendingLoad.playerPosition, Quaternion.Euler(pendingLoad.playerRotation));
        if (controller != null) controller.enabled = true;

        GameSession.BankedGold = pendingLoad.bankedGold;
        RestoreHeldItem(player, pendingLoad.heldItemName);
        RestoreCurrentLevel(pendingLoad.currentLevelName);

        pendingLoad = null;
        SetStatus("Save loaded.");
        CloseMenu();
    }

    private void RestoreHeldItem(PlayerController player, string itemName)
    {
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory == null) return;
        inventory.DropHeld();
        if (string.IsNullOrEmpty(itemName)) return;

        ItemData item = FindItem(itemName);
        if (item == null || item.worldPrefab == null) return;

        GameObject itemObject = Instantiate(item.worldPrefab, player.transform.position, Quaternion.identity);
        WorldItem worldItem = itemObject.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            Destroy(itemObject);
            return;
        }

        worldItem.Initialize(item, 1);
        if (!inventory.TryPickUp(item, itemObject)) Destroy(itemObject);
    }

    private static ItemData FindItem(string itemName)
    {
        ItemData[] items = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in items)
        {
            if (item != null && string.Equals(item.itemName, itemName, StringComparison.Ordinal)) return item;
        }
        return null;
    }

    private static void RestoreCurrentLevel(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return;
        LevelDefinition[] levels = Resources.FindObjectsOfTypeAll<LevelDefinition>();
        foreach (LevelDefinition level in levels)
        {
            if (level != null && string.Equals(level.levelName, levelName, StringComparison.Ordinal))
            {
                GameSession.CurrentLevel = level;
                return;
            }
        }
    }

    private void BuildMenu()
    {
        if (menuRoot != null) return;

        EnsureEventSystem();

        SaveGameMenu sceneMenu = FindFirstObjectByType<SaveGameMenu>(FindObjectsInactive.Include);
        if (sceneMenu != null)
        {
            menuRoot = sceneMenu.gameObject;
            editableMenu = sceneMenu;
            editableMenu.Bind(this);
            mainPanel = editableMenu.MainPanel;
            loadPanel = editableMenu.LoadPanel;
            statusText = editableMenu.StatusText;
            slotLabels = editableMenu.SlotLabels;
            menuRoot.SetActive(false);
            return;
        }

        GameObject menuPrefab = Resources.Load<GameObject>("SaveGameMenu");
        if (menuPrefab != null && menuPrefab.GetComponent<SaveGameMenu>() != null)
        {
            menuRoot = Instantiate(menuPrefab);
            menuRoot.name = "Save Menu";
            DontDestroyOnLoad(menuRoot);
            editableMenu = menuRoot.GetComponent<SaveGameMenu>();
            editableMenu.Bind(this);
            mainPanel = editableMenu.MainPanel;
            loadPanel = editableMenu.LoadPanel;
            statusText = editableMenu.StatusText;
            slotLabels = editableMenu.SlotLabels;
            menuRoot.SetActive(false);
            return;
        }

        menuRoot = new GameObject("Save Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(menuRoot);
        Canvas canvas = menuRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = menuRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image backdrop = CreateImage("Backdrop", menuRoot.transform, new Color(0f, 0f, 0f, 0.68f), Vector2.zero, Vector2.one);
        backdrop.rectTransform.offsetMin = Vector2.zero;
        backdrop.rectTransform.offsetMax = Vector2.zero;

        mainPanel = CreatePanel("Main Panel", menuRoot.transform, new Vector2(520f, 500f));
        CreateLabel("Title", mainPanel.transform, "PAUSED", 42, new Vector2(0f, 150f), new Vector2(440f, 70f));
        statusText = CreateLabel("Status", mainPanel.transform, "Autosaves every 10 minutes", 18, new Vector2(0f, 95f), new Vector2(440f, 40f));
        CreateButton("Save", mainPanel.transform, "SAVE GAME", new Vector2(0f, 25f), SaveManually);
        CreateButton("Load", mainPanel.transform, "LOAD GAME", new Vector2(0f, -55f), ShowLoadPanel);
        CreateButton("Close", mainPanel.transform, "CONTINUE", new Vector2(0f, -135f), CloseMenu);

        loadPanel = CreatePanel("Load Panel", menuRoot.transform, new Vector2(640f, 620f));
        CreateLabel("Load Title", loadPanel.transform, "LOAD GAME", 38, new Vector2(0f, 230f), new Vector2(560f, 60f));
        slotLabels = new TextMeshProUGUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            int slot = i;
            slotLabels[i] = CreateButton("Slot " + (i + 1), loadPanel.transform, "", new Vector2(0f, 145f - i * 70f), () => LoadSlot(slot)).GetComponentInChildren<TextMeshProUGUI>();
        }
        CreateButton("Back", loadPanel.transform, "BACK", new Vector2(0f, -225f), ShowMainPanel);

        menuRoot.SetActive(false);
    }

    private void RefreshSlotCache()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            cachedSlots[i] = null;
            string path = GetSlotPath(i);
            if (File.Exists(path))
            {
                try { cachedSlots[i] = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path)); }
                catch (Exception exception) { Debug.LogWarning($"[SaveGameManager] Could not read slot {i + 1}: {exception.Message}"); }
            }

            if (slotLabels != null && slotLabels[i] != null)
            {
                SaveGameData data = cachedSlots[i];
                slotLabels[i].text = data == null ? $"SLOT {i + 1}  -  EMPTY" : $"SLOT {i + 1}  -  {data.savedAt}";
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log("[SaveGameManager] " + message);
    }

    private static string GetSlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"forged_save_{slot}.json");
    }

    private static void LoadSlotIndex()
    {
        PlayerPrefs.GetInt(NextSlotKey, 0);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject eventSystem = new GameObject("Save Menu EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        Image image = CreateImage(name, parent, new Color(0.06f, 0.08f, 0.1f, 0.98f), Vector2.zero, Vector2.zero);
        image.rectTransform.sizeDelta = size;
        return image.gameObject;
    }

    private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject objectRoot = new GameObject(name, typeof(RectTransform), typeof(Image));
        objectRoot.transform.SetParent(parent, false);
        Image image = objectRoot.GetComponent<Image>();
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        return image;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject objectRoot = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        objectRoot.transform.SetParent(parent, false);
        TextMeshProUGUI label = objectRoot.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        RectTransform rect = label.rectTransform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        Image image = CreateImage(name, parent, new Color(0.16f, 0.35f, 0.42f, 1f), Vector2.zero, Vector2.zero);
        image.rectTransform.sizeDelta = new Vector2(440f, 56f);
        image.rectTransform.anchoredPosition = position;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateLabel("Text", image.transform, text, 20, Vector2.zero, new Vector2(420f, 50f));
        return button;
    }

    private static bool MapViewControllerIsOpen()
    {
        return MapViewController.Instance != null && MapViewController.Instance.IsOpen;
    }
}

[Serializable]
public class SaveGameData
{
    public string sceneName;
    public string savedAt;
    public Vector3 playerPosition;
    public Vector3 playerRotation;
    public int bankedGold;
    public string heldItemName;
    public string currentLevelName;
}
