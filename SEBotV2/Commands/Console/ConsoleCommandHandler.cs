using System.Reflection;
using HarmonyLib;
using SEBotV2.API.Helpers;

namespace SEBotV2.Commands.Console
{
    public class ConsoleCommandHandler
    {
        private bool _isListening = false;
        private Thread _inputThread;
        private CancellationTokenSource _cts;
        public List<ConsoleCommandBase> commands = [];

        public ConsoleCommandHandler()
        {
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            _inputThread = new Thread(ListenForInput)
            {
                IsBackground = true,
                Name = "ConsoleInputThread"
            };
            _inputThread.Start();
            LogManager.Info("Console command handler started. Press SPACE to enter command mode.");
            GetAllCommands();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _isListening = false;
        }

        private void GetAllCommands()
        {
            List<Type> commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(ConsoleCommandBase).IsAssignableFrom(t)).ToList();
            LogManager.Info($"Found {commands.Count} console command types to register");

            int registered = 0;
            int skipped = 0;
            foreach (Type type in commands)
            {
                try
                {
                    ConstructorInfo ctor = AccessTools.Constructor(type, Type.EmptyTypes);

                    if (ctor == null)
                    {
                        LogManager.Warn($"Skipping {type.Name}: no parameterless constructor found.");
                        skipped++;
                        continue;
                    }

                    ConsoleCommandBase instance = (ConsoleCommandBase)ctor.Invoke([]);
                    this.commands.Add(instance);
                    registered++;
                    LogManager.Debug($"Registered console command: {instance.Name}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to register console command {type.Name}: {ex.Message}");
                    skipped++;
                }
            }

            LogManager.Info($"Console command registration complete: {registered} registered, {skipped} skipped");
        }

        private void ListenForInput()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (System.Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = System.Console.ReadKey(true);
                        
                        if (key.Key == ConsoleKey.Spacebar && !_isListening)
                        {
                            _isListening = true;
                            System.Console.Write("\n> ");
                            string input = System.Console.ReadLine();
                            
                            if (!string.IsNullOrWhiteSpace(input))
                            {
                                _ = Task.Run(async () => await ProcessCommandAsync(input));
                            }
                            
                            _isListening = false;
                        }
                    }
                    
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Error in console input thread: {ex}");
                }
            }
        }

        private async Task ProcessCommandAsync(string input)
        {
            try
            {
                await ProcessConsoleCommandAsync(input);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error processing console command: {ex}");
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task ProcessConsoleCommandAsync(string command)
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLower();
            List<string> args = parts.Skip(1).ToList();

            bool commandFound = false;
            foreach (ConsoleCommandBase commandBase in commands)
            {
                if (commandBase.Name.ToLower().Equals(cmd, StringComparison.OrdinalIgnoreCase))
                {
                    commandFound = true;
                    
                    try
                    {
                        CommandResult result = await commandBase.ExecuteAsync(args);
                        
                        if (!string.IsNullOrWhiteSpace(result.Response))
                        {
                            System.Console.WriteLine(result.Response);
                        }
                        
                        if (!result.Success)
                        {
                            LogManager.Debug($"Command '{cmd}' executed with failure status");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Error executing command '{cmd}': {ex}");
                        System.Console.WriteLine($"Error executing command: {ex.Message}");
                    }
                    
                    break;
                }
            }

            if (!commandFound)
            {
                System.Console.WriteLine($"Unknown command: {cmd}");
                System.Console.WriteLine("Type 'help' for a list of available commands.");
            }
        }
    }
}