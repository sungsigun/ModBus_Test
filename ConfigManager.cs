using ModBusDevExpress.Models;
using ModBusDevExpress.Utils;
using System;
using System.IO;
using System.Text.Json;

namespace ModBusDevExpress.Service
{
    public static class ConfigManager
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ModBusApp",
            "dbconfig.json"
        );

        public static void SaveDatabaseSettings(DatabaseSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));

                var configToSave = new
                {
                    Server = settings.Server,
                    Port = settings.Port,
                    Database = settings.Database,
                    Username = settings.Username,
                    Password = settings.RememberPassword ?
                              EncryptionHelper.Encrypt(settings.Password) : "",
                    RememberPassword = settings.RememberPassword
                };

                string jsonContent = JsonSerializer.Serialize(configToSave,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(ConfigFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"설정 저장 실패: {ex.Message}", ex);
            }
        }

        public static DatabaseSettings LoadDatabaseSettings()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return new DatabaseSettings();

                string jsonContent = File.ReadAllText(ConfigFilePath);
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    var root = doc.RootElement;

                    return new DatabaseSettings
                    {
                        Server = root.TryGetProperty("Server", out var server) ?
                                 server.GetString() ?? "" : "",
                        Port = root.TryGetProperty("Port", out var port) ?
                               port.GetInt32() : 5432,
                        Database = root.TryGetProperty("Database", out var db) ?
                                   db.GetString() ?? "" : "",
                        Username = root.TryGetProperty("Username", out var user) ?
                                   user.GetString() ?? "" : "",
                        Password = root.TryGetProperty("Password", out var pwd) ?
                                   EncryptionHelper.Decrypt(pwd.GetString() ?? "") : "",
                        RememberPassword = root.TryGetProperty("RememberPassword", out var remember) ?
                                          remember.GetBoolean() : true
                    };
                }
            }
            catch
            {
                return new DatabaseSettings();
            }
        }

        // 🎯 핵심 수정: 비밀번호 체크 제거
        public static bool HasValidConfig()
        {
            var settings = LoadDatabaseSettings();

            // 기본 정보만 체크 (비밀번호는 체크하지 않음)
            return !string.IsNullOrEmpty(settings.Server) &&
                   !string.IsNullOrEmpty(settings.Database) &&
                   !string.IsNullOrEmpty(settings.Username);
        }
    }
}