
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
    [SerializeField] private BuildingCategory category;

    [Header("Visual Assets")]
    [SerializeField] private Sprite buildingIcon;
    [SerializeField] private GameObject buildingPrefab;

    public string BuildingId => buildingId;
    public string BuildingName => buildingName;
    public int Price => price;
    public BuildingCategory Category => category;
    public Sprite BuildingIcon => buildingIcon;
    public GameObject BuildingPrefab => buildingPrefab;
}
