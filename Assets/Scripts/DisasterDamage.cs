using UnityEngine;

public enum DisasterKind { Flood, Fire }

/// <summary>Leaves occupancy and selling intact while disabling building contributions.</summary>
public sealed class DisasterDamage : MonoBehaviour
{
    [SerializeField] private bool isDamaged;
    [SerializeField] private DisasterKind disaster;
    public bool IsDamaged => isDamaged;
    public DisasterKind Disaster => disaster;

    public void Apply(DisasterKind kind)
    {
        if (isDamaged) return;
        isDamaged = true;
        disaster = kind;
        ApplyTint();
        GameState.Instance?.RefreshPopulation();
    }

    private void Start() { if (isDamaged) ApplyTint(); }

    private void ApplyTint()
    {
        Color tint = disaster == DisasterKind.Fire
            ? new Color32(255, 95, 95, 255) : new Color32(90, 150, 255, 255);
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            renderer.color = tint;
    }
}
