// The lesson advances through actions and race progress, rather than timed slides.
public class DrivingIntroductionProgress
{
    public enum Lesson { Accelerate, Steer, Brake, Finish, Complete }
    public Lesson Current { get; private set; }
    bool accelerated;
    bool steered;
    bool braked;
    float brakingStartSpeed;

    public void Observe(bool raceStarted, bool finished, int checkpoints, float speed,
        bool accelerate, bool steer, bool brake)
    {
        if (finished) { this.Current = Lesson.Complete; return; }
        // Remember driving to the start gate, but keep the first lesson through the countdown.
        this.accelerated |= accelerate && speed > 1f;
        if (!raceStarted || this.Current == Lesson.Complete) { return; }
        this.steered |= steer && speed > 1f;
        if (brake)
        {
            if (speed > this.brakingStartSpeed) { this.brakingStartSpeed = speed; }
            this.braked |= this.brakingStartSpeed > 1f && speed < this.brakingStartSpeed - 0.5f;
        }
        else { this.brakingStartSpeed = 0f; }
        // Explain turns on the opening straight, before reaching the first checkpoint/bend.
        if (this.Current == Lesson.Accelerate && this.accelerated) { this.Current = Lesson.Steer; }
        if (this.Current == Lesson.Steer && this.steered && checkpoints >= 1) { this.Current = Lesson.Brake; }
        if (this.Current == Lesson.Brake && this.braked) { this.Current = Lesson.Finish; }
    }
}
