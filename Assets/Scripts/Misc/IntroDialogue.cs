using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Dialogue playback for the reusable Dialogue prefab. Layout is authored in the prefab.</summary>
public sealed class IntroDialogue : MonoBehaviour
{
    [SerializeField] private Texture2D idleTexture;
    [SerializeField] private Texture2D talkingTexture;
    [SerializeField] private string mascotName = "Dawny";
    [SerializeField] private string nextScene = "SampleScene";
    [SerializeField, Min(1)] private float charactersPerSecond = 40f;
    [SerializeField, TextArea(2, 5)] private string[] dialogueLines =
    {
        "Welcome to Elysium!",
        "Hi, I’m {name}, your new, <i>permanent</i> junior city planning assistant.",
        "Together, we’re going to walk through the process of constructing and demolishing buildings. In this case, your beautiful new Mayor's Office!"
    };

    [SerializeField] private Text dialogueText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text buttonText;
    [SerializeField] private RawImage portrait;
    [SerializeField] private Button continueButton;
    [SerializeField] private Text nameText;
    private Coroutine typing;
    private string fullLine;
    private int lineIndex;
    private bool isTyping;
    private bool isLoading;
    private bool hasStarted;

    private void Start()
    {
        hasStarted = true;
        BeginDialogue();
    }

    private void OnEnable()
    {
        if (hasStarted) BeginDialogue();
    }

    private void BeginDialogue()
    {
        if (dialogueText == null || progressText == null || buttonText == null ||
            portrait == null || continueButton == null || nameText == null)
        {
            Debug.LogError("Dialogue prefab is missing a UI reference. Check its controller in Prefab Mode.", this);
            enabled = false;
            return;
        }
        nameText.text = mascotName;
        portrait.texture = idleTexture;
        lineIndex = 0;
        isTyping = false;
        isLoading = false;
        continueButton.interactable = true;
        buttonText.text = "Continue  ›";
        // Reuse the scene's EventSystem; supply one only when this prefab is used in a scene without UI.
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Dialogue EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            dialogueText.text = "Welcome to Elysium!";
            progressText.text = "";
            return;
        }
        ShowLine();
    }

    private void OnDisable()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        isTyping = false;
        if (portrait != null) portrait.texture = idleTexture;
    }

    public void Continue()
    {
        if (isLoading) return;
        if (isTyping)
        {
            StopCoroutine(typing);
            FinishLine();
            return;
        }
        if (dialogueLines != null && lineIndex + 1 < dialogueLines.Length)
        {
            lineIndex++;
            ShowLine();
            return;
        }
        // A blank destination lets the same prefab serve as an in-scene conversation.
        if (string.IsNullOrWhiteSpace(nextScene))
        {
            gameObject.SetActive(false);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"Add '{nextScene}' to the build scene list before continuing.", this);
            dialogueText.text = $"The next scene is unavailable. Please add {nextScene} to the build scene list.";
            return;
        }
        isLoading = true;
        continueButton.interactable = false;
        buttonText.text = "Loading…";
        SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
    }

    private void ShowLine()
    {
        fullLine = (dialogueLines[lineIndex] ?? "").Replace("{name}", mascotName);
        progressText.text = $"{lineIndex + 1} / {dialogueLines.Length}";
        typing = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        float started = Time.unscaledTime;
        int revealed = 0;
        int visibleCount = 0;
        while (revealed < fullLine.Length)
        {
            int target = Mathf.FloorToInt((Time.unscaledTime - started) * Mathf.Max(1, charactersPerSecond));
            while (revealed < fullLine.Length && visibleCount < target)
            {
                // Reveal formatting tags atomically and keep italic text balanced while typing.
                if (fullLine[revealed] == '<' && fullLine.IndexOf('>', revealed) is int end && end >= 0)
                    revealed = end + 1;
                else
                {
                    revealed++;
                    visibleCount++;
                }
            }
            string partial = fullLine.Substring(0, revealed);
            if (partial.LastIndexOf("<i>", System.StringComparison.Ordinal) >
                partial.LastIndexOf("</i>", System.StringComparison.Ordinal)) partial += "</i>";
            dialogueText.text = partial;
            portrait.texture = ((int)((Time.unscaledTime - started) / 0.14f) % 2 == 0)
                ? talkingTexture : idleTexture;
            yield return null;
        }
        FinishLine();
    }

    private void FinishLine()
    {
        isTyping = false;
        dialogueText.text = fullLine;
        portrait.texture = idleTexture;
        typing = null;
    }

}

