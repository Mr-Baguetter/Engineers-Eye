using Okolni.Source.Query;
using Okolni.Source.Query.Responses;

namespace SEBotV2.API.Helpers
{
    public class ServerManager
    {
        public static event Action<ServerInfo>? OnSentQuery;

        public static Dictionary<ulong, ServerInfo> ServerInfoByGuildId { get; } = [];
        private static readonly Dictionary<string, Task<ServerInfo?>> _pendingQueries = [];
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public class ServerInfo
        {
            public string Name { get; set; } = string.Empty;
            public int PlayerCount { get; set; }
            public int MaxPlayers { get; set; }
            public List<Player> Players { get; set; } = [];
            public string Ip { get; set; }
            public int Port { get; set; }
            public PlayerResponse PlayerResponse { get; set; }
            public InfoResponse InfoResponse { get; set; }
        }

        public static async Task<ServerInfo?> QueryServer(ulong guildId)
        {
            try
            {
                Dictionary<ulong, Bot.ServerPingInfo> servers = await ConfigManager.LoadAsync<Dictionary<ulong, Bot.ServerPingInfo>>("PingInfo") ?? [];
                string host = servers[guildId].IP;
                int port = servers[guildId].Port;
                
                string serverKey = $"{host}:{port}";
                Task<ServerInfo?>? queryTask = null;

                await _semaphore.WaitAsync();
                try
                {
                    if (_pendingQueries.TryGetValue(serverKey, out Task<ServerInfo?>? existingTask))
                    {
                        queryTask = existingTask;
                        LogManager.Debug($"Reusing existing query for {serverKey}");
                    }
                    else
                    {
                        queryTask = PerformQuery(host, port, serverKey);
                        _pendingQueries[serverKey] = queryTask;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                ServerInfo? info = await queryTask;
                OnSentQuery?.Invoke(info);

                await _semaphore.WaitAsync();
                try
                {
                    _pendingQueries.Remove(serverKey);
                }
                finally
                {
                    _semaphore.Release();
                }

                if (info != null)
                {
                    ServerInfoByGuildId[guildId] = info;
                }
                return info;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error querying server: {ex.Message}");
                return null;
            }
        }

        public static async Task<ServerInfo?> PerformQuery(string host, int port, string serverKey)
        {
            return await Task.Run(() =>
            {
                try
                {
                    QueryConnection conn = new()
                    {
                        Host = host,
                        Port = port
                    };

                    conn.Connect();

                    InfoResponse infoResp = conn.GetInfo();
                    PlayerResponse playersResp = conn.GetPlayers();

                    ServerInfo info = new()
                    {
                        Name = infoResp?.Name ?? string.Empty,
                        PlayerCount = infoResp?.Players ?? 0,
                        MaxPlayers = infoResp?.MaxPlayers ?? 0,
                        Players = playersResp.Players ?? [],
                        Ip = host,
                        Port = port,
                        PlayerResponse = playersResp ?? new(),
                        InfoResponse = infoResp ?? new(),
                    };

                    LogManager.Info($"Successfully queried {serverKey}: {info.Name} ({info.PlayerCount}/{info.MaxPlayers})");
                    return info;
                }
                catch (Exception innerEx)
                {
                    LogManager.Warn($"Query failed for {host}:{port} - {innerEx.Message}");
                    return null;
                }
            });
        }
    }
}