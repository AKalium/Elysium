using UnityEngine;

public class GridPlaceable : MonoBehaviour
{
    [SerializeField] private Vector2Int footprint = Vector2Int.one;

    public Vector2Int Footprint => new Vector2Int(
        Mathf.Max(1, footprint.x),
        Mathf.Max(1, footprint.y));

    private void OnValidate()
    {
        footprint.x = Mathf.Max(1, footprint.x);
        footprint.y = Mathf.Max(1, footprint.y);
    }
}
