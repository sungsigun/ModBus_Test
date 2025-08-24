using System;
using System.IO;
using System.Windows.Forms;

namespace ModBusDevExpress.Utils
{
    /// <summary>
    /// 파일 권한 확인 유틸리티
    /// </summary>
    public static class FilePermissionHelper
    {
        /// <summary>
        /// 지정된 디렉토리에 쓰기 권한이 있는지 확인
        /// </summary>
        /// <param name="directoryPath">확인할 디렉토리 경로</param>
        /// <returns>쓰기 권한 여부</returns>
        public static bool HasWritePermission(string directoryPath)
        {
            try
            {
                // 임시 파일명을 고유하게 생성 (GUID 사용)
                string tempFileName = $"temp_write_test_{Guid.NewGuid():N}.tmp";
                string tempFilePath = Path.Combine(directoryPath, tempFileName);
                
                // 임시 파일 생성 시도
                File.WriteAllText(tempFilePath, "permission test");
                
                // 즉시 삭제
                File.Delete(tempFilePath);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 설정 파일용 안전한 경로 가져오기
        /// </summary>
        /// <param name="fileName">설정 파일명 (예: "dbconfig.json")</param>
        /// <returns>쓰기 가능한 설정 파일 경로</returns>
        public static string GetSafeConfigPath(string fileName)
        {
            // 1순위: 실행 파일 폴더
            string executablePath = Path.Combine(Application.StartupPath, fileName);
            if (HasWritePermission(Application.StartupPath))
            {
                return executablePath;
            }

            // 2순위: Documents/ModBusApp 폴더
            string documentsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ModBusApp"
            );
            
            // 디렉토리가 없으면 생성
            try
            {
                if (!Directory.Exists(documentsDir))
                {
                    Directory.CreateDirectory(documentsDir);
                }
            }
            catch
            {
                // 생성 실패 시 임시 폴더 사용
                documentsDir = Path.GetTempPath();
            }

            return Path.Combine(documentsDir, fileName);
        }
    }
}




