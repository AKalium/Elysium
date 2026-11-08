using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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

    void Awake()
    {
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
            var obj = GameObject.Find("Quit game Button");
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

        SceneManager.LoadScene(gameplayScene);
    }

    public void ShowCredits()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
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