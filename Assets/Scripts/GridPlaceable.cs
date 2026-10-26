using UnityEngine;

public class GridPlaceable : MonoBehaviour
{
    [SerializeField] private Vector2Int footprint = Vector2Int.one;

    [SerializeField] private BuildingCategory category;
    public BuildingCategory Category => category;

    [Tooltip("Population contributed while this house is active. Use 0 for non-housing buildings.")]
    [SerializeField, Min(0)] private int residentsProvided;
    public int ResidentsProvided => Mathf.Max(0, residentsProvided);

    private void OnEnable() => GameState.Instance?.RefreshPopulation();
    private void OnDisable() => GameState.Instance?.RefreshPopulation();

    public Vector2Int Footprint => new Vector2Int(
        Mathf.Max(1, footprint.x),
        Mathf.Max(1, footprint.y));

    private void OnValidate()
    {
        footprint.x = Mathf.Max(1, footprint.x);
        footprint.y = Mathf.Max(1, footprint.y);
    }
}
