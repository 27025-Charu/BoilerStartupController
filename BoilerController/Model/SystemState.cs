namespace BoilerController.Model
{
    public enum SystemState
    {
        /// <summary>
        /// Initial state.
        /// </summary>
        Lockout = 1,
        /// <summary>
        /// State when the boiler starts.
        /// </summary>
        Ready,
        /// <summary>
        /// State when the boiler is running (in any phase).
        /// </summary>
        Running,
        /// <summary>
        /// State when the stop is called.
        /// </summary>
        Stopped,
        /// <summary>
        /// State when the error is called.
        /// </summary>
        Error,
    }
}
