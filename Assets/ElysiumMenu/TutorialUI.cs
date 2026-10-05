using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public GameObject tutorialPanel;

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }
}