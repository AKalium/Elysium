using UnityEngine;

[DisallowMultipleComponent]
public sealed class BuildingIncome : MonoBehaviour
{
    private CurrencySystem wallet;
    private BuildingIncomeTimer timer;
    private int activatedFrame;

    public decimal IncomePerSecond => timer == null ? 0m : timer.Payout / (decimal)timer.Interval;

    public void BeginProduction(CurrencySystem owner, GridPlaceable settings)
    {
        StopProduction();
        if (owner == null || settings == null) return;
        wallet = owner;
        timer = new BuildingIncomeTimer(settings.IncomeAmount, settings.IncomeIntervalSeconds);
        activatedFrame = Time.frameCount;
        if (isActiveAndEnabled) wallet.RegisterProducer(this);
    }

    public decimal Advance(double seconds) =>
        timer == null || Time.frameCount == activatedFrame ? 0m : timer.Advance(seconds);

    public void StopProduction()
    {
        if (wallet != null) wallet.UnregisterProducer(this);
        wallet = null;
        timer = null;
    }

    private void OnEnable()
    {
        activatedFrame = Time.frameCount;
        if (wallet != null && timer != null) wallet.RegisterProducer(this);
    }

    private void OnDisable()
    {
        if (wallet != null) wallet.UnregisterProducer(this);
    }

    private void OnDestroy() => StopProduction();
}

