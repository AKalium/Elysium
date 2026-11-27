internal static class CityRuleChanges
{
    public const int LowHappinessDepartureDay = 3;
    public const int UnemploymentDepartureDay = 6;
    public const int FullMissingServicePenaltyDay = 31;

    // Save files track how many announcements have been seen, in chronological order.
    public static readonly (int Day, string Message)[] Announcements =
    {
        (LowHappinessDepartureDay,
            $"From day {LowHappinessDepartureDay} onward, apartments with 5 or more residents and happiness below 30 lose 2 residents each night."),
        (UnemploymentDepartureDay,
            $"From day {UnemploymentDepartureDay} onward, apartments with 5 or more residents and nobody employed lose 2 residents each night. If both penalties apply, 4 residents leave."),
        (DisasterSystem.FirstDisasterDay,
            $"From day {DisasterSystem.FirstDisasterDay} onward, floods can occur at {DisasterSystem.FloodPollutionThreshold} total pollution and fires at {DisasterSystem.FirePollutionThreshold}."),
        (BuildingPollution.FirstFactoryPollutionIncreaseDay,
            $"Starting tonight, each factory produces {BuildingPollution.InitialFactoryPollution + 1} pollution. That rate increases by 1 every {BuildingPollution.FactoryPollutionIncreaseInterval} days until it reaches {BuildingPollution.MaximumFactoryPollution} on day {BuildingPollution.MaximumFactoryPollutionDay}. More pollution lowers apartment happiness."),
        (FullMissingServicePenaltyDay,
            $"The first {FullMissingServicePenaltyDay - 1} days were a grace period. Starting today, missing services reduce apartment happiness by the full penalty.")
    };
}
