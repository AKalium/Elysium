using UnityEngine;

public class GridPlaceable : MonoBehaviour
{
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [SerializeField] private BuildingCategory category;
    public BuildingCategory Category => category;
    [SerializeField] private BuildingData definition;
    public BuildingData Definition => definition;
    public bool IsStoneBridge => definition != null && definition.BuildingId == "stone_bridge";
    public bool RequiresWater => IsBridge || (definition != null && definition.BuildingId == "water_park");
    public bool IsBridge => definition != null && (definition.BuildingId == "bridge" || IsStoneBridge);
    public bool IsMayorsOffice => category == BuildingCategory.MayorsOffice ||
        (definition != null && definition.BuildingId == "city_hall");
    public int OfficeLevel { get; private set; } = 1;
    public const int OfficeUpgradeCost = 1000;
    public const int BridgeUpgradeCost = 100;
    internal void UpgradeBridgeDefinition(BuildingData stone) => definition = stone;

    public bool TryUpgradeOffice(CurrencySystem wallet)
    {
        if (!IsMayorsOffice || OfficeLevel >= 2 || !IsOperational || wallet == null) return false;
        var sprite = Resources.Load<Sprite>("MayorsOfficeLevel2");
        var renderer = GetComponent<SpriteRenderer>();
        if (sprite == null || renderer == null || !wallet.TrySpend(OfficeUpgradeCost)) return false;
        OfficeLevel = 2;
        Vector3 size = renderer.sprite.bounds.size;
        Vector3 scale = transform.localScale;
        renderer.sprite = sprite;
        transform.localScale = new Vector3(scale.x * size.x / sprite.bounds.size.x,
            scale.y * size.y / sprite.bounds.size.y, scale.z);
        return true;
    }
    public bool IsRoad => definition != null && (definition.BuildingId == "road" || IsBridge);
    [Header("Placement Limits")]
[Tooltip("Maximum number of this building allowed. 0 means unlimited.")]
[SerializeField, Min(0)] private int maxAllowed = 0;

public int MaxAllowed => maxAllowed;
[Tooltip("Unique ID shared by all instances of this building type.")]
[SerializeField] private string buildingTypeId;

public string BuildingTypeId => buildingTypeId;
    [Tooltip("Population contributed while this house is active. Use 0 for non-housing buildings.")]
    [SerializeField, Min(0)] private int residentsProvided;
    public bool IsTemporarilyUnavailable { get; private set; }
    public bool IsDamaged => TryGetComponent<DisasterDamage>(out var damage) && damage.IsDamaged;
    public bool IsOperational => !IsTemporarilyUnavailable && !IsDamaged;
    public void SetTemporarilyUnavailable(bool unavailable)
    {
        if (IsTemporarilyUnavailable == unavailable) return;
        IsTemporarilyUnavailable = unavailable;
        // Direct disaster damage keeps its own red/blue tint.
        if (!IsDamaged)
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                renderer.color = unavailable ? new Color32(155, 155, 155, 255) : Color.white;
    }
    public int ResidentsProvided => IsOperational ? Mathf.Max(0, residentsProvided) : 0;

    public bool IsApartment => Definition != null && Definition.BuildingId == "apartment";

    public bool IsFoundingApartment { get; private set; }
    [SerializeField, Min(1)] private int constructionDay = 1;
    public int ConstructionDay => constructionDay;
    public void InitializeConstructionDay(int day) => constructionDay = Mathf.Max(1, day);
    private int CurrentSimulationDay => FindAnyObjectByType<TimeManager>()?.SimulationDay ?? 1;
    public static int FoundingBaseHappiness(int day) => 60 + Mathf.Clamp(40 - 5 * Mathf.Max(0, day - 10), 0, 40);
    public int BaseHappiness => IsFoundingApartment ? FoundingBaseHappiness(CurrentSimulationDay) : 60;
    public double UpkeepForDay(int day) => Definition == null ? 0 :
        Definition.BuildingId == "factory" && day - constructionDay < 10 ? 0 : Definition.DailyUpkeep;
    public int HousingCapacity => !IsOperational ? 0 : Definition != null && Definition.BuildingId == "apartment"
        ? Mathf.Max(10, ResidentsProvided) : ResidentsProvided;

    public void InitializeFoundingApartment()
    {
        IsFoundingApartment = true;
        residentsProvided = 8;
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
