namespace BoilerController.Service
{
    internal class Notification
    {
        public event Action<string>? Notify;
        public void InvokeNotification(string message)
        {
            Notify?.Invoke(message);
        }
    }
}
