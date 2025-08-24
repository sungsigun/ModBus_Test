using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ModBusDevExpress.Utils
{
    /// <summary>
    /// 통합 로깅 헬퍼 클래스
    /// </summary>
    public static class LoggingHelper
    {
        private static readonly UTF8Encoding Utf8Bom = new UTF8Encoding(true);

        /// <summary>
        /// 메인 시스템 로그 (log{yyyyMMdd}.txt)
        /// </summary>
        public static void LogSystem(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"{timestamp} {message}";
                var logFile = Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt");
                
                using (var writer = new StreamWriter(logFile, append: true, encoding: Utf8Bom))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch { /* 로그 실패는 무시 */ }
        }

        /// <summary>
        /// 디바이스별 로그 (log_{deviceName}_{yyyyMMdd_HH}.txt)
        /// </summary>
        public static void LogDevice(string deviceName, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd HHmmss");
                var logMessage = $"{timestamp} {message}";
                var cleanDeviceName = (!string.IsNullOrEmpty(deviceName) ? deviceName : "Unknown").Replace(" ", "_");
                var fileName = $"log_{cleanDeviceName}_{DateTime.Now:yyyyMMdd_HH}.txt";
                var logFile = Path.Combine(Application.StartupPath, fileName);
                
                using (var writer = new StreamWriter(logFile, append: true, encoding: Utf8Bom))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch { /* 로그 실패는 무시 */ }
        }

        /// <summary>
        /// ReliableModBus 서비스 로그 (reliable_modbus_{yyyyMMdd}.log)
        /// </summary>
        public static void LogReliableModBus(string deviceName, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] [{deviceName}] {message}";
                
                var baseName = $"reliable_modbus_{DateTime.Now:yyyyMMdd}";
                var logFile = baseName + ".log";
                
                // BOM 체크 및 파일 회전 로직
                try
                {
                    if (File.Exists(logFile))
                    {
                        using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            byte[] head = new byte[Math.Min(3, (int)fs.Length)];
                            fs.Read(head, 0, head.Length);
                            bool hasBom = head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
                            if (!hasBom)
                            {
                                logFile = baseName + "_utf8.log";
                            }
                        }
                    }
                }
                catch { }
                
                using (var writer = new StreamWriter(logFile, append: true, encoding: Utf8Bom))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch { /* 로그 실패는 무시 */ }
        }
    }
}




