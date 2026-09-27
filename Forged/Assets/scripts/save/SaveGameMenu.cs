using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editable layout for the save menu. This is a prefab under Resources so it
/// can be opened and styled in the Unity editor while SaveGameManager supplies
/// the runtime actions.
/// </summary>
[ExecuteAlways]
public class SaveGameMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject loadPanel;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[5];

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button[] slotButtons = new Button[5];

    public GameObject MainPanel => mainPanel;
    public GameObject LoadPanel => loadPanel;
    public TextMeshProUGUI StatusText => statusText;
    public TextMeshProUGUI[] SlotLabels => slotLabels;

    private void OnEnable()
    {
        if (Application.isPlaying && transform.childCount == 0)
        {
            BuildEditableLayout();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && transform.childCount == 0)
        {
            BuildEditableLayout();
        }
    }

    [ContextMenu("Build Editable Layout")]
    public void BuildEditableLayout()
    {
        if (transform.childCount > 0)
        {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        Image backdrop = CreateImage("Backdrop", transform, new Color(0f, 0f, 0f, 0.68f), Vector2.zero, Vector2.one);
        backdrop.rectTransform.offsetMin = Vector2.zero;
        backdrop.rectTransform.offsetMax = Vector2.zero;

        mainPanel = CreatePanel("Main Panel", transform, new Vector2(520f, 500f));
        CreateLabel("Title", mainPanel.transform, "PAUSED", 42, new Vector2(0f, 150f), new Vector2(440f, 70f));
        statusText = CreateLabel("Status", mainPanel.transform, "Autosaves every 10 minutes", 18, new Vector2(0f, 95f), new Vector2(440f, 40f));
        saveButton = CreateButton("Save", mainPanel.transform, "SAVE GAME", new Vector2(0f, 25f));
        loadButton = CreateButton("Load", mainPanel.transform, "LOAD GAME", new Vector2(0f, -55f));
        closeButton = CreateButton("Close", mainPanel.transform, "CONTINUE", new Vector2(0f, -135f));

        loadPanel = CreatePanel("Load Panel", transform, new Vector2(640f, 620f));
        CreateLabel("Load Title", loadPanel.transform, "LOAD GAME", 38, new Vector2(0f, 230f), new Vector2(560f, 60f));
        slotLabels = new TextMeshProUGUI[5];
        slotButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            slotButtons[i] = CreateButton("Slot " + (i + 1), loadPanel.transform, "SLOT " + (i + 1), new Vector2(0f, 145f - i * 70f));
            slotLabels[i] = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }
        backButton = CreateButton("Back", loadPanel.transform, "BACK", new Vector2(0f, -225f));

        mainPanel.SetActive(Application.isPlaying ? false : true);
        loadPanel.SetActive(false);
    }

    public void Bind(SaveGameManager manager)
    {
        if (manager == null) return;

        AddListener(saveButton, manager.SaveManually);
        AddListener(loadButton, manager.ShowLoadPanel);
        AddListener(closeButton, manager.CloseMenu);
        AddListener(backButton, manager.ShowMainPanel);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            AddListener(slotButtons[i], () => manager.LoadSlot(slot));
        }
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
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

    private static Button CreateButton(string name, Transform parent, string text, Vector2 position)
    {
        Image image = CreateImage(name, parent, new Color(0.16f, 0.35f, 0.42f, 1f), Vector2.zero, Vector2.zero);
        image.rectTransform.sizeDelta = new Vector2(440f, 56f);
        image.rectTransform.anchoredPosition = position;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        CreateLabel("Text", image.transform, text, 20, Vector2.zero, new Vector2(420f, 50f));
        return button;
    }
}
