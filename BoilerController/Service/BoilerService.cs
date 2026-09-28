using BoilerController.Model;
using BoilerController.Repository;

namespace BoilerController.Service
{
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
        ResumeLog _resumeLog = new ResumeLog();
        private CancellationTokenSource cts;
        public static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);
        private Task _runningTask = Task.CompletedTask;

        public BoilerService(Logger logger)
        {
            this.logger = logger;
        }
        internal Task RunAsync()
        {
            return LoggingAsync("START", "Boilder controller initialized. [State: Lockout, Switch: Open]");
        }

        internal async Task<string> StartBoilerAsync()
        {
            TimeSpan remaining = Duration;
            Phases phase = Phases.prepurge;
            bool resumed = false;
            DateTime end = default;
            CancellationToken token = default;
            await _semaphoreSlim.WaitAsync();
            try
            {
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
                if (block is not null)
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
                await LoggingAsync(resumed ? "RESUME" : "START", message);

                _runningTask = RunAsync(phase, end, token);
                return message;
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

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
                    await LoggingAsync("PHASE_COMPLETE", $"Phase - {phase} completed");

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

        private async Task DelayAsync(DateTime endTime, CancellationToken token)
        {
            TimeSpan remaining = endTime - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, token);
            }
        }

        internal async Task<string> ResetLockoutAsync()
        {
            await _semaphoreSlim.WaitAsync();
            ResumeLog _previousLog = null;
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
                if (blockMessage is not null)
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

        private async Task<string> BlockAsync(string v, string blockMessage)
        {
            await LoggingAsync(v, blockMessage);
            return blockMessage;
        }

        internal async Task<string> SimulateBoilerErrorAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                bool flag = false;
                lock (_obj)
                {
                    if (_phase == Phases.operational && _state == SystemState.Running)
                    {
                        flag = true;
                        _resumeLog = null;
                        _state = SystemState.Error; //Just resetting the state alone. The phase will help to identify in which phase the error occured. So, didn't change that.
                    }
                }
                if (!flag)
                {
                    return await BlockAsync("ERROR BLOCKED", "Errors can only be raised in the operational state.");
                }

                const string message = "Error is simulated in operational state. To proceed go forward with the reset.";
                await LoggingAsync("ERROR RAISED", message);
                return message;
            }
            finally
            {
                _semaphoreSlim?.Release();
            }
        }

        internal void StopBoilerAsync()
        {
        }

        internal void ToggleRunInterlockAsync()
        {

        }

        internal List<string> ViewEventLogAsync()
        {
            return logger.ReadAll();
        }

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

        private async Task LoggingAsync(string evtFormat, string message)
        {
            await logger.WriteAsync(evtFormat, message);
            Notify?.Invoke($"{DateTime.UtcNow:HH:mm:ss} - {evtFormat,-15} - {message}");
        }
    }
}
