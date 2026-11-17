using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public GameObject tutorialPanel;

    [SerializeField] private AudioClip _Clip;

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);

        if (_Clip == null) return;

        if (!TryGetComponent<AudioSource>(out var speaker))
        {
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
        }

        speaker.PlayOneShot(_Clip);
    }
}
