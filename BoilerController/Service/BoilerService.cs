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

        internal void ResetLockout()
        {

        }

        internal void SimulateBoilerError()
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

        internal List<string> ViewEventLog()
        {
            return logger.ReadAll();
        }
    }
}
