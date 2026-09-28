namespace BoilerController.Service
{
    internal class Notification
    {
        public Action<string> Notify;
        void InvokeNotification(string message)
        {
            Notify?.Invoke(message);
        }
    }
}
