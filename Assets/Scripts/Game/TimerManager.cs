using System.Collections.Generic;

public class TimerManager
{
    public List<Timer> timers = new ();

    public void UpdateTimers(float deltaTime)
    {
        foreach (var timer in timers)
        {
            if(!timer.UsingFixedDeltaTime)
                timer.TimeUpdate(deltaTime);
        }
    }
    
    public void FixedUpdateTimers(float fixedDeltaTime)
    {
        foreach (var timer in timers)
        {
            if(timer.UsingFixedDeltaTime)
                timer.TimeUpdate(fixedDeltaTime);
        }
    }
}