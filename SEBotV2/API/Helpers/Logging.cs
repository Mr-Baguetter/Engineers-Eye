namespace SEBotV2.API.Helpers
{
    public class Logging
    {
        private static readonly string LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "PlayerLogs");

        public static void LogToFile(string line)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            string filePath = Path.Combine(LogDirectory, $"Log{DateTime.Now:yyyy-MM-dd}.txt");
            string logLine = $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}";

            File.AppendAllText(filePath, logLine);
        }
    }
}
