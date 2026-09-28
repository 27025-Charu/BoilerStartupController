namespace BoilerController.Model
{
    public record ResumeLog
    {

        public ResumeLog(Phases phase, TimeSpan remaining)
        {
            this.phase = phase;
            this.remainingTime = remaining;
        }

        /// <summary>
        /// Current phase where the boiler is in.
        /// </summary>
        public Phases phase { get; set; }
        /// <summary>
        /// Remaining time when the boiler is running per phase.
        /// </summary>
        public TimeSpan remainingTime { get; set; }
    }
}
