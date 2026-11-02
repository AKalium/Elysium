
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBuilding",
    menuName = "Elysium/Building Data"
)]
public class BuildingData : ScriptableObject
{
    public enum BuildingCategory
    {
        Residential,
        Commercial,
        Services
    }

    [Header("Building Information")]
    [SerializeField] private string buildingId;
    [SerializeField] private string buildingName;
    [SerializeField] private int price;
    [Header("Daily Finances")]
    [SerializeField, Min(0)] private int dailyIncome;
    [SerializeField, Min(0)] private int dailyUpkeep;
    [SerializeField] private BuildingCategory category;

    [Header("Visual Assets")]
    [SerializeField] private Sprite buildingIcon;
    [SerializeField] private GameObject buildingPrefab;

    public string BuildingId => buildingId;
    public string BuildingName => buildingName;
    public int Price => Mathf.Max(0, price);
    public int DailyIncome => Mathf.Max(0, dailyIncome);
    public int DailyUpkeep => Mathf.Max(0, dailyUpkeep);
    public long DailyNet => (long)DailyIncome - DailyUpkeep;
    public BuildingCategory Category => category;
    public Sprite BuildingIcon => buildingIcon;
    public GameObject BuildingPrefab => buildingPrefab;
}
