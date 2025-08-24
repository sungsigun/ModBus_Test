using DevExpress.LookAndFeel;
using ModBusDevExpress.Service;
using System;
using System.Windows.Forms;

namespace ModBusDevExpress
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // 🎯 프로그램 시작 시 임시 비밀번호 초기화
                SessionService.ClearTemporaryPassword();

                // DB 설정 확인 및 설정 폼 표시
                bool needConfig = !ConfigManager.HasValidConfig();
                bool configSuccess = false;

                while (!configSuccess)
                {
                    if (needConfig)
                    {
                        using (var configForm = new DatabaseConfigForm())
                        {
                            if (configForm.ShowDialog() != DialogResult.OK)
                            {
                                MessageBox.Show("데이터베이스 설정이 필요합니다.", "설정 필요");
                                return;
                            }
                        }
                    }

                    // DB 연결 테스트
                    try
                    {
                        var sessionService = SessionService.Instance;
                        configSuccess = true; // 연결 성공
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = GetSimpleErrorMessage(ex);

                        var result = MessageBox.Show(
                            $"{errorMessage}\n\n설정을 다시 입력하시겠습니까?",
                            "데이터베이스 연결 오류",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Error);

                        if (result == DialogResult.Yes)
                        {
                            needConfig = true; // 설정 폼 다시 표시
                        }
                        else
                        {
                            return; // 프로그램 종료
                        }
                    }
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 시작 실패:\n{ex.Message}", "오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetSimpleErrorMessage(Exception ex)
        {
            string msg = ex.Message.ToLower();

            if (msg.Contains("connection") && msg.Contains("refused"))
                return "🔌 서버에 연결할 수 없습니다.\n서버 주소와 포트를 확인하세요.";

            if (msg.Contains("authentication") || msg.Contains("password"))
                return "🔐 인증에 실패했습니다.\n사용자명과 비밀번호를 확인하세요.";

            if (msg.Contains("database") && msg.Contains("not exist"))
                return "🗃️ 데이터베이스를 찾을 수 없습니다.\n데이터베이스 이름을 확인하세요.";

            if (msg.Contains("timeout"))
                return "⏰ 연결 시간이 초과되었습니다.\n네트워크 상태를 확인하세요.";

            if (msg.Contains("비밀번호가 필요합니다"))
                return "🔐 비밀번호가 저장되지 않았습니다.\n설정에서 비밀번호를 다시 입력하세요.";

            return $"❌ 데이터베이스 연결 중 오류가 발생했습니다.\n\n상세 오류: {ex.Message}";
        }
    }
}