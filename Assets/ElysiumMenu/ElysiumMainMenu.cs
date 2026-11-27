using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

public class ElysiumMainMenu : MonoBehaviour
{
    public string gameplayScene = "IntroScene";

    public Button startButton;
    public Button quitButton;
    public Button creditsButton;
    public Button backButton;

    public GameObject mainPanel;
    public GameObject creditsPanel;

    public Text statusLabel;
    private CitySaveMenu loadMenu;
    private Button loadButton;

    //AUDIO
    [SerializeField] private AudioClip _Clip;                          //think of this as the "tape" to play on the speaker
    private AudioSource _Speaker;                                      //Like a speaker, plays the audio that is put into it

    void Awake()
    {
        //initialize audio
        _Speaker = GetComponent<AudioSource>();                        //finds attached audio source and fills it with the info

        if(_Speaker == null)
            Debug.Log("Audio source is null");
        else
            _Speaker.clip = _Clip;

        //event system code
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        if (startButton == null)
        {
            var obj = GameObject.Find("Start game Button");
            if (obj != null) startButton = obj.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            var obj = GameObject.Find("Quit game button");
            if (obj != null) quitButton = obj.GetComponent<Button>();
        }

        if (creditsButton == null)
        {
            var obj = GameObject.Find("Credits Button");
            if (obj != null) creditsButton = obj.GetComponent<Button>();
        }

        if (backButton == null)
        {
            var obj = GameObject.Find("Go back Button");
            if (obj != null) backButton = obj.GetComponent<Button>();
        }

        if (mainPanel == null) mainPanel = GameObject.Find("Main Menu");
        if (creditsPanel == null) creditsPanel = GameObject.Find("Credits");

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(ShowCredits);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ShowMainMenu);
        }

        if (mainPanel == null || startButton == null) return;
        loadMenu = new CitySaveMenu(mainPanel.transform.parent, startButton, ShowMainMenu, load: LoadCity);
        loadButton = CitySaveMenu.AddButton(startButton, mainPanel.transform, "Load Game", -70);
        loadButton.onClick.AddListener(() => ShowLoadMenu());
        if (creditsButton != null)
            ((RectTransform)creditsButton.transform).anchoredPosition = new Vector2(0, -170);
        if (quitButton != null)
            ((RectTransform)quitButton.transform).anchoredPosition = new Vector2(0, -270);

        if (CitySaveSlots.LoadError != null)
        {
            ShowLoadMenu(CitySaveSlots.LoadError);
            CitySaveSlots.LoadError = null;
        }
    }

    private void Update()
    {
        if (loadMenu != null && loadMenu.IsOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            ShowMainMenu();
    }

    private void ShowLoadMenu(string message = "")
    {
        mainPanel.SetActive(false);
        loadMenu.ShowSlots(message);
    }

    private void LoadCity(int slot, CitySave save)
    {
        if (!Application.CanStreamedLevelBeLoaded(CitySaveSlots.GameplayScene))
        {
            loadMenu.ShowSlots($"Could not load Slot {slot}.");
            return;
        }

        CitySaveSlots.PendingLoad = save;
        SceneManager.LoadScene(CitySaveSlots.GameplayScene);
    }

    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            if (statusLabel != null)
                statusLabel.text = "Scene '" + gameplayScene + "' is not added to Build Settings!";
            
            Debug.LogError("Scene '" + gameplayScene + "' is not in the active Build Settings (File > Build Settings).");
            return;
        }
        //play audio
        _Speaker.Play();

        SceneManager.LoadScene(gameplayScene);
    }

    public void ShowCredits()
    {
        //play audio
        _Speaker.Play();

        //do function
        if (mainPanel != null) mainPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (_Speaker != null) _Speaker.Play();

        bool returningFromLoad = loadMenu != null && loadMenu.IsOpen;
        loadMenu?.Hide();
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (returningFromLoad) EventSystem.current?.SetSelectedGameObject(loadButton.gameObject);
    }

    public void QuitGame()
    {
        //play audio
        _Speaker.Play();

        //do function
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
