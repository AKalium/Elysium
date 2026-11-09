using UnityEngine;

public class GridPlaceable : MonoBehaviour
{
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [SerializeField] private BuildingCategory category;
    public BuildingCategory Category => category;
    [SerializeField] private BuildingData definition;
    public BuildingData Definition => definition;
    [Header("Placement Limits")]
[Tooltip("Maximum number of this building allowed. 0 means unlimited.")]
[SerializeField, Min(0)] private int maxAllowed = 0;

public int MaxAllowed => maxAllowed;
[Tooltip("Unique ID shared by all instances of this building type.")]
[SerializeField] private string buildingTypeId;

public string BuildingTypeId => buildingTypeId;
    [Tooltip("Population contributed while this house is active. Use 0 for non-housing buildings.")]
    [SerializeField, Min(0)] private int residentsProvided;
    public bool IsOperational => !TryGetComponent<DisasterDamage>(out var damage) || !damage.IsDamaged;
    public int ResidentsProvided => IsOperational ? Mathf.Max(0, residentsProvided) : 0;

    public bool IsApartment => Definition != null && Definition.BuildingId == "apartment";

    public bool IsFoundingApartment { get; private set; }
    public int BaseHappiness => IsFoundingApartment ? 100 : 60;
    public int HousingCapacity => !IsOperational ? 0 : Definition != null && Definition.BuildingId == "apartment"
        ? Mathf.Max(10, ResidentsProvided) : ResidentsProvided;

    public void InitializeFoundingApartment()
    {
        IsFoundingApartment = true;
        residentsProvided = 10;
    }

    public void InitializeEmptyApartment() => residentsProvided = 0;

    public bool TryAddResident()
    {
        if (!IsOperational || !IsApartment || ResidentsProvided >= HousingCapacity) return false;
        residentsProvided++;
        return true;
    }

    public void RemoveResidents(int count)
    {
        residentsProvided = Mathf.Max(0, ResidentsProvided - Mathf.Max(0, count));
    }

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
