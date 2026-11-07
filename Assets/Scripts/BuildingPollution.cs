using UnityEngine;

public enum PollutionTier
{
    CleanEnergy,
    MediumPollution,
    HighPollution
}

public class BuildingPollution : MonoBehaviour
{
    [SerializeField]
    private PollutionTier tier;

    [SerializeField, Min(0)]
    private int pollutionPerDay;

    [SerializeField, Min(0)] private int pollutionRemovalPerDay;
    public int PollutionRemovalPerDay => Mathf.Max(0, pollutionRemovalPerDay);

    public PollutionTier Tier => tier;
    public int PollutionPerDay => Mathf.Max(0, pollutionPerDay);

    public int GetPollutionForDay(int day)
    {
        var building = GetComponent<GridPlaceable>();
        bool factory = building != null && building.Definition != null &&
            building.Definition.BuildingId == "factory";
        return factory && day >= 1 && day <= 30 ? 8 : PollutionPerDay;
    }

}