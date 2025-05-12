using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using AEMSApp.Models;

namespace AEMSApp.Services
{
    public class CurriculumProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiApiEndpoint;
        private readonly FileProcessingService _fileProcessingService;
        private readonly DatabaseService _databaseService;

        public CurriculumProcessingService(string aiApiEndpoint, FileProcessingService fileProcessingService, DatabaseService databaseService)
        {
            _httpClient = new HttpClient();
            _aiApiEndpoint = aiApiEndpoint;
            _fileProcessingService = fileProcessingService;
            _databaseService = databaseService;
        }

        public string ExtractTextFromPdf(string pdfFilePath)
        {
            return _fileProcessingService.ExtractTextFromPdf(pdfFilePath);
        }

        public async Task<List<CurriculumNode>> SendTextToAiApiAsync(string text)
        {
            try
            {
                var requestBody = new
                {
                    prompt = $@"Extract hierarchical curriculum structure from the following text and return as JSON array.
                    Each node should have: title, type (Unit/Module/Chapter/Topic/etc), level (0-based depth), description, standardCode (if any).
                    Example format:
                    [{{
                        ""title"": ""Mathematics"",
                        ""type"": ""Unit"",
                        ""level"": 0,
                        ""description"": ""Core mathematics concepts"",
                        ""standardCode"": ""MATH-001"",
                        ""children"": [{{...}}]
                    }}]
                    
                    Text to analyze:
                    {text}",
                    max_tokens = 2000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_aiApiEndpoint, content);
                response.EnsureSuccessStatusCode();
                
                var responseString = await response.Content.ReadAsStringAsync();
                var curriculumNodes = JsonSerializer.Deserialize<List<CurriculumNode>>(responseString);

                if (curriculumNodes != null)
                {
                    // Save curriculum nodes to database
                    foreach (var node in curriculumNodes)
                    {
                        await _databaseService.SaveCurriculumNodeAsync(node);
                    }
                }

                return curriculumNodes ?? new List<CurriculumNode>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error communicating with AI API: {ex.Message}", ex);
            }
        }

        public async Task<List<CurriculumNode>> LoadSavedCurriculumAsync()
        {
            return await _databaseService.LoadCurriculumAsync();
        }
    }
}
