using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    public GameObject tutorialPanel;

    //AUDIO
    private AudioSource _Speaker;
    [SerializeField] private AudioClip _Clip;

    public void CloseTutorial()
    {
        //Initialize audio
        _Speaker = GetComponent<AudioSource>();

        if(_Speaker == null)
            Debug.Log("Bananas in my pajamas");
        else
            _Speaker.clip = _Clip;

        //play audio
        _Speaker.Play();

        tutorialPanel.SetActive(false);
    }
}