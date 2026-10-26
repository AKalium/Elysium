using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only clock. UI artists can edit the linked prefab and label.</summary>
public class TimeEconomyHUD : MonoBehaviour
{
    [SerializeField] private TimeManager clock;
    [SerializeField] private Text timeLabel;
    [SerializeField] private RectTransform panel;
    [SerializeField] private bool showDay = true;

    public string DisplayText => clock == null ? "" :
        (showDay ? $"Day {clock.Day}  |  {clock.DisplayTime}" : clock.DisplayTime);

    private void Awake()
    {
        if (clock == null) clock = FindAnyObjectByType<TimeManager>();
    }

    private void LateUpdate()
    {
        if (timeLabel != null) timeLabel.text = DisplayText;
    }

    public bool ContainsScreenPoint(Vector2 position) => isActiveAndEnabled && panel != null &&
        RectTransformUtility.RectangleContainsScreenPoint(panel, position);
}
