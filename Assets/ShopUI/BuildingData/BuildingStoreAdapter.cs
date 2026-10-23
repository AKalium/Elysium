
using System;
using UnityEngine;

public class BuildingStoreAdapter : MonoBehaviour
{
    [Serializable]
    public class BuildingMapping
    {
        public BuildingData building;
        public int placementIndex;
    }

    [Header("Placement System")]
    [SerializeField] private GridPlacementSystem placementSystem;

    [Header("Building Mappings")]
    [SerializeField] private BuildingMapping[] mappings;

    public int GetPlacementCost(BuildingData building)
    {
        if (placementSystem == null || building == null || mappings == null) return -1;
        foreach (var mapping in mappings)
            if (mapping != null && mapping.building == building)
                return placementSystem.GetConstructionCost(mapping.placementIndex);
        return -1;
    }

    public bool SelectBuilding(BuildingData building)
    {
        if (placementSystem == null || building == null || GetPlacementCost(building) < 0)
        {
            Debug.LogWarning("Missing placement system or building data.");
            return false;
        }

        foreach (BuildingMapping mapping in mappings)
        {
            if (mapping.building == building)
            {
                placementSystem.SelectPlaceable(mapping.placementIndex);

                Debug.Log("Placement activated: " + building.BuildingName);
                return true;
            }
        }

        Debug.LogWarning(
            "No placement mapping found for: " + building.BuildingName
        );

        return false;
    }
}
