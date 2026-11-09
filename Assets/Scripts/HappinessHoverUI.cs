
using UnityEngine;
using UnityEngine.InputSystem;

public class HappinessHoverUI : MonoBehaviour
{
    [SerializeField] private RectTransform happinessPanel;
    [SerializeField] private GameObject breakdownPanel;

    private RectTransform breakdownRect;
    private float lastHoverTime = -100f;

    private void Start()
    {
        if (breakdownPanel == null) return;

        breakdownRect = breakdownPanel.GetComponent<RectTransform>();
        breakdownPanel.SetActive(false);
    }

    private void Update()
    {
        if (happinessPanel == null || breakdownPanel == null ||
            breakdownRect == null || Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        bool overHappiness = RectTransformUtility.RectangleContainsScreenPoint(
            happinessPanel, mousePosition, null);

        bool overBreakdown = breakdownPanel.activeSelf &&
    RectTransformUtility.RectangleContainsScreenPoint(
        breakdownRect, mousePosition, null);

        if (overHappiness || overBreakdown)
            lastHoverTime = Time.unscaledTime;

        bool show = overHappiness || overBreakdown ||
                    Time.unscaledTime - lastHoverTime < 0.2f;

        if (breakdownPanel.activeSelf != show)
            breakdownPanel.SetActive(show);
    }
}
