namespace BoilerController.Model
{
    /// <summary>
    /// Boiler current details. [Thought of using it for displaying in the console for count down]
    /// </summary>
    internal class BoilerModel
    {

        public BoilerModel(SystemState state, Switch @switch, Phases phase, TimeSpan? remaining)
        {
            this.state = state;
            this.switchState = @switch;
            this.phase = phase;
            Remaining = remaining;
        }

        /// <summary>
        /// State where the system is in.
        /// </summary>
        public SystemState state { get; set; }
        /// <summary>
        /// Switch state (open/closed)
        /// </summary>
        public Switch switchState { get; set; }
        /// <summary>
        /// Phase where the boiler is in.
        /// </summary>
        public Phases phase { get; set; }
        /// <summary>
        /// Remaining time for the boiler.
        /// </summary>
        public TimeSpan? Remaining { get; set; }
    }
}
