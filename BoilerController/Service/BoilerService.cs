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
        CancellationTokenSource cts = new CancellationTokenSource();

        public BoilerService(Logger logger)
        {
            this.logger = logger;
        }
        internal Task RunAsync()
        {
            return LoggingAsync("START", "Boilder controller initialized. [State: Lockout, Switch: Open]");
        }

        internal async Task StartBoilerAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                while (true)
                {

                }
            }
            finally
            {
                _semaphoreSlim?.Release();
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
                    return await BlockAsync("Reset blocked", blockMessage);
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
                ;
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

        internal void SimulateBoilerErrorAsync()
        {

        }

        internal void StopBoilerAsync()
        {
        }

        internal void ToggleRunInterlock()
        {
        }

        internal void ToggleRunInterlockAsync()
        {

        }

        internal List<string> ViewEventLogAsync()
        {
            return logger.ReadAll();
        }

        internal void DisposeAsync()
        {

        }

        private async Task LoggingAsync(string evtFormat, string message)
        {
            await logger.WriteAsync(evtFormat, message);
            Notify?.Invoke($"{DateTime.UtcNow:HH:mm:ss} - {evtFormat,-15} - {message}");
        }
    }
}
