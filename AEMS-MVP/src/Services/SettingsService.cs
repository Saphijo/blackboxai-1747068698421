using System;
using System.Threading.Tasks;

namespace AEMSApp.Services
{
    public class SettingsService
    {
        private readonly DatabaseService _databaseService;
        private const string AI_ENDPOINT_KEY = "AIEndpoint";

        public SettingsService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<string> GetAiEndpointAsync()
        {
            var endpoint = await _databaseService.GetSettingAsync(AI_ENDPOINT_KEY);
            return endpoint ?? "http://localhost:11434/api"; // Default Ollama endpoint
        }

        public async Task SaveAiEndpointAsync(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("AI endpoint cannot be empty");
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid URL format for AI endpoint");
            }

            await _databaseService.SaveSettingAsync(AI_ENDPOINT_KEY, endpoint);
        }

        public async Task InitializeDefaultSettingsAsync()
        {
            var endpoint = await _databaseService.GetSettingAsync(AI_ENDPOINT_KEY);
            if (endpoint == null)
            {
                await SaveAiEndpointAsync("http://localhost:11434/api");
            }
        }
    }
}
