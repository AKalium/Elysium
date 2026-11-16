using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public GameObject tutorialPanel;

    [SerializeField] private AudioClip _Clip;

    internal bool IsOpen => tutorialPanel != null && tutorialPanel.activeInHierarchy;

    public void CloseTutorial()
    {
        var speaker = GetComponent<AudioSource>();
        if (speaker != null)
        {
            speaker.clip = _Clip;
            speaker.Play();
        }

        tutorialPanel.SetActive(false);
    }
}
