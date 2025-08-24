using DevExpress.Xpo.DB;
using DevExpress.Xpo;
using ModBusDevExpress.Service;
using System;

namespace ModBusDevExpress.Service
{
    public class SessionService
    {
        private static SessionService instance;
        public static SessionService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SessionService();
                }
                return instance;
            }
        }

        // 🎯 현재 세션에서만 사용할 임시 비밀번호 저장
        private static string _temporaryPassword = "";

        public readonly UnitOfWork UOW;

        private SessionService()
        {
            try
            {
                var settings = ConfigManager.LoadDatabaseSettings();
                if (string.IsNullOrEmpty(settings.Server) ||
                    string.IsNullOrEmpty(settings.Database) ||
                    string.IsNullOrEmpty(settings.Username))
                {
                    throw new InvalidOperationException("데이터베이스 설정이 필요합니다.");
                }

                string passwordToUse = "";

                // 🎯 비밀번호 우선순위: 1) 임시 비밀번호 2) 저장된 비밀번호
                if (!string.IsNullOrEmpty(_temporaryPassword))
                {
                    passwordToUse = _temporaryPassword;
                }
                else if (settings.RememberPassword && !string.IsNullOrEmpty(settings.Password))
                {
                    passwordToUse = settings.Password;
                }
                else
                {
                    throw new InvalidOperationException("비밀번호가 필요합니다.");
                }

                string serverPort = settings.Port == 5432 ?
                    settings.Server :
                    $"{settings.Server}:{settings.Port}";

                string connectionString = PostgreSqlConnectionProvider.GetConnectionString(
                    serverPort, settings.Username, passwordToUse, settings.Database);

                XpoDefault.DataLayer = XpoDefault.GetDataLayer(connectionString, AutoCreateOption.None);
                UOW = new UnitOfWork();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"데이터베이스 초기화 실패: {ex.Message}", ex);
            }
        }

        // 🎯 임시 비밀번호 설정 (현재 세션에서만 유효)
        public static void SetTemporaryPassword(string password)
        {
            _temporaryPassword = password;
        }

        // 🎯 임시 비밀번호 제거
        public static void ClearTemporaryPassword()
        {
            _temporaryPassword = "";
        }

        public void InsertOrUpdate()
        {
            UOW.CommitChanges();
        }
    }
}