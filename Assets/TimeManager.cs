using System;
using UnityEngine;

/// <summary>Scene-local clock. Other game systems can subscribe to Midnight.</summary>
public class TimeManager : MonoBehaviour
{
    [SerializeField, Min(1f)] private float secondsPerDay = 300f;
    [SerializeField, Range(0, 23)] private int startingHour = 8;
    [SerializeField, Min(0f)] private float speed = 1f;
    private double minuteOfDay;

    public int Day { get; private set; } = 1;
    public int Hour => (int)minuteOfDay / 60;
    public int Minute => (int)minuteOfDay % 60;
    public bool Paused { get; set; }
    public float Speed => speed;

    public void CycleSpeed()
    {
        speed = speed < 2f ? 2f : speed < 4f ? 4f : 1f;
    }
    public event Action<int> Midnight;
    public string DisplayTime => $"{(Hour % 12 == 0 ? 12 : Hour % 12)}:{Minute:00} {(Hour < 12 ? "AM" : "PM")}";

    private void Awake() => minuteOfDay = startingHour * 60;
    private void Update() => AdvanceTime(Time.deltaTime);

    // Public for simulation/testing; scaled delta time also respects Unity's pause.
    public void AdvanceTime(double elapsedSeconds)
    {
        if (Paused || elapsedSeconds <= 0 || double.IsNaN(elapsedSeconds) ||
            double.IsInfinity(elapsedSeconds)) return;
        double minutes = elapsedSeconds * 1440d / Math.Max(1d, secondsPerDay) * Math.Max(0d, speed);
        while (minutes >= 1440d - minuteOfDay)
        {
            minutes -= 1440d - minuteOfDay;
            minuteOfDay = 0;
            Day++;
            Midnight?.Invoke(Day);
        }
        minuteOfDay += minutes;
    }
}