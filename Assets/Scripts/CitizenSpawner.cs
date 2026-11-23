
using System;
using UnityEngine;

[Serializable]
public class CitizenAppearance
{
    public string characterName;

    public Sprite[] walkUp;
    public Sprite[] walkDown;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;
}

public class CitizenSpawner : MonoBehaviour
{
    [Header("Citizen Settings")]
    [SerializeField] private GameObject citizenPrefab;
    [SerializeField, Min(0)] private int citizenCount = 5;
    [SerializeField, Min(1)] private int maxCitizens = 20;

    [Header("Character Variations")]
    [SerializeField] private CitizenAppearance[] appearances;
    [Header("Residential Spawning")]
[SerializeField] private float refreshInterval = 0.5f;

private readonly System.Collections.Generic.List<GameObject> spawnedCitizens = new();
private float nextRefresh;

    
private void Start()
{
    if (citizenPrefab == null)
    {
        Debug.LogError("CitizenSpawner: No citizen prefab assigned!");
        enabled = false;
        return;
    }

    UpdateCitizens();
}

private void Update()
{
    if (Time.time < nextRefresh)
        return;

    nextRefresh = Time.time + refreshInterval;
    UpdateCitizens();
}

private void UpdateCitizens()
{
    if (GameState.Instance == null)
        return;

    int residentialCount = GameState.Instance.ResidentialCount;
    int targetCount = Mathf.Min(residentialCount, maxCitizens);

    // Spawn citizens when residential buildings are added.
    while (spawnedCitizens.Count < targetCount)
    {
        GameObject citizen = Instantiate(
            citizenPrefab,
            transform.position,
            Quaternion.identity
        );

        if (appearances != null && appearances.Length > 0)
        {
            CitizenAppearance appearance =
                appearances[UnityEngine.Random.Range(0, appearances.Length)];

            CitizenAnimation animation =
                citizen.GetComponent<CitizenAnimation>();

            if (animation != null)
            {
                animation.SetSprites(
                    appearance.walkUp,
                    appearance.walkDown,
                    appearance.walkLeft,
                    appearance.walkRight
                );
            }
        }

        spawnedCitizens.Add(citizen);
    }

    // Remove citizens when residential buildings are demolished.
    while (spawnedCitizens.Count > targetCount)
    {
        int lastIndex = spawnedCitizens.Count - 1;

        Destroy(spawnedCitizens[lastIndex]);
        spawnedCitizens.RemoveAt(lastIndex);
    }
}

}
