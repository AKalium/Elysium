using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only clock. UI artists can edit the linked prefab and label.</summary>
public class TimeEconomyHUD : MonoBehaviour
{
    [SerializeField] private TimeManager clock;
    [SerializeField] private Text timeLabel;
    [SerializeField] private RectTransform panel;
    [SerializeField] private bool showDay = true;
    [SerializeField] private Button speedButton;
    [SerializeField] private Text speedLabel;

    public string DisplayText => clock == null ? "" :
        (showDay ? $"Day {clock.Day}  |  {clock.DisplayTime}" : clock.DisplayTime);

    private void Awake()
    {
        if (clock == null) clock = FindAnyObjectByType<TimeManager>();
    }

    private void OnEnable()
    {
        if (speedButton != null) speedButton.onClick.AddListener(CycleSpeed);
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (speedButton != null) speedButton.onClick.RemoveListener(CycleSpeed);
    }

    private void CycleSpeed()
    {
        if (clock != null) clock.CycleSpeed();
        RefreshDisplay();
    }

    private void LateUpdate()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (timeLabel != null) timeLabel.text = DisplayText;
        if (speedLabel != null) speedLabel.text = clock == null ? "1×" : $"{clock.Speed:0}×";
        if (speedButton != null) speedButton.interactable = clock != null;
    }

    public bool ContainsScreenPoint(Vector2 position) => isActiveAndEnabled && panel != null &&
        RectTransformUtility.RectangleContainsScreenPoint(panel, position);
}
