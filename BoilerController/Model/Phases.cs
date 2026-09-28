namespace BoilerController.Model
{
    public enum Phases
    {
        /// <summary>
        /// Idle state
        /// </summary>
        none = 1,
        /// <summary>
        /// Phase 1 when the boiler starts running.
        /// </summary>
        prepurge,
        /// <summary>
        /// Phase 2 when the boiler starts running.
        /// </summary>
        ignition,
        /// <summary>
        /// Phase 3 when the boiler starts running.
        /// </summary>
        operational,
    }
}
