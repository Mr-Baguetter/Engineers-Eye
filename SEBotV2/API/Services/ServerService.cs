using System.Text.Json;

namespace SEBotV2.API.Services
{
    public class ServerService
    {
        private readonly HttpClient _httpClient;
        private readonly Config _config;

        public ServerService(HttpClient httpClient, Config config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<ServerInfo> GetServerInfoAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_config.SteamApiKey))
                {
                    var steamResult = await QuerySteamWebApiAsync();
                    if (steamResult.IsOnline)
                    {
                        return steamResult;
                    }
                }

                return await QueryServerDirectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching server info: {ex.Message}");
                return new ServerInfo
                {
                    Name = "Space Engineers Server",
                    Players = 0,
                    MaxPlayers = 0,
                    IsOnline = false
                };
            }
        }

        private async Task<ServerInfo> QuerySteamWebApiAsync()
        {
            try
            {
                var url = $"https://api.steampowered.com/IGameServersService/GetServerList/v1/" +
                         $"?key={_config.SteamApiKey}&filter=addr\\{_config.ServerIp}:{_config.ServerPort}&limit=1";

                var response = await _httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);

                if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement) &&
                    responseElement.TryGetProperty("servers", out var serversElement) &&
                    serversElement.GetArrayLength() > 0)
                {
                    var server = serversElement[0];

                    return new ServerInfo
                    {
                        Name = server.GetProperty("name").GetString() ?? "Space Engineers Server",
                        Players = server.GetProperty("players").GetInt32(),
                        MaxPlayers = server.GetProperty("max_players").GetInt32(),
                        IsOnline = true
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Steam Web API query failed: {ex.Message}");
            }

            return new ServerInfo { IsOnline = false };
        }

        private async Task<ServerInfo> QueryServerDirectAsync()
        {
            try
            {
                var url = $"http://{_config.ServerIp}:{_config.ServerPort}/vrageremote/v1/server";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetStringAsync(url, cts.Token);
                var jsonDoc = JsonDocument.Parse(response);

                if (jsonDoc.RootElement.TryGetProperty("Data", out var dataElement))
                {
                    var serverName = "Space Engineers Server";
                    var players = 0;
                    var maxPlayers = 0;

                    if (dataElement.TryGetProperty("ServerName", out var nameElement))
                        serverName = nameElement.GetString() ?? serverName;

                    if (dataElement.TryGetProperty("Players", out var playersElement))
                        players = playersElement.GetInt32();

                    if (dataElement.TryGetProperty("MaxPlayers", out var maxPlayersElement))
                        maxPlayers = maxPlayersElement.GetInt32();

                    return new ServerInfo
                    {
                        Name = serverName,
                        Players = players,
                        MaxPlayers = maxPlayers,
                        IsOnline = true
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Direct server query failed: {ex.Message}");
            }

            return new ServerInfo
            {
                Name = "Space Engineers Server",
                Players = 0,
                MaxPlayers = 0,
                IsOnline = false
            };
        }
    }
}
