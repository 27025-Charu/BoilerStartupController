using BoilerController.Repository;
using BoilerController.Service;
using BoilerController.View;

namespace BoilerController
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            string fileName = " BoilerLog.csv";
            Logger logger = new Logger(fileName);
            BoilerService service = new BoilerService(logger);
            ConsoleView view = new ConsoleView(service);
            Console.WriteLine(@"====================================
WELCOME TO BOILER STARTUP CONTROLLER
====================================");
            await service.RunAsync();
            await view.ExecuteAsync();
        }
    }
}
