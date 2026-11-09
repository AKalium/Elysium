
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CityHappinessHUD : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem grid;
    [SerializeField] private TMP_Text happinessText;
    [SerializeField] private Image happinessBarFill;
    [SerializeField] private TMP_Text breakdownText;

    private readonly string[] serviceIds =
    {
        "school", "hospital", "police_station", "fire_station"
    };

    private void Update()
    {
        if (grid == null || happinessText == null ||
            happinessBarFill == null || breakdownText == null)
            return;

        var buildings = grid.GetPlacedObjects();

        // Check which services exist anywhere in the city.
        bool[] servicesExist = new bool[serviceIds.Length];

        foreach (var item in buildings)
        {
            var placeable = item.Object.GetComponent<GridPlaceable>();

            if (placeable == null || placeable.Definition == null)
                continue;

            for (int i = 0; i < serviceIds.Length; i++)
            {
                if (placeable.Definition.BuildingId == serviceIds[i])
                    servicesExist[i] = true;
            }
        }

        int totalResidents = 0;
        double totalHappiness = 0;
        double totalBase = 0;
        double totalServices = 0;
        double totalParks = 0;
        double totalUnemployment = 0;
        double totalPollution = 0;

        foreach (var item in buildings)
        {
            var apartment = item.Object.GetComponent<GridPlaceable>();

            if (apartment == null || !apartment.IsApartment)
                continue;

            ApartmentStatsUI.CalculateHappiness(
                grid, apartment,
                out bool[] covered,
                out int residents,
                out _, out _,
                out int unemployment,
                out int pollution,
                out int happiness
            );

            if (residents == 0)
                continue;

            int serviceEffect = 0;

            for (int i = 0; i < serviceIds.Length; i++)
            {
                serviceEffect += covered[i]
                    ? 10
                    : (servicesExist[i] ? -7 : -5);
            }

            int parkBonus = 0;

            foreach (var nearby in buildings)
            {
                var building = nearby.Object.GetComponent<GridPlaceable>();

                if (building == null || building.Definition == null)
                    continue;

                if (building.Definition.BuildingId == "park" &&
                    Vector2.Distance(
                        apartment.transform.position,
                        building.transform.position
                    ) <= 8 * grid.CellSize)
                {
                    parkBonus += 5;
                }
            }

            parkBonus = Mathf.Min(15, parkBonus);

            totalResidents += residents;
            totalHappiness += happiness * residents;
            totalBase += apartment.BaseHappiness * residents;
            totalServices += serviceEffect * residents;
            totalParks += parkBonus * residents;
            totalUnemployment += unemployment * residents;
            totalPollution += pollution * residents;
        }

        if (totalResidents == 0)
        {
            happinessText.text = "Happiness: 0 / 100";
            happinessBarFill.fillAmount = 0;
            breakdownText.text =
                "HAPPINESS BREAKDOWN\n\nNo residents yet.";
            return;
        }

        double average = totalHappiness / totalResidents;

        happinessText.text = $"Happiness: {average:0} / 100";
        happinessBarFill.fillAmount = (float)(average / 100.0);

        breakdownText.text =
            "HAPPINESS BREAKDOWN\n\n" +
            $"Base happiness: +{totalBase / totalResidents:0.0}\n" +
            $"Public services: {totalServices / totalResidents:+0.0;-0.0;0.0}\n" +
            $"Nearby parks: +{totalParks / totalResidents:0.0}\n" +
            $"Unemployment: -{totalUnemployment / totalResidents:0.0}\n" +
            $"Pollution: -{totalPollution / totalResidents:0.0}\n\n" +
            $"Overall happiness: {average:0} / 100";
    }
}
