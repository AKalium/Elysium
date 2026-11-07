using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only map snapshots used by indicators and demolition previews.</summary>
public readonly struct PlacedMapObject
{
    public GameObject Object { get; }
    public Vector2Int Anchor { get; }
    public Vector2Int Footprint { get; }
    public bool IsRoad { get; }
    public PlacedMapObject(GameObject obj, Vector2Int anchor, Vector2Int footprint, bool isRoad)
    { Object = obj; Anchor = anchor; Footprint = footprint; IsRoad = isRoad; }
}

public sealed class DemolitionPlan
{
    public IReadOnlyList<PlacedMapObject> Targets { get; }
    public int RoadCount { get; }
    public int BuildingCount { get; }
    public long Refund { get; }
    public bool RemovesCentralRoad { get; }
    public DemolitionPlan(IEnumerable<PlacedMapObject> targets, long refund, bool removesCentralRoad)
    {
        var copy = new List<PlacedMapObject>(targets);
        Targets = copy.AsReadOnly(); Refund = refund; RemovesCentralRoad = removesCentralRoad;
        foreach (var target in copy) { if (target.IsRoad) RoadCount++; else BuildingCount++; }
    }
}
