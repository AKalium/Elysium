using UnityEngine;



public class PollutionSystem : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int naturalDecayPerDay = 5;
    
    [SerializeField] private TimeManager clock;
    
    public int TotalPollution { get; private set; }
    public int LastDailyPollution { get; private set; }

    private void OnEnable()
    {
        if (clock != null)
            clock.Midnight += AddDailyPollution;
    }

    private void OnDisable()
    {
        if (clock != null)
            clock.Midnight -= AddDailyPollution;
    }

    private void AddDailyPollution(int day)
    {
        int dailyTotal = 0;

        var buildings = FindObjectsByType<BuildingPollution>(
            FindObjectsSortMode.None);

        foreach (var building in buildings)
        {
            if (!building.isActiveAndEnabled)
                continue;

            if (building.gameObject.scene != gameObject.scene)
                continue;

            dailyTotal += building.PollutionPerDay;
        }

        LastDailyPollution = dailyTotal;
        TotalPollution = Mathf.Max(
            0,
            TotalPollution + dailyTotal - naturalDecayPerDay
        );

        Debug.Log(
            $"Day {day}: +{dailyTotal} pollution. " +
            $"Total: {TotalPollution}");
    }
}