using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using DevExpress.Xpo.DB;
using DevExpress.Xpo;

namespace ModBusDevExpress.Forms
{
    public partial class DatabaseConfigForm : Form
    {
        public DatabaseSettings DatabaseSettings { get; private set; }

        public DatabaseConfigForm()
        {
            InitializeComponent();
            
            // 🎯 기본값 설정 (처음 실행 시)
            chkRememberPassword.Checked = true; // 비밀번호 기억 기본 활성화
            rbSqlServer.Checked = true; // SQL Server 기본 선택
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var settings = ConfigManager.LoadDatabaseSettings();
                
                // DB 타입 설정
                rbSqlServer.Checked = settings.DatabaseType == DatabaseType.SqlServer;
                rbPostgreSQL.Checked = settings.DatabaseType == DatabaseType.PostgreSQL;
                
                txtServer.Text = settings.Server;
                nudPort.Value = settings.Port;
                txtDatabase.Text = settings.Database;
                txtUsername.Text = settings.Username;
                txtPassword.Text = settings.Password;
                chkRememberPassword.Checked = settings.RememberPassword;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 로드 실패: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                // 🎯 입력 검증
                if (!ValidateInput())
                    return;

                var testSettings = GetSettingsFromUI();
                
                // 🔧 가장 간단하고 안전한 연결 테스트
                if (testSettings.DatabaseType == DatabaseType.SqlServer)
                {
                    // System.Data.SqlClient 사용 (더 안정적)
                    string connectionString = $"Server={testSettings.Server},{testSettings.Port};Database={testSettings.Database};User Id={testSettings.Username};Password={testSettings.Password};Connection Timeout=5;";
                    
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (var command = new SqlCommand("SELECT 1", connection))
                        {
                            command.ExecuteScalar();
                        }
                    }
                }
                else
                {
                    // PostgreSQL은 XPO 방식 사용
                    string connectionString = testSettings.GetConnectionString();
                    var tempDataLayer = XpoDefault.GetDataLayer(connectionString, AutoCreateOption.None);
                    using (var tempUow = new UnitOfWork(tempDataLayer))
                    {
                        // 간단한 연결 확인
                        tempUow.ExecuteScalar("SELECT 1");
                    }
                }

                MessageBox.Show("데이터베이스 연결에 성공했습니다!", "연결 테스트", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터베이스 연결에 실패했습니다.\n\n오류: {ex.Message}", 
                    "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                var settings = GetSettingsFromUI();
                ConfigManager.SaveDatabaseSettings(settings);
                
                DatabaseSettings = settings;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 저장 실패: {ex.Message}", "오류", 
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
                MessageBox.Show("데이터베이스 이름을 입력하세요.", "입력 오류", 
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

        private DatabaseSettings GetSettingsFromUI()
        {
            return new DatabaseSettings
            {
                DatabaseType = rbSqlServer.Checked ? DatabaseType.SqlServer : DatabaseType.PostgreSQL,
                Server = txtServer.Text.Trim(),
                Port = (int)nudPort.Value,
                Database = txtDatabase.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text,
                RememberPassword = chkRememberPassword.Checked
            };
        }

        private void DatabaseType_CheckedChanged(object sender, EventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
                if (radioButton == rbPostgreSQL)
                {
                    nudPort.Value = 5432; // PostgreSQL 기본 포트
                    txtServer.Text = "localhost"; // PostgreSQL 기본 서버
                    txtServer.ReadOnly = false;
                    nudPort.ReadOnly = false;
                    txtServer.BackColor = System.Drawing.SystemColors.Window;
                    nudPort.BackColor = System.Drawing.SystemColors.Window;
                }
                else if (radioButton == rbSqlServer)
                {
                    // 🔒 SQL Server는 고정값 사용
                    nudPort.Value = 11433; // 고정 포트
                    txtServer.Text = "175.45.202.13"; // 고정 서버 주소
                    txtServer.ReadOnly = true;
                    nudPort.ReadOnly = true;
                    txtServer.BackColor = System.Drawing.SystemColors.Control;
                    nudPort.BackColor = System.Drawing.SystemColors.Control;
                }
            }
        }
    }
}