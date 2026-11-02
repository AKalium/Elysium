using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Drag a window by its header, using canvas coordinates and keeping it on screen.</summary>
public sealed class DraggableBudgetPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform panel;
    private RectTransform parent;
    private Vector2 pointerOffset;
    private bool dragging;
    private readonly Vector3[] corners = new Vector3[4];

    public void SetPanel(RectTransform value) => panel = value;

    public void OnBeginDrag(PointerEventData data)
    {
        dragging = false;
        if (data.button != PointerEventData.InputButton.Left || panel == null) return;
        parent = panel.parent as RectTransform;
        if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, data.position, data.pressEventCamera, out var point)) return;
        pointerOffset = (Vector2)panel.localPosition - point;
        dragging = true;
    }

    public void OnDrag(PointerEventData data)
    {
        if (!dragging || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, data.position, data.pressEventCamera, out var point)) return;
        panel.localPosition = new Vector3(point.x + pointerOffset.x, point.y + pointerOffset.y, panel.localPosition.z);
        panel.GetWorldCorners(corners);
        var min = (Vector2)parent.InverseTransformPoint(corners[0]);
        var max = (Vector2)parent.InverseTransformPoint(corners[2]);
        var bounds = parent.rect;
        float dx = max.x - min.x > bounds.width ? bounds.center.x - (min.x + max.x) / 2f
            : Mathf.Clamp(0, bounds.xMin - min.x, bounds.xMax - max.x);
        float dy = max.y - min.y > bounds.height ? bounds.center.y - (min.y + max.y) / 2f
            : Mathf.Clamp(0, bounds.yMin - min.y, bounds.yMax - max.y);
        panel.localPosition += new Vector3(dx, dy, 0);
    }

    public void OnEndDrag(PointerEventData data) => dragging = false;
    private void OnDisable() => dragging = false;
}
