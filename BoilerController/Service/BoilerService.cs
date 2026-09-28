using BoilerController.Repository;

namespace BoilerController.Service
{
    internal class BoilerService
    {
        private Logger logger;

        public BoilerService(Logger logger)
        {
            this.logger = logger;
        }

        internal void DisposeAsync()
        {
            throw new NotImplementedException();
        }

        internal void ResetLockoutAsync()
        {

        }

        internal void RunAsync()
        {
            throw new NotImplementedException();
        }

        internal void SimulateBoilerErrorAsync()
        {

        }

        internal void StartBoilerAsync()
        {

        }

        internal void StopBoilerAsync()
        {
            throw new NotImplementedException();
        }

        internal void ToggleRunInterlock()
        {
            throw new NotImplementedException();
        }

        internal void ToggleRunInterlockAsync()
        {
            throw new NotImplementedException();
        }

        internal List<string> ViewEventLogAsync()
        {
            return logger.ReadAll();
        }
    }
}
