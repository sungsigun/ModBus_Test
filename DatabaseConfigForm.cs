// ================================
// DatabaseConfigForm.cs 전체 파일 교체
// ================================
using System;
using System.Windows.Forms;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using DevExpress.Xpo.DB;
using DevExpress.Xpo;

namespace ModBusDevExpress
{
    public partial class DatabaseConfigForm : Form
    {
        public DatabaseSettings DatabaseSettings { get; private set; }

        public DatabaseConfigForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var settings = ConfigManager.LoadDatabaseSettings();
                txtServer.Text = settings.Server;
                nudPort.Value = settings.Port;
                txtDatabase.Text = settings.Database;
                txtUsername.Text = settings.Username;
                txtPassword.Text = settings.Password;
                chkRememberPassword.Checked = settings.RememberPassword;

                // 기본값 설정 (최초 실행 시)
                if (string.IsNullOrEmpty(txtServer.Text))
                {
                    txtServer.Text = "211.233.58.176";
                    txtDatabase.Text = "XAFQTPML";
                    txtUsername.Text = "maintenance";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 로드 실패: {ex.Message}", "오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            btnTest.Enabled = false;
            btnTest.Text = "테스트 중...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var testSettings = GetCurrentSettings();

                // 연결 시도 로그
                string logInfo = $"연결 시도 → 서버: {testSettings.Server}:{testSettings.Port}, DB: {testSettings.Database}, 사용자: {testSettings.Username}";
                System.Diagnostics.Debug.WriteLine(logInfo);

                string serverPort = testSettings.Port == 5432 ?
                    testSettings.Server :
                    $"{testSettings.Server}:{testSettings.Port}";

                string connectionString = PostgreSqlConnectionProvider.GetConnectionString(
                    serverPort, testSettings.Username, testSettings.Password, testSettings.Database);

                var testDataLayer = XpoDefault.GetDataLayer(connectionString, AutoCreateOption.None);

                using (var session = new Session(testDataLayer))
                {
                    // PostgreSQL 버전 확인으로 연결 테스트
                    var version = session.ExecuteScalar("SELECT version()");

                    // 성공 메시지
                    string successMsg = $"✅ 데이터베이스 연결 성공!\n\n" +
                                       $"📍 연결 정보:\n" +
                                       $"• 서버: {testSettings.Server}:{testSettings.Port}\n" +
                                       $"• 데이터베이스: {testSettings.Database}\n" +
                                       $"• 사용자: {testSettings.Username}\n\n" +
                                       $"🔧 서버 버전:\n{version?.ToString().Split(',')[0]}";

                    MessageBox.Show(successMsg, "연결 테스트 성공",
                                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // 🎯 상세한 오류 분석 및 해결책 제시
                ShowDetailedError(ex, GetCurrentSettings());
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "연결 테스트";
                this.Cursor = Cursors.Default;
            }
        }

        private void ShowDetailedError(Exception ex, DatabaseSettings settings)
        {
            string errorMsg = ex.Message.ToLower();
            string title = "데이터베이스 연결 실패";
            string mainMessage = "";
            string solutions = "";
            string technicalInfo = "";

            // 🔍 오류 유형별 분석
            if (errorMsg.Contains("connection") && (errorMsg.Contains("refused") || errorMsg.Contains("could not")))
            {
                mainMessage = "🔌 서버에 연결할 수 없습니다.";
                solutions = "• 서버 주소가 정확한지 확인하세요\n" +
                           "• 포트 번호가 맞는지 확인하세요 (기본값: 5432)\n" +
                           "• PostgreSQL 서비스가 실행 중인지 확인하세요\n" +
                           "• 방화벽에서 해당 포트가 차단되지 않았는지 확인하세요\n" +
                           "• 네트워크 연결 상태를 확인하세요";
            }
            else if (errorMsg.Contains("authentication") || errorMsg.Contains("password") || errorMsg.Contains("login"))
            {
                mainMessage = "🔐 사용자 인증에 실패했습니다.";
                solutions = "• 사용자명을 다시 확인하세요\n" +
                           "• 비밀번호를 정확히 입력했는지 확인하세요\n" +
                           "• 대소문자를 구분하여 입력하세요\n" +
                           "• 해당 사용자가 데이터베이스 접근 권한이 있는지 확인하세요\n" +
                           "• PostgreSQL의 pg_hba.conf 설정을 확인하세요";
            }
            else if (errorMsg.Contains("database") && (errorMsg.Contains("not exist") || errorMsg.Contains("does not exist")))
            {
                mainMessage = "🗃️ 데이터베이스를 찾을 수 없습니다.";
                solutions = "• 데이터베이스 이름을 정확히 입력했는지 확인하세요\n" +
                           "• 대소문자를 구분하여 입력하세요\n" +
                           "• 해당 데이터베이스가 실제로 생성되어 있는지 확인하세요\n" +
                           "• 데이터베이스 관리자에게 문의하세요";
            }
            else if (errorMsg.Contains("timeout") || errorMsg.Contains("time out"))
            {
                mainMessage = "⏰ 연결 시간이 초과되었습니다.";
                solutions = "• 네트워크 연결이 느릴 수 있습니다\n" +
                           "• 서버가 과부하 상태일 수 있습니다\n" +
                           "• 잠시 후 다시 시도해보세요\n" +
                           "• VPN 연결이 필요한지 확인하세요";
            }
            else if (errorMsg.Contains("permission") || errorMsg.Contains("access denied"))
            {
                mainMessage = "⛔ 데이터베이스 접근 권한이 부족합니다.";
                solutions = "• 데이터베이스 관리자에게 권한을 요청하세요\n" +
                           "• 해당 사용자가 데이터베이스 사용 권한이 있는지 확인하세요\n" +
                           "• PostgreSQL 권한 설정을 확인하세요";
            }
            else if (errorMsg.Contains("ssl") || errorMsg.Contains("certificate"))
            {
                mainMessage = "🔒 SSL 보안 연결에 문제가 있습니다.";
                solutions = "• SSL 설정을 확인하세요\n" +
                           "• 서버의 SSL 인증서가 유효한지 확인하세요\n" +
                           "• SSL 모드 설정을 변경해보세요";
            }
            else if (errorMsg.Contains("host") || errorMsg.Contains("network"))
            {
                mainMessage = "🌐 네트워크 연결에 문제가 있습니다.";
                solutions = "• 인터넷 연결을 확인하세요\n" +
                           "• 서버 주소를 다시 확인하세요\n" +
                           "• DNS 설정을 확인하세요\n" +
                           "• 프록시 설정을 확인하세요";
            }
            else
            {
                mainMessage = "❌ 데이터베이스 연결 중 알 수 없는 오류가 발생했습니다.";
                solutions = "• 모든 연결 정보를 다시 확인하세요\n" +
                           "• 네트워크 관리자 또는 데이터베이스 관리자에게 문의하세요\n" +
                           "• 시스템 로그를 확인하세요";
            }

            // 기술적 정보
            technicalInfo = $"연결 시도 정보:\n" +
                           $"• 서버: {settings.Server}:{settings.Port}\n" +
                           $"• 데이터베이스: {settings.Database}\n" +
                           $"• 사용자: {settings.Username}\n\n" +
                           $"상세 오류 메시지:\n{ex.Message}";

            // 📋 커스텀 오류 대화상자 표시
            ShowCustomErrorDialog(title, mainMessage, solutions, technicalInfo);
        }

        private void ShowCustomErrorDialog(string title, string mainMessage, string solutions, string technicalInfo)
        {
            Form errorForm = new Form();
            errorForm.Text = title;
            errorForm.Size = new System.Drawing.Size(600, 500);
            errorForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            errorForm.MaximizeBox = false;
            errorForm.MinimizeBox = false;
            errorForm.StartPosition = FormStartPosition.CenterParent;
            errorForm.BackColor = System.Drawing.Color.White;

            // 아이콘
            Label iconLabel = new Label();
            iconLabel.Text = "⚠️";
            iconLabel.Font = new System.Drawing.Font("Segoe UI", 20F);
            iconLabel.Location = new System.Drawing.Point(20, 20);
            iconLabel.Size = new System.Drawing.Size(40, 40);

            // 메인 메시지
            Label messageLabel = new Label();
            messageLabel.Text = mainMessage;
            messageLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            messageLabel.ForeColor = System.Drawing.Color.FromArgb(192, 57, 43);
            messageLabel.Location = new System.Drawing.Point(70, 25);
            messageLabel.Size = new System.Drawing.Size(500, 30);

            // 해결책 제목
            Label solutionTitleLabel = new Label();
            solutionTitleLabel.Text = "📋 해결 방법:";
            solutionTitleLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            solutionTitleLabel.Location = new System.Drawing.Point(20, 70);
            solutionTitleLabel.Size = new System.Drawing.Size(200, 25);

            // 해결책 내용
            Label solutionLabel = new Label();
            solutionLabel.Text = solutions;
            solutionLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            solutionLabel.Location = new System.Drawing.Point(20, 95);
            solutionLabel.Size = new System.Drawing.Size(550, 150);
            solutionLabel.AutoSize = false;

            // 기술적 정보 제목
            Label techTitleLabel = new Label();
            techTitleLabel.Text = "🔍 기술적 상세 정보:";
            techTitleLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            techTitleLabel.Location = new System.Drawing.Point(20, 260);
            techTitleLabel.Size = new System.Drawing.Size(200, 20);

            // 기술적 정보 내용
            TextBox techTextBox = new TextBox();
            techTextBox.Text = technicalInfo;
            techTextBox.Font = new System.Drawing.Font("Consolas", 8F);
            techTextBox.Location = new System.Drawing.Point(20, 285);
            techTextBox.Size = new System.Drawing.Size(550, 120);
            techTextBox.Multiline = true;
            techTextBox.ScrollBars = ScrollBars.Both;
            techTextBox.ReadOnly = true;
            techTextBox.BackColor = System.Drawing.Color.FromArgb(248, 248, 248);

            // 복사 버튼
            Button copyButton = new Button();
            copyButton.Text = "오류 정보 복사";
            copyButton.Location = new System.Drawing.Point(380, 420);
            copyButton.Size = new System.Drawing.Size(100, 30);
            copyButton.Click += (s, e) => {
                try
                {
                    string fullInfo = $"{mainMessage}\n\n{solutions}\n\n{technicalInfo}";
                    Clipboard.SetText(fullInfo);
                    MessageBox.Show("오류 정보가 클립보드에 복사되었습니다.", "복사 완료");
                }
                catch
                {
                    MessageBox.Show("클립보드 복사에 실패했습니다.", "오류");
                }
            };

            // 확인 버튼
            Button okButton = new Button();
            okButton.Text = "확인";
            okButton.Location = new System.Drawing.Point(490, 420);
            okButton.Size = new System.Drawing.Size(80, 30);
            okButton.DialogResult = DialogResult.OK;

            // 컨트롤 추가
            errorForm.Controls.Add(iconLabel);
            errorForm.Controls.Add(messageLabel);
            errorForm.Controls.Add(solutionTitleLabel);
            errorForm.Controls.Add(solutionLabel);
            errorForm.Controls.Add(techTitleLabel);
            errorForm.Controls.Add(techTextBox);
            errorForm.Controls.Add(copyButton);
            errorForm.Controls.Add(okButton);

            errorForm.AcceptButton = okButton;
            errorForm.ShowDialog();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                DatabaseSettings = GetCurrentSettings();

                // 🎯 현재 세션용 임시 비밀번호 설정
                SessionService.SetTemporaryPassword(DatabaseSettings.Password);

                // 설정 저장 (비밀번호 기억하기에 따라 저장 여부 결정)
                ConfigManager.SaveDatabaseSettings(DatabaseSettings);

                string saveMessage;
                if (DatabaseSettings.RememberPassword)
                {
                    saveMessage = "✅ 설정이 성공적으로 저장되었습니다!";
                }
                else
                {
                    saveMessage = "✅ 설정이 저장되었습니다.\n" +
                                 "비밀번호는 현재 세션에서만 사용되며,\n" +
                                 "다음 실행 시 다시 입력해야 합니다.";
                }

                MessageBox.Show(saveMessage, "저장 완료",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ 설정 저장 실패:\n{ex.Message}", "저장 오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                MessageBox.Show("서버 주소를 입력하세요.", "입력 오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServer.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDatabase.Text))
            {
                MessageBox.Show("데이터베이스명을 입력하세요.", "입력 오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDatabase.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("사용자명을 입력하세요.", "입력 오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("비밀번호를 입력하세요.", "입력 오류",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            return true;
        }

        private DatabaseSettings GetCurrentSettings()
        {
            return new DatabaseSettings
            {
                Server = txtServer.Text.Trim(),
                Port = (int)nudPort.Value,
                Database = txtDatabase.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text,
                RememberPassword = chkRememberPassword.Checked
            };
        }
    }
}