using UnityEngine;

public class GridPlaceable : MonoBehaviour
{
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [Header("Building role")]
    [SerializeField] private bool isRoad;
    [SerializeField] private bool requiresRoad = true;
    [SerializeField, Min(0)] private int populationCapacity;
    public bool IsRoad => isRoad;
    public bool RequiresRoad => requiresRoad && !isRoad;
    public int PopulationCapacity => Mathf.Max(0, populationCapacity);
    [Header("Income (per placed building)")]
    [SerializeField, Min(0f)] private float incomeAmount = 0f;
    [SerializeField, Min(0.01f)] private float incomeIntervalSeconds = 1f;

    public decimal IncomeAmount => float.IsNaN(incomeAmount) || float.IsInfinity(incomeAmount)
        ? 0m : System.Math.Round((decimal)Mathf.Clamp(incomeAmount, 0f, 1000000f), 2,
            System.MidpointRounding.AwayFromZero);
    public double IncomeIntervalSeconds => float.IsNaN(incomeIntervalSeconds) || float.IsInfinity(incomeIntervalSeconds)
        ? 1d : System.Math.Min(86400d, System.Math.Max(0.01d, incomeIntervalSeconds));

    public Vector2Int Footprint => new Vector2Int(
        Mathf.Max(1, footprint.x),
        Mathf.Max(1, footprint.y));

    private void OnValidate()
    {
        footprint.x = Mathf.Max(1, footprint.x);
        footprint.y = Mathf.Max(1, footprint.y);
        incomeAmount = Mathf.Clamp(incomeAmount, 0f, 1000000f);
        incomeIntervalSeconds = Mathf.Clamp(incomeIntervalSeconds, 0.01f, 86400f);
    }
}

