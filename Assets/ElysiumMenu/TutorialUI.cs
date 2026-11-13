using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public GameObject tutorialPanel;

    internal bool IsOpen => tutorialPanel != null && tutorialPanel.activeInHierarchy;

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }
}