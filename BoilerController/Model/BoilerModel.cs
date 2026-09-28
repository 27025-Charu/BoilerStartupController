namespace BoilerController.Model
{
    internal class BoilerModel
    {

        public BoilerModel(SystemState state, Switch @switch, Phases phase, TimeSpan? remaining)
        {
            this.state = state;
            this.switchState = @switch;
            this.phase = phase;
            Remaining = remaining;
        }

        public SystemState state { get; set; }
        public Switch switchState { get; set; }
        public Phases phase { get; set; }
        public TimeSpan? Remaining { get; set; }
    }
}
