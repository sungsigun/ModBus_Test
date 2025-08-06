using DevExpress.Xpo.DB;
using DevExpress.Xpo;
using ModBusDevExpress.Models;
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
                    // ❌ 예외 발생 대신 경고 후 계속 진행
                    throw new InvalidOperationException("비밀번호가 필요합니다.");
                }

                // 🔧 DatabaseConfigForm과 정확히 동일한 연결 문자열 사용
                string connectionString;
                if (settings.DatabaseType == DatabaseType.SqlServer)
                {
                    // ✅ DatabaseConfigForm과 동일한 방식: 간단한 연결 문자열
                    connectionString = $"Server={settings.Server},{settings.Port};Database={settings.Database};User Id={settings.Username};Password={passwordToUse};Connection Timeout=30;";
                    // XPO용으로 변환
                    connectionString = MSSqlConnectionProvider.GetConnectionString(
                        $"{settings.Server},{settings.Port}", 
                        settings.Username, 
                        passwordToUse, 
                        settings.Database);
                }
                else
                {
                    // PostgreSQL: 기존 방식 사용
                    connectionString = settings.GetConnectionString().Replace(settings.Password, passwordToUse);
                }

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

        // 🎯 SessionService 인스턴스 초기화 (DB 설정 변경 시 사용)
        public static void ResetInstance()
        {
            try
            {
                if (instance?.UOW != null)
                {
                    instance.UOW.Dispose();
                }
            }
            catch (Exception ex)
            {
                // 로그 기록만 하고 계속 진행
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} SessionService 리셋 중 기존 UOW 해제 오류: {ex.Message}\r\n"
                );
            }
            finally
            {
                instance = null; // 인스턴스 초기화
            }
        }

        public void InsertOrUpdate()
        {
            UOW.CommitChanges();
        }
    }
}