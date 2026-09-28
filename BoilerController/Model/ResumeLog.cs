namespace BoilerController.Model
{
    public record ResumeLog
    {
        public Phases phase { get; set; }
        public TimeSpan remainingTime { get; set; }
    }
}
