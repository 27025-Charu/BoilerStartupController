namespace BoilerController.Model
{
    public record ResumeLog
    {

        public ResumeLog(Phases phase, TimeSpan remaining)
        {
            this.phase = phase;
            this.remainingTime = remaining;
        }

        public Phases phase { get; set; }
        public TimeSpan remainingTime { get; set; }
    }
}
