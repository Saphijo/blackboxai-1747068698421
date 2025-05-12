using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AEMSApp.Services
{
    public class MaterialFileMetadata
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public string FileType { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Topics { get; set; } = string.Empty;
        public List<string> LinkedCurriculumNodes { get; set; } = new List<string>();
    }

    public class MaterialIngestionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiApiEndpoint;
        private readonly FileProcessingService _fileProcessingService;
        private readonly DatabaseService _databaseService;
        private readonly string[] _supportedExtensions = new[] { 
            ".docx", ".pptx", ".xlsx", ".pdf", 
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", 
            ".mp4", ".avi", ".mov", ".wmv" 
        };

        public MaterialIngestionService(string aiApiEndpoint, FileProcessingService fileProcessingService, DatabaseService databaseService)
        {
            _httpClient = new HttpClient();
            _aiApiEndpoint = aiApiEndpoint;
            _fileProcessingService = fileProcessingService;
            _databaseService = databaseService;
        }

        public IEnumerable<string> GetSupportedFilesFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            return Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        }

        public async Task<MaterialFileMetadata> ProcessFileAsync(string filePath)
        {
            var metadata = new MaterialFileMetadata
            {
                FilePath = filePath,
                FileType = Path.GetExtension(filePath).ToLowerInvariant(),
                Status = "Processing"
            };

            try
            {
                string extractedText = string.Empty;

                switch (metadata.FileType)
                {
                    case ".pdf":
                        extractedText = _fileProcessingService.ExtractTextFromPdf(filePath);
                        break;
                    case ".docx":
                        extractedText = _fileProcessingService.ExtractTextFromDocx(filePath);
                        break;
                    case ".pptx":
                        extractedText = _fileProcessingService.ExtractTextFromPptx(filePath);
                        break;
                    case ".xlsx":
                        extractedText = _fileProcessingService.ExtractTextFromXlsx(filePath);
                        break;
                    default:
                        if (IsImageFile(metadata.FileType) || IsVideoFile(metadata.FileType))
                        {
                            extractedText = _fileProcessingService.GetMediaFileContext(filePath);
                        }
                        else
                        {
                            metadata.Status = "Unsupported file type";
                            return metadata;
                        }
                        break;
                }

                // Send to AI API for topic identification
                var topics = await SendToAiForTopicsAsync(extractedText);
                metadata.Topics = topics;
                metadata.Status = "Success";

                // Save to database
                await _databaseService.SaveMaterialAsync(metadata);
            }
            catch (Exception ex)
            {
                metadata.Status = $"Failed: {ex.Message}";
            }

            return metadata;
        }

        private bool IsImageFile(string extension)
        {
            return new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }.Contains(extension);
        }

        private bool IsVideoFile(string extension)
        {
            return new[] { ".mp4", ".avi", ".mov", ".wmv" }.Contains(extension);
        }

        private async Task<string> SendToAiForTopicsAsync(string text)
        {
            var requestBody = new
            {
                prompt = $@"Identify main educational topics and concepts from the following text.
                Return as a comma-separated list of topics.
                Text to analyze: {text}",
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_aiApiEndpoint, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<MaterialFileMetadata>> LoadSavedMaterialsAsync()
        {
            return await _databaseService.LoadMaterialsAsync();
        }
    }
}
