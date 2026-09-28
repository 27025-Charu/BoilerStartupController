namespace BoilerController.Repository
{
    internal class Logger
    {
        private string _filePath;
        private static readonly string[] Header = { "TimeStamp", "Event", "Event Data" };
        public Logger(string fileName)
        {
            this._filePath = fileName;
            if (!File.Exists(fileName))
            {
                File.WriteAllText(this._filePath, string.Join(",", Header) + Environment.NewLine);
            }
        }
        public void Add(string message)
        {
            File.AppendAllText(this._filePath, message + Environment.NewLine);
        }

        public List<string> ReadAll()
        {
            var lines = File.ReadAllLines(this._filePath).Skip(1);
            return lines.ToList();
        }

        private void WriteAll()
        {
            var lines = new List<string> { string.Join(",", Header) };
            File.WriteAllLines(this._filePath, lines);
        }

    }
}
