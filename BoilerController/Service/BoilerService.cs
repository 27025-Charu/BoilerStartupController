using BoilerController.Model;
using BoilerController.Repository;

namespace BoilerController.Service
{
    /// <summary>
    /// Bpiler service has various methods for starting and stopping the boiler, Reseting the lockout, Simulating boiler error, toggling between open/close, viewing the logs, exiting the application.
    /// </summary>
    internal class BoilerService
    {
        private readonly Logger logger;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private static readonly object _obj = new();
        private SystemState _state = SystemState.Lockout;
        private Switch _switch = Switch.opened;
        private Phases _phase = Phases.none;
        private DateTime _TimeEndPhase;
        public event Action<string>? Notify;
        ResumeLog _resumeLog;
        private CancellationTokenSource cts;
        public static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);
        private Task _runningTask = Task.CompletedTask;

        /// <summary>
        /// Boiler service's constructor that takes the logger and assignes the private field.
        /// </summary>
        /// <param name="logger"></param>
        public BoilerService(Logger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Starts the application by logging this text.
        /// </summary>
        /// <returns>Returns the logging.</returns>
        public Task RunAsync()
        {
            return LoggingAsync("START", "Boilder controller initialized. [State: Lockout, Switch: Open]");
        }

        /// <summary>
        /// Starting the boiler when the switch is closed and the system state is in ready state.
        /// </summary>
        /// <returns>Returns the text stating the current operation status.</returns>
        internal async Task<string> StartBoilerAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                TimeSpan remaining = Duration;
                Phases phase = Phases.prepurge;
                bool resumed = false;
                DateTime end = default;
                CancellationToken token = default;
                string block = string.Empty;
                lock (_obj)
                {
                    if (_switch == Switch.opened)
                    {
                        block = "The switch is opened. For the boiler to start we need to change the switch to closed state.";
                    }
                    else if (_state == SystemState.Running)
                    {
                        block = "The boiler is already in running state.";
                    }
                    else if (_state == SystemState.Lockout)
                    {
                        block = "The boiler can't run if the system is in the lockout state.";
                    }
                    else if (_state == SystemState.Error)
                    {
                        block = "The user simulated error. Reset it to proceed.";
                    }
                    else
                    {
                        if (_state == SystemState.Stopped && _resumeLog is not null)
                        {
                            _phase = _resumeLog.phase;
                            remaining = _resumeLog.remainingTime;
                            resumed = true;
                        }
                        end = DateTime.UtcNow + remaining;
                        _resumeLog = null;
                        _state = SystemState.Running;
                        _phase = phase;
                        _TimeEndPhase = end;
                        cts = new CancellationTokenSource();
                        token = cts.Token;
                    }
                }
                if (block != string.Empty)
                {
                    await BlockAsync("START BLOCKED", block);
                    return block;
                }
                string message = string.Empty;
                if (resumed)
                {
                    message = $"Resumed from the {phase} with the remaining time of {remaining}.";
                }
                else
                {
                    message = "Started the sequence and begun from Phase 1 - Pre-Purge";
                }
                if (resumed)
                {
                    await LoggingAsync("RESUME", message);
                }
                else
                {
                    await LoggingAsync("START", message);
                }
                _runningTask = RunAsync(phase, end, token);
                return message;
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

        /// <summary>
        /// Helper method for the start boiler method
        /// </summary>
        /// <param name="phase">Phase specified</param>
        /// <param name="end">End time. </param>
        /// <param name="token">Cancellation token.</param>
        /// <returns></returns>
        private async Task RunAsync(Phases phase, DateTime end, CancellationToken token)
        {
            try
            {
                while (phase != Phases.operational)
                {
                    lock (_obj)
                    {
                        if (_state != SystemState.Running)
                        {
                            return;
                        }
                        _phase = phase;
                        _TimeEndPhase = end;
                    }

                    await LoggingAsync("PHASE_START", $"{phase} started, ends {end:HH:mm:ss} UTC");
                    await DelayAsync(end, token);
                    await LoggingAsync("PHASE_COMPLETE", $"Phase - {phase} completed.");

                    if (phase == Phases.prepurge)
                    {
                        phase = Phases.ignition;
                    }
                    else
                    {
                        phase = Phases.operational;
                    }
                    end = DateTime.UtcNow + Duration;
                }

                lock (_obj)
                {
                    if (_state != SystemState.Running)
                    {
                        return;
                    }
                    _phase = Phases.operational;
                }

                await LoggingAsync("OPERATIONAL", "Boiler is in operational phase with no timer.");
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("The boiler is cancelled due to stopping or toggling the switch or error or shutdown");
            }
            catch (Exception ex)
            {
                lock (_obj)
                {
                    _state = SystemState.Error;
                }
                await LoggingAsync("ERROR", ex.Message);
            }
        }

        /// <summary>
        /// Delay method for delaying and moving from one phase to the next.
        /// </summary>
        /// <param name="endTime">Phase's end time</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        private async Task DelayAsync(DateTime endTime, CancellationToken token)
        {
            TimeSpan remaining = endTime - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, token);
            }
        }

        /// <summary>
        /// Reseting the lockout to ready state.
        /// </summary>
        /// <returns>Message that states the current state.</returns>
        internal async Task<string> ResetLockoutAsync()
        {
            await _semaphoreSlim.WaitAsync();
            ResumeLog? _previousLog = null;
            try
            {
                string blockMessage = string.Empty;
                lock (_obj)
                {
                    if (_state == SystemState.Ready)
                    {
                        blockMessage = "Boiler is already in the ready state.";
                    }
                    else if (_switch == Switch.opened)
                    {
                        blockMessage = "Switch needs to be closed. Currently it is opened.";
                    }
                    else
                    {
                        _previousLog = _resumeLog;
                        _resumeLog = null;
                        _phase = Phases.none;
                        _state = SystemState.Ready;
                    }
                }
                if (blockMessage != string.Empty)
                {
                    return await BlockAsync("RESET BLOCKED", blockMessage);
                }
                string message = string.Empty;
                if (_previousLog is null)
                {
                    message = "Reset is set to ready";
                    await LoggingAsync("RESET", message);
                }
                else
                {
                    message = $"Reset is set to ready. The current flow is stopped in Phase:{_previousLog.phase}, Remaining Time: {_previousLog.remainingTime}.";
                    await LoggingAsync("RESET", message);
                }
                return message;
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

        /// <summary>
        /// The operation's that is blocked due to some reason is added in the blockmessage and displayed to the user.
        /// </summary>
        /// <param name="v">Phase</param>
        /// <param name="blockMessage">Message due to block</param>
        /// <returns></returns>
        private async Task<string> BlockAsync(string v, string blockMessage)
        {
            await LoggingAsync(v, blockMessage);
            return blockMessage;
        }

        /// <summary>
        /// Simulating the boiler error and then continuing the boiler.
        /// </summary>
        /// <returns>Message that states the current state.</returns>
        internal async Task<string> SimulateBoilerErrorAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                CancellationTokenSource? _cts = null;
                bool flag = false;
                lock (_obj)
                {
                    if (_phase == Phases.operational && _state == SystemState.Running)
                    {
                        _cts = cts;
                        flag = true;
                        _resumeLog = null;
                        _state = SystemState.Error; //Just resetting the state alone. The phase will help to identify in which phase the error occured. So, didn't change that.
                    }
                }
                if (!flag)
                {
                    return await BlockAsync("ERROR BLOCKED", "Errors can only be raised in the operational state.");
                }
                await CancelTokenSource(cts);
                const string message = "Error is simulated in operational state. To proceed go forward with the reset.";
                await LoggingAsync("ERROR RAISED", message);
                return message;
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

        /// <summary>
        /// Stopping the boiler in between the running process and then resuming it using the start boiler method call.
        /// </summary>
        /// <returns>Returns the message that states the current state of the operation.</returns>
        internal async Task<string> StopBoilerAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                ResumeLog temp = null;
                CancellationTokenSource? cts = null;
                lock (_obj)
                {
                    if (_state == SystemState.Running)
                    {
                        TimeSpan remaining = TimeSpan.Zero;
                        if (_phase == Phases.prepurge || _phase == Phases.ignition)
                        {
                            remaining = _TimeEndPhase - DateTime.UtcNow;
                            if (remaining < TimeSpan.Zero)
                            {
                                remaining = TimeSpan.Zero;
                            }
                            temp = new ResumeLog(_phase, remaining);
                            _resumeLog = temp;
                            _state = SystemState.Stopped;
                        }
                    }
                }
                if (temp is null)
                {
                    return await BlockAsync("STOP_BLOCKED", "boiler is not running.");
                }
                string message;
                if (temp.phase == Phases.operational)
                {
                    message = "Stopped in Operational phase. Start will resume from the operational phase itself.";
                }
                else
                {
                    message = $"Stopped in {temp.phase} with {temp.remainingTime} remaining. Start will resume from here.";
                }
                await LoggingAsync("STOP", message);
                return message;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Toggling the switch between open and close.
        /// </summary>
        /// <returns>Returns the message that states the current state of the operation.</returns>
        internal async Task<string> ToggleRunInterlockAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                bool open, wasRunning = false;
                Phases phase = Phases.none;
                CancellationTokenSource? _cts = null;
                lock (_obj)
                {
                    if (_switch == Switch.closed)
                    {
                        _switch = Switch.opened;
                        open = true;
                        if (_state == SystemState.Running)
                        {
                            wasRunning = true;
                        }
                        phase = _phase;
                        if (wasRunning)
                        {
                            _cts = cts;
                            _resumeLog = null;
                        }
                        _state = SystemState.Lockout;
                        _phase = Phases.none;
                    }
                    else
                    {
                        _switch = Switch.closed;
                        open = false;
                    }
                }
                if (wasRunning)
                {
                    await CancelTokenSource(cts);
                }
                string message;
                if (!open)
                {
                    message = "Switch is in closed state. Boiler is in lockout, reset to Ready state.";
                }
                else
                {
                    if (wasRunning)
                    {
                        message = $"Switch is opened. The phase when the toggle happened was {phase}. Close the switch and reset to ready state.";
                    }
                    else
                    {
                        message = $"Switch is opened. The boiler wasn't running. Close the switch and reset to ready state.";
                    }
                }
                await LoggingAsync("TOGGLE SWITCH", message);
                return message;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Cancelling the cancellation token.
        /// </summary>
        /// <param name="cts">Token</param>
        /// <returns>cancelled completed task as result.</returns>
        private async Task CancelTokenSource(CancellationTokenSource cts)
        {
            if (cts == null)
            {
                return;
            }
            cts.Cancel();
        }

        /// <summary>
        /// Retrieving the event log data.
        /// </summary>
        /// <returns>Viewing the event log data from the logged file.</returns>
        internal List<string> ViewEventLogAsync()
        {
            return logger.ReadAll();
        }

        /// <summary>
        /// Is running tells us that the system is in the running state or not.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_obj)
                {
                    return _state == SystemState.Running;
                }
            }
        }

        /// <summary>
        /// Disposing once the exit is called or application is exiting.
        /// </summary>
        /// <returns>Returns the task whether this method is completed or not.</returns>
        internal async Task DisposeAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                lock (_obj)
                {
                    if (_state == SystemState.Running)
                    {
                        _state = SystemState.Stopped;
                    }
                }
                await LoggingAsync("EXIT", "Boiler Controller exiting");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Logging every events that happens in the application.
        /// </summary>
        /// <param name="evtFormat">The method that is being processed.</param>
        /// <param name="message">Error or success message</param>
        /// <returns>returns a task.</returns>
        private async Task LoggingAsync(string evtFormat, string message)
        {
            await logger.WriteAsync(evtFormat, message);
            Notify?.Invoke($"{DateTime.UtcNow:HH:mm:ss} - {evtFormat} - {message}");
        }
    }
}