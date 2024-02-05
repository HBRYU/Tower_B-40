using System;

public class Timer
{
    // Properties
    public float Time { get; private set; }
    public bool UsingFixedDeltaTime { get; private set; }

    public Action onTime;

    public Timer(float time, bool usingFixedDeltaTime)
    {
        Time = time;
        UsingFixedDeltaTime = usingFixedDeltaTime;
    }

    public void TimeUpdate(float deltaTime)
    {
        Time -= deltaTime;
    }
}