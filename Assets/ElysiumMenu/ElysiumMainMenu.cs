using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Keeps the existing scene and Inspector fields compatible with the original menu.
public class ElysiumMainMenu : MonoBehaviour
{
    public string gameplayScene = "SampleScene";
    [TextArea] public string credits = "Elysium\n\nAdd your team names and asset credits here.";
    public Sprite backgroundSprite;
    public Sprite buttonSprite;
    public Font menuFont;
    private GameObject mainPanel, creditsPanel;
    private Button startButton, backButton;
    private Text status;
    private readonly Color ink = new Color32(46, 63, 48, 255);
    private readonly Color gold = new Color32(174, 146, 83, 255);
    private readonly Color paper = new Color32(247, 240, 221, 255);

    void Start()
    {
        Time.timeScale = 1f;
        if (menuFont == null) menuFont = Resources.Load<Font>("ElysiumFonts/MenuSerif");
        if (menuFont == null) menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var background = Panel("Forest background", canvasObject.transform).AddComponent<Image>();
        background.color = backgroundSprite == null ? ink : Color.white;
        background.sprite = backgroundSprite;
        background.raycastTarget = false;
        // A fixed composition scales together; the background fills wider displays.
        var card = Box(canvasObject.transform, "Parchment", Vector2.zero, new Vector2(940, 940), paper);
        Border(card.transform, 908, 908, gold);
        Border(card.transform, 880, 880, new Color32(211, 199, 166, 255));
        Sprig(canvasObject.transform, new Vector2(-665, -130), 1);
        Sprig(canvasObject.transform, new Vector2(665, -130), -1);
        Label(canvasObject.transform, "A WORLD WORTH BUILDING", new Vector2(0, -510), new Vector2(800, 40), 18, new Color32(218, 211, 187, 255));

        mainPanel = Panel("Main Menu", card.transform);
        Label(mainPanel.transform, "BUILD  /  BALANCE  /  BELONG", new Vector2(0, 325), new Vector2(740, 40), 19, ink);
        var title = Label(mainPanel.transform, "Elysium", new Vector2(0, 220), new Vector2(800, 160), 112, ink);
        title.fontStyle = FontStyle.Italic;
        Ornament(mainPanel.transform, 112);
        Label(mainPanel.transform, "A new city. A greener tomorrow.", new Vector2(0, 52), new Vector2(800, 60), 25, ink);
        startButton = MakeButton(mainPanel.transform, "Start game", -62, StartGame, true);
        var creditsButton = MakeButton(mainPanel.transform, "Credits", -165, ShowCredits, false);
        var quitButton = MakeButton(mainPanel.transform, "Quit game", -268, QuitGame, false);
        LinkNavigation(new[] { startButton, creditsButton, quitButton });
        status = Label(mainPanel.transform, "", new Vector2(0, -360), new Vector2(810, 70), 18, ink);

        creditsPanel = Panel("Credits", card.transform);
        Label(creditsPanel.transform, "The people behind", new Vector2(0, 320), new Vector2(800, 50), 24, ink);
        Label(creditsPanel.transform, "Elysium", new Vector2(0, 235), new Vector2(800, 130), 78, ink).fontStyle = FontStyle.Italic;
        Ornament(creditsPanel.transform, 140);
        var body = Label(creditsPanel.transform, credits, new Vector2(0, -25), new Vector2(750, 270), 28, ink);
        body.resizeTextForBestFit = true;
        body.resizeTextMinSize = 16;
        body.resizeTextMaxSize = 28;
        Label(creditsPanel.transform, "Menu typeface: DejaVu Serif", new Vector2(0, -206), new Vector2(760, 40), 17, ink);
        backButton = MakeButton(creditsPanel.transform, "Go back", -300, ShowMainMenu, true);
        creditsPanel.SetActive(false);
        startButton.Select();
    }

    GameObject Panel(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return obj;
    }

    Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var image = obj.GetComponent<Image>();
        image.color = color; image.raycastTarget = false;
        return image;
    }

    void Border(Transform parent, float width, float height, Color color)
    {
        Box(parent, "Top rule", new Vector2(0, height / 2), new Vector2(width, 2), color);
        Box(parent, "Bottom rule", new Vector2(0, -height / 2), new Vector2(width, 2), color);
        Box(parent, "Left rule", new Vector2(-width / 2, 0), new Vector2(2, height), color);
        Box(parent, "Right rule", new Vector2(width / 2, 0), new Vector2(2, height), color);
    }

    void Ornament(Transform parent, float y)
    {
        Box(parent, "Left ornament", new Vector2(-143, y), new Vector2(230, 2), gold);
        Box(parent, "Right ornament", new Vector2(143, y), new Vector2(230, 2), gold);
        Box(parent, "Diamond", new Vector2(0, y), new Vector2(12, 12), gold).rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
    }

    void Sprig(Transform parent, Vector2 origin, float mirror)
    {
        var color = new Color32(102, 120, 83, 255);
        Box(parent, "Botanical stem", origin, new Vector2(3, 430), color).rectTransform.localRotation = Quaternion.Euler(0, 0, -18 * mirror);
        for (int i = 0; i < 6; i++)
        {
            float y = -150 + i * 60;
            float x = y * 0.325f * mirror;
            foreach (int side in new[] { -1, 1 })
            {
                var leaf = Box(parent, "Geometric leaf", origin + new Vector2(x + side * 29, y), new Vector2(54, 17), color);
                leaf.rectTransform.localRotation = Quaternion.Euler(0, 0, side * 38);
            }
        }
    }

    Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = obj.GetComponent<Text>();
        text.font = menuFont; text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }

    Button MakeButton(Transform parent, string title, float y, UnityEngine.Events.UnityAction action, bool primary)
    {
        var image = Box(parent, title + " Button", new Vector2(0, y), new Vector2(540, 82), Color.white);
        image.sprite = buttonSprite; image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = primary ? ink : new Color32(236, 227, 201, 255);
        colors.highlightedColor = primary ? new Color32(74, 95, 62, 255) : new Color32(223, 208, 170, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = primary ? new Color32(27, 43, 29, 255) : new Color32(199, 179, 132, 255);
        colors.fadeDuration = 0.14f; button.colors = colors;
        Border(image.transform, 536, 78, gold);
        Label(image.transform, title, Vector2.zero, new Vector2(490, 72), 29, primary ? paper : ink);
        image.gameObject.AddComponent<ElysiumButtonMotion>();
        button.onClick.AddListener(action);
        return button;
    }

    void LinkNavigation(Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var navigation = new Navigation { mode = Navigation.Mode.Explicit };
            navigation.selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length];
            navigation.selectOnDown = buttons[(i + 1) % buttons.Length];
            buttons[i].navigation = navigation;
        }
    }

    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            status.text = "The gameplay scene is unavailable. Check the Build Profile scene list.";
            Debug.LogError("Add " + gameplayScene + " to the active Build Profile scene list.");
            return;
        }
        SceneManager.LoadScene(gameplayScene);
    }
    public void ShowCredits()
    {
        mainPanel.SetActive(false); creditsPanel.SetActive(true); backButton.Select();
    }
    public void ShowMainMenu()
    {
        creditsPanel.SetActive(false); mainPanel.SetActive(true); startButton.Select();
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

// Pointer and keyboard focus share the same subtle movement.
public class ElysiumButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private bool hovered, selected, pressed;
    void Update()
    {
        float scale = pressed ? 0.98f : (hovered || selected ? 1.025f : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
    }
    void OnDisable() { hovered = selected = pressed = false; transform.localScale = Vector3.one; }
    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; pressed = false; }
}
