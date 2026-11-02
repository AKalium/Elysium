using UnityEngine;

public class GridPlaceable : MonoBehaviour
{
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [SerializeField] private BuildingCategory category;
    public BuildingCategory Category => category;
    [Header("Placement Limits")]
[Tooltip("Maximum number of this building allowed. 0 means unlimited.")]
[SerializeField, Min(0)] private int maxAllowed = 0;

public int MaxAllowed => maxAllowed;
[Tooltip("Unique ID shared by all instances of this building type.")]
[SerializeField] private string buildingTypeId;

public string BuildingTypeId => buildingTypeId;
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
