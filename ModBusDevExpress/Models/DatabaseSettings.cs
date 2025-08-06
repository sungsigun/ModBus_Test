using DevExpress.Xpo.DB;
using System;

namespace ModBusDevExpress.Models
{
    public enum DatabaseType
    {
        PostgreSQL,
        SqlServer
    }

    public class DatabaseSettings
    {
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;  // 🔧 기본값을 SQL Server로 변경
        public string Server { get; set; } = "175.45.202.13";  // 🔒 고정 SQL Server 주소
        public int Port { get; set; } = 11433;  // 🔒 고정 SQL Server 포트
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberPassword { get; set; } = true;

        public string GetConnectionString()
        {
            switch (DatabaseType)
            {
                case DatabaseType.PostgreSQL:
                    // PostgreSQL 연결 문자열 생성
                    string serverPort = Port == 5432 ? Server : $"{Server}:{Port}";
                    return PostgreSqlConnectionProvider.GetConnectionString(
                        serverPort,  // server:port
                        Username,    // userId
                        Password,    // password
                        Database     // database
                    );

                case DatabaseType.SqlServer:
                    // 🔧 MS SQL Server 연결 문자열 생성 (Microsoft.Data.SqlClient 호환)
                    string serverName = Port == 1433 || Port == 11433 ? Server : $"{Server},{Port}";
                    return $"XpoProvider=MSSqlServer;Server={serverName};Database={Database};User Id={Username};Password={Password};TrustServerCertificate=true;Encrypt=false;";

                default:
                    throw new NotSupportedException($"지원하지 않는 데이터베이스 타입: {DatabaseType}");
            }
        }

        public int GetDefaultPort()
        {
            return DatabaseType == DatabaseType.PostgreSQL ? 5432 : 11433;  // 🎯 SQL Server 기본 포트를 11433으로
        }
    }
}