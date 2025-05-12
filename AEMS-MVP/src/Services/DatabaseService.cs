using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using AEMSApp.Models;

namespace AEMSApp.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aems.db");
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                // Create curriculum nodes table
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CurriculumNodes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Type TEXT NOT NULL,
                        ParentId INTEGER,
                        Level INTEGER NOT NULL,
                        Description TEXT,
                        StandardCode TEXT,
                        FOREIGN KEY(ParentId) REFERENCES CurriculumNodes(Id)
                    )";
                command.ExecuteNonQuery();

                // Create materials table
                command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Materials (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FilePath TEXT NOT NULL UNIQUE,
                        FileType TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Topics TEXT,
                        ProcessedDate TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();

                // Create material-curriculum links table
                command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MaterialCurriculumLinks (
                        MaterialId INTEGER,
                        CurriculumNodeId INTEGER,
                        PRIMARY KEY (MaterialId, CurriculumNodeId),
                        FOREIGN KEY(MaterialId) REFERENCES Materials(Id),
                        FOREIGN KEY(CurriculumNodeId) REFERENCES CurriculumNodes(Id)
                    )";
                command.ExecuteNonQuery();

                // Create settings table
                command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
            }
        }

        public async Task SaveCurriculumNodeAsync(CurriculumNode node)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO CurriculumNodes (Title, Type, ParentId, Level, Description, StandardCode)
                VALUES (@title, @type, @parentId, @level, @description, @standardCode)
                RETURNING Id";

            command.Parameters.AddWithValue("@title", node.Title);
            command.Parameters.AddWithValue("@type", node.Type);
            command.Parameters.AddWithValue("@parentId", node.ParentId.HasValue ? node.ParentId : DBNull.Value);
            command.Parameters.AddWithValue("@level", node.Level);
            command.Parameters.AddWithValue("@description", node.Description);
            command.Parameters.AddWithValue("@standardCode", node.StandardCode);

            node.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

            foreach (var child in node.Children)
            {
                child.ParentId = node.Id;
                await SaveCurriculumNodeAsync(child);
            }
        }

        public async Task SaveMaterialAsync(MaterialFileMetadata material)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Materials (FilePath, FileType, Status, Topics, ProcessedDate)
                VALUES (@filePath, @fileType, @status, @topics, @processedDate)";

            command.Parameters.AddWithValue("@filePath", material.FilePath);
            command.Parameters.AddWithValue("@fileType", material.FileType);
            command.Parameters.AddWithValue("@status", material.Status);
            command.Parameters.AddWithValue("@topics", material.Topics);
            command.Parameters.AddWithValue("@processedDate", DateTime.UtcNow.ToString("o"));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<CurriculumNode>> LoadCurriculumAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var nodes = new Dictionary<int, CurriculumNode>();
            var rootNodes = new List<CurriculumNode>();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM CurriculumNodes ORDER BY Level, Id";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var node = new CurriculumNode
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Type = reader.GetString(reader.GetOrdinal("Type")),
                    Level = reader.GetInt32(reader.GetOrdinal("Level")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    StandardCode = reader.IsDBNull(reader.GetOrdinal("StandardCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("StandardCode"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("ParentId")))
                {
                    node.ParentId = reader.GetInt32(reader.GetOrdinal("ParentId"));
                    if (nodes.ContainsKey(node.ParentId.Value))
                    {
                        nodes[node.ParentId.Value].Children.Add(node);
                    }
                }
                else
                {
                    rootNodes.Add(node);
                }

                nodes[node.Id] = node;
            }

            return rootNodes;
        }

        public async Task<List<MaterialFileMetadata>> LoadMaterialsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var materials = new List<MaterialFileMetadata>();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Materials ORDER BY ProcessedDate DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var material = new MaterialFileMetadata
                {
                    FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                    FileType = reader.GetString(reader.GetOrdinal("FileType")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Topics = reader.IsDBNull(reader.GetOrdinal("Topics")) ? string.Empty : reader.GetString(reader.GetOrdinal("Topics"))
                };

                materials.Add(material);
            }

            return materials;
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Settings (Key, Value)
                VALUES (@key, @value)";

            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
            command.Parameters.AddWithValue("@key", key);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
    }
}
