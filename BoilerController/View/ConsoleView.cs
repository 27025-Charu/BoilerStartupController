using BoilerController.Service;

namespace BoilerController.View
{
    internal class ConsoleView
    {
        private const int MAXCOUNT = 10;
        private BoilerService _service;
        private readonly Queue<string> _queue = new();
        private readonly object _eventsLock = new();
        private volatile bool _confirmingToggle;
        private volatile string _message = string.Empty;
        public ConsoleView(BoilerService service)
        {
            this._service = service;
            service.Notify += OnEvent;
        }

        private void OnEvent(string line)
        {
            lock (_eventsLock)
            {
                _queue.Enqueue(line);
                while (_queue.Count > MAXCOUNT)
                {
                    _queue.Dequeue();
                }
            }
        }

        internal async Task ExecuteAsync()
        {
            Console.Clear();
            Console.CursorVisible = false;
            using var cts = new CancellationTokenSource();
            Task consoleRender = RenderingConsole(cts.Token);
            try
            {
                await UserInputAsync();
            }
            finally
            {
                cts.Cancel();
                await consoleRender;
                Console.CursorVisible = true;
                Console.SetCursorPosition(0, Console.CursorTop + 1);
            }
        }

        private async Task RenderingConsole(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Render();
                    await Task.Delay(200, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Render()
        {
            int width = Math.Max(20, Console.WindowWidth - 1);
            Console.SetCursorPosition(0, 0);
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
            WriteLine("-------------------------------------------", width);
            WriteLine($" > {_message}", width);
            WriteLine("-------------------------------------------", width);
            Console.WriteLine("Events:");
            string[] events;
            lock (_eventsLock) { events = _queue.ToArray(); }
            for (int i = 0; i < MAXCOUNT; i++)
            {
                WriteLine(i < events.Length ? " " + events[i] : "", width);
            }
        }

        private void WriteLine(string text, int width, ConsoleColor? color = null)
        {
            string padded = text.Length > width ? text[..width] : text.PadRight(width);
            Console.WriteLine(padded);
        }

        private async Task UserInputAsync()
        {
            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    await Task.Delay(50);
                    continue;
                }

                ConsoleKey key = Console.ReadKey(intercept: true).Key;
                if (_confirmingToggle)
                {
                    _confirmingToggle = false;
                    if (key == ConsoleKey.Y)
                    {
                        await _service.ToggleRunInterlockAsync();
                    }
                    else
                    {
                        _message = "Toggle cancelled. Boiler keeps running.";
                    }
                    continue;
                }
                switch (key)
                {
                    case ConsoleKey.A:
                        _message = await _service.StartBoilerAsync();
                        break;
                    case ConsoleKey.B:
                        _message = await _service.StopBoilerAsync();
                        break;
                    case ConsoleKey.C:
                        _message = await _service.SimulateBoilerErrorAsync();
                        break;
                    case ConsoleKey.D:
                        //if (_service.IsRunning())
                        //{

                        //}
                        //else
                        //{
                        //    _message = await _service.ToggleRunInterlockAsync();
                        //}
                        break;
                    case ConsoleKey.E:
                        _message = await _service.ResetLockoutAsync();
                        break;
                    case ConsoleKey.F:
                        List<string> logs = _service.ViewEventLogAsync();
                        foreach (var log in logs)
                        {
                            Console.WriteLine(log);
                        }
                        break;
                    case ConsoleKey.G:
                        await _service.DisposeAsync();
                        Console.WriteLine("Exiting the application");
                        return;
                }
            }
        }
    }
}
