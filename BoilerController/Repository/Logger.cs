namespace BoilerController.Repository
{
    /// <summary>
    /// Logger is used for logging and capturing the significant events in a .csv file.
    /// </summary>
    internal class Logger
    {
        private string _filePath;
        private SemaphoreSlim _semaphoreLock = new SemaphoreSlim(1, 1);
        private static readonly string[] Header = { "TimeStamp", "Event", "Event Data" };
        /// <summary>
        /// If the file is not present in that directory this will help in creating and adding the header to it.
        /// </summary>
        /// <param name="fileName"></param>
        public Logger(string fileName)
        {
            this._filePath = fileName;
            if (!File.Exists(fileName) || new FileInfo(_filePath).Length == 0)
            {
                File.WriteAllText(this._filePath, string.Join(",", Header) + Environment.NewLine);
            }
        }

        /// <summary>
        /// Reading all the data from the file.
        /// </summary>
        /// <returns>Returns the retrieved data.</returns>
        public List<string> ReadAll()
        {
            var lines = File.ReadAllLines(this._filePath).Skip(1);
            return lines.ToList();
        }

        /// <summary>
        /// Writing the event data in the format of - Absolute date and time string, Event string, Event data string and storing it in the .csv file.
        /// </summary>
        /// <param name="evt">Method that triggered this event to perform</param>
        /// <param name="eventData">The method's state during the operation.</param>
        /// <returns></returns>
        public async Task WriteAsync(string evt, string eventData)
        {
            string[] lines =
            {
                DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"),evt,eventData
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