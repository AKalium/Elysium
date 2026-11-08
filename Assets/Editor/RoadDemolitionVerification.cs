using UnityEditor;
using UnityEngine;

public static class RoadDemolitionVerification
{
    [MenuItem("Elysium/Verify Road Demolition")]
    public static void Run()
    {
        CentralRoadVerification.Run();
        Debug.Log("ROAD_DEMOLITION_VERIFICATION_PASSED");
    }
}