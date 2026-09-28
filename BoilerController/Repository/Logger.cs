namespace BoilerController.Repository
{
    internal class Logger
    {
        private string _filePath;
        private SemaphoreSlim _semaphoreLock = new SemaphoreSlim(1, 1);
        private static readonly string[] Header = { "TimeStamp", "Event", "Event Data" };
        public Logger(string fileName)
        {
            this._filePath = fileName;
            if (!File.Exists(fileName))
            {
                File.WriteAllText(this._filePath, string.Join(",", Header) + Environment.NewLine);
            }
        }

        public List<string> ReadAll()
        {
            var lines = File.ReadAllLines(this._filePath).Skip(1);
            return lines.ToList();
        }

        public async Task WriteAsync(string evt, string eventData)
        {
            string[] lines =
            {
                DateTime.UtcNow.ToString("dd-mm-yyyy HH:mm:ss"),evt,eventData
            };
            string line = string.Join(",", lines.Select(line => line));

            await _semaphoreLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(_filePath, line + Environment.NewLine);
            }
            finally
            {
                _semaphoreLock.Release();
            }
        }
    }
}
