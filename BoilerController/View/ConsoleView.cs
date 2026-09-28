using BoilerController.Service;

namespace BoilerController.View
{
    internal class ConsoleView
    {
        private BoilerService _service;
        private readonly Queue<string> _events = new();
        private readonly object _eventsLock = new();
        private volatile bool _confirmingToggle;
        private volatile string _message;
        public ConsoleView(BoilerService service)
        {
            this._service = service;
        }

        internal async Task ExecuteAsync()
        {
            Console.WriteLine($@"=========================================
MAIN MENU
=========================================
[A] START BOILER SEQUENCE
[B] STOP BOILER SEQUENCE
[C] SIMULATE BOILER ERROR
[D] TOGGLE RUN INTERLOCK SWITCH
[E] RESET LOCKOUT
[F] VIEW EVENT LOG
[G] EXIT APPLICATION");
            while (true)
            {
                Console.WriteLine("Enter option:");
                ConsoleKeyInfo key = Console.ReadKey(false);
                switch (key.Key)
                {
                    case ConsoleKey.A:
                        await _service.StartBoilerAsync();
                        continue;
                    case ConsoleKey.B:
                        await _service.StopBoilerAsync();
                        continue;
                    case ConsoleKey.C:
                        await _service.SimulateBoilerErrorAsync();
                        continue;
                    case ConsoleKey.D:
                        await _service.ToggleRunInterlockAsync();
                        continue;
                    case ConsoleKey.E:
                        await _service.ResetLockoutAsync();
                        continue;
                    case ConsoleKey.F:
                        List<string> logs = _service.ViewEventLogAsync();
                        foreach (var log in logs)
                        {
                            Console.WriteLine(log);
                        }
                        continue;
                    case ConsoleKey.G:
                        await _service.DisposeAsync();
                        Console.WriteLine("Exiting the application");
                        break;
                }
            }
        }
    }
}
