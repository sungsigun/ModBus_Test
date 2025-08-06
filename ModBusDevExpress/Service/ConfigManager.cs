using ModBusDevExpress.Models;
using ModBusDevExpress.Utils;
using System;
using System.IO;
using System.Text.Json;

namespace ModBusDevExpress.Service
{
    public static class ConfigManager
    {
        // 🔍 설정 파일 저장 경로 (권한 문제 해결)
        private static string ConfigFilePath
        {
            get
            {
                // 1순위: 실행 파일 폴더 (쓰기 가능한 경우)
                string executablePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "dbconfig.json");
                
                // 쓰기 권한 확인
                try
                {
                    string testFile = Path.Combine(System.Windows.Forms.Application.StartupPath, "test_write.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    return executablePath; // 쓰기 가능하면 실행 파일 폴더 사용
                }
                catch
                {
                    // 2순위: Documents 폴더 (권한 안전)
                    string documentsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "ModBusApp",
                        "dbconfig.json"
                    );
                    return documentsPath;
                }
            }
        }

        public static void SaveDatabaseSettings(DatabaseSettings settings)
        {
            try
            {
                // 🔍 저장 경로 확인 및 디버깅
                string saveDirectory = Path.GetDirectoryName(ConfigFilePath);
                string fileName = Path.GetFileName(ConfigFilePath);
                
                // 디렉토리가 존재하지 않으면 생성 (권한 문제 방지)
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }
                
                // 운영에서는 팝업을 띄우지 않음 (로그만 필요 시 기록)

                var configToSave = new
                {
                    DatabaseType = settings.DatabaseType.ToString(),  // 🔧 DB 타입 저장 추가
                    Server = EncryptionHelper.Encrypt(settings.Server),      // 🔒 서버 IP 암호화
                    Port = settings.Port,  // 포트는 평문 (보안에 덜 민감)
                    Database = EncryptionHelper.Encrypt(settings.Database),  // 🔒 DB명 암호화  
                    Username = EncryptionHelper.Encrypt(settings.Username),  // 🔒 사용자명 암호화
                    Password = settings.RememberPassword ?
                              EncryptionHelper.Encrypt(settings.Password) : "",
                    RememberPassword = settings.RememberPassword
                };

                string jsonContent = JsonSerializer.Serialize(configToSave,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(ConfigFilePath, jsonContent);
                
                // 저장 성공/실패 팝업 제거
            }
            catch (Exception ex)
            {
                // 서비스 레이어에서는 팝업 대신 예외만 던짐
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

                    // 🔧 DB 타입 로드 추가
                    DatabaseType dbType = DatabaseType.SqlServer; // 🎯 기본값을 SQL Server로 변경
                    if (root.TryGetProperty("DatabaseType", out var dbTypeProperty))
                    {
                        if (Enum.TryParse<DatabaseType>(dbTypeProperty.GetString(), out var parsedType))
                        {
                            dbType = parsedType;
                        }
                    }

                    return new DatabaseSettings
                    {
                        DatabaseType = dbType,  // 🔧 DB 타입 설정
                        Server = root.TryGetProperty("Server", out var server) ?
                                 EncryptionHelper.Decrypt(server.GetString() ?? "") : (dbType == DatabaseType.SqlServer ? "175.45.202.13" : "localhost"),  // 🔒 서버 IP 복호화
                        Port = root.TryGetProperty("Port", out var port) ?
                               port.GetInt32() : (dbType == DatabaseType.PostgreSQL ? 5432 : 11433),  // 🔒 SQL Server 고정 포트
                        Database = root.TryGetProperty("Database", out var db) ?
                                   EncryptionHelper.Decrypt(db.GetString() ?? "") : "",  // 🔒 DB명 복호화
                        Username = root.TryGetProperty("Username", out var user) ?
                                   EncryptionHelper.Decrypt(user.GetString() ?? "") : "",  // 🔒 사용자명 복호화
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

        // 🔧 완전한 DB 설정이 있는지 확인 (원래 방식 복원)
        public static bool HasValidConfig()
        {
            try
            {
                var settings = LoadDatabaseSettings();

                // 🎯 모든 필수 정보 체크 (비밀번호 포함)
                return !string.IsNullOrEmpty(settings.Server) &&
                       !string.IsNullOrEmpty(settings.Database) &&
                       !string.IsNullOrEmpty(settings.Username) &&
                       (settings.RememberPassword && !string.IsNullOrEmpty(settings.Password));
            }
            catch
            {
                return false; // 설정 파일 로드 실패 시 false
            }
        }

        // 📂 현재 사용 중인 설정 파일 경로 반환
        public static string GetCurrentConfigPath()
        {
            return ConfigFilePath;
        }
    }
}