using Discord;

namespace SEBotV2.API.Helpers
{
    public class LogManager
    {
        public static List<(string message, LogSeverity severity, DateTime timestamp)> Logs = [];
        private static readonly string LogDirectory = "logs";
        private static bool Debugbool;

        public static async Task Init() =>
            Debugbool = await ConfigManager.LoadBoolAsync("Debug");

        public static void Info(string message)
        {
            Logs.Add((message, LogSeverity.Info, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [INFO] {message}");
            Console.ResetColor();
        }

        public static void Warn(string message)
        {
            Logs.Add((message, LogSeverity.Warning, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [WARN] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Logs.Add((message, LogSeverity.Error, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [ERROR] {message}");
            Console.ResetColor();
        }

        public static void Debug(string message)
        {
            Logs.Add((message, LogSeverity.Debug, DateTime.Now));
            if (Debugbool)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [DEBUG] {message}");
                Console.ResetColor();
            }
        }

        public static void Critical(string message)
        {
            Logs.Add((message, LogSeverity.Critical, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [CRITICAL] {message}");
            Console.ResetColor();
        }

        public static void SaveLogs()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                string filename = $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                string filepath = Path.Combine(LogDirectory, filename);

                using StreamWriter writer = new(filepath);
                writer.WriteLine($"=== Log Session - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                writer.WriteLine();

                foreach ((string message, LogSeverity severity, DateTime timestamp) in Logs)
                    writer.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{severity}] {message}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Logs saved to {filepath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to save logs: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}