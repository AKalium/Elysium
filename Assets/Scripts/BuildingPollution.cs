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

    public PollutionTier Tier => tier;
    public int PollutionPerDay => Mathf.Max(0, pollutionPerDay);

}