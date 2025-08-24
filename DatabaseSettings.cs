using DevExpress.Xpo.DB;
using System;

namespace ModBusDevExpress.Models
{
    public class DatabaseSettings
    {
        public string Server { get; set; } = "";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberPassword { get; set; } = true;

        public string GetConnectionString()
        {
            // 포트가 기본값(5432)이 아니면 서버:포트 형태로 조합
            string serverPort = Port == 5432 ? Server : $"{Server}:{Port}";

            return PostgreSqlConnectionProvider.GetConnectionString(
                serverPort,  // server:port
                Username,    // userId
                Password,    // password
                Database     // database
            );
        }
    }
}