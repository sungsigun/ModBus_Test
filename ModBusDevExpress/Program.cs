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

                // 🔧 DB 설정이 있는지 확인
                if (!ConfigManager.HasValidConfig())
                {
                    // 처음 실행 또는 설정 없음 - DB 설정 요구
                    using (var dbConfigForm = new Forms.DatabaseConfigForm())
                    {
                        if (dbConfigForm.ShowDialog() != DialogResult.OK)
                        {
                            // 사용자가 취소하면 프로그램 종료
                            return;
                        }
                    }
                }

                // 🚀 DB 연결 테스트 및 프로그램 시작
                try
                {
                    var sessionService = SessionService.Instance; // DB 연결 테스트
                    Application.Run(new MainForm()); // 메인 프로그램 시작
                }
                catch (Exception dbEx)
                {
                    // DB 연결 실패 시 설정 다시 요구
                    MessageBox.Show($"데이터베이스 연결에 실패했습니다.\n\n{dbEx.Message}\n\n설정을 다시 확인해주세요.",
                        "DB 연결 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    using (var dbConfigForm = new Forms.DatabaseConfigForm())
                    {
                        if (dbConfigForm.ShowDialog() == DialogResult.OK)
                        {
                            // 🔧 설정 후 다시 시도
                            try
                            {
                                SessionService.ResetInstance();
                                var newSessionService = SessionService.Instance;
                                Application.Run(new MainForm());
                            }
                            catch (Exception retryEx)
                            {
                                MessageBox.Show($"설정 저장 후에도 연결에 실패했습니다.\n\n상세 오류:\n{retryEx.Message}\n\n{retryEx.InnerException?.Message}", 
                                    "연결 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 시작 실패:\n{ex.Message}", "오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}