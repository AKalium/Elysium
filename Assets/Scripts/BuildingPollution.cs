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

    internal const int InitialFactoryPollution = 8;
    internal const int FirstFactoryPollutionIncreaseDay = 15;
    internal const int FactoryPollutionIncreaseInterval = 3;
    internal const int MaximumFactoryPollution = 18;
    internal const int MaximumFactoryPollutionDay = FirstFactoryPollutionIncreaseDay +
        (MaximumFactoryPollution - InitialFactoryPollution - 1) * FactoryPollutionIncreaseInterval;

    public static int FactoryPollutionForDay(int day)
    {
        if (day < FirstFactoryPollutionIncreaseDay) return InitialFactoryPollution;
        int increases = 1 + (day - FirstFactoryPollutionIncreaseDay) / FactoryPollutionIncreaseInterval;
        return Mathf.Min(MaximumFactoryPollution, InitialFactoryPollution + increases);
    }
}
