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
        return factory ? FactoryPollutionForDay(day) : PollutionPerDay;
    }

    public static int FactoryPollutionForDay(int day)
    {
        if (day < 15) return 8;
        return 9 + Mathf.Min(9, (day - 15) / 3);
    }

}
