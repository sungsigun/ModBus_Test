using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;

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
                
                // DB 타입 설정 (SQL Server만 지원)
                rbSqlServer.Checked = true;
                
                txtServer.Text = settings.Server;
                nudPort.Value = settings.Port;
                txtDatabase.Text = settings.Database;
                txtUsername.Text = settings.Username;
                txtPassword.Text = settings.Password;
                chkRememberPassword.Checked = settings.RememberPassword;
                
                // 🎯 저장된 회사명 설정
                if (!string.IsNullOrEmpty(settings.SelectedCompany))
                {
                    cmbCompany.Items.Add(settings.SelectedCompany);
                    cmbCompany.SelectedItem = settings.SelectedCompany;
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
            try
            {
                // 🎯 입력 검증
                if (!ValidateInput())
                    return;

                var testSettings = GetSettingsFromUI();
                
                // SQL Server 연결 테스트
                string connectionString = $"Server={testSettings.Server},{testSettings.Port};Database={testSettings.Database};User Id={testSettings.Username};Password={testSettings.Password};Connection Timeout=5;";
                
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT 1", connection))
                    {
                        command.ExecuteScalar();
                    }
                }

                MessageBox.Show("데이터베이스 연결에 성공했습니다!", "연결 테스트", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // 🎯 연결 성공 시 회사 목록 로드
                LoadCompanyList();
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
                DatabaseType = DatabaseType.SqlServer,  // SQL Server만 지원
                Server = txtServer.Text.Trim(),
                Port = (int)nudPort.Value,
                Database = txtDatabase.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text,
                RememberPassword = chkRememberPassword.Checked,
                SelectedCompany = cmbCompany.SelectedItem?.ToString() ?? "",  // 🎯 선택된 회사명 포함
                SelectedCompanyGuid = GetSelectedCompanyGuid()  // 🎯 선택된 회사의 GUID 포함
            };
        }
        
        // 회사명-GUID 매핑을 위한 딕셔너리
        private Dictionary<string, string> companyGuidMap = new Dictionary<string, string>();
        
        // 선택된 회사의 GUID 반환
        private string GetSelectedCompanyGuid()
        {
            string selectedCompany = cmbCompany.SelectedItem?.ToString() ?? "";
            if (companyGuidMap.ContainsKey(selectedCompany))
            {
                return companyGuidMap[selectedCompany];
            }
            return "";
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

        // 🎯 DB에서 회사 목록 조회
        private void LoadCompanyList()
        {
            try
            {
                var settings = GetSettingsFromUI();
                List<string> companyList = new List<string>();

                // 🔍 연결 정보 디버깅
                string debugInfo = $"서버: {settings.Server}, 포트: {settings.Port}, DB: {settings.Database}, 사용자: {settings.Username}";
                
                if (settings.DatabaseType == DatabaseType.SqlServer)
                {
                    string connectionString = $"Server={settings.Server},{settings.Port};Database={settings.Database};User Id={settings.Username};Password={settings.Password};Connection Timeout=10;TrustServerCertificate=true;";
                    
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            
                            // 🔍 테이블 존재 여부 확인
                            using (var checkCommand = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'company'", connection))
                            {
                                int tableCount = (int)checkCommand.ExecuteScalar();
                                if (tableCount == 0)
                                {
                                    MessageBox.Show("'company' 테이블이 존재하지 않습니다.\n데이터베이스를 확인해주세요.", 
                                        "테이블 없음", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                            
                            // 🔍 회사 데이터 조회
                            using (var command = new SqlCommand("SELECT CompanyName, Id FROM company ORDER BY CompanyName", connection))
                            {
                                using (var reader = command.ExecuteReader())
                                {
                                    companyGuidMap.Clear(); // 기존 매핑 정리
                                    int recordCount = 0;
                                    
                                    while (reader.Read())
                                    {
                                        string companyName = reader["CompanyName"]?.ToString();
                                        string companyGuid = reader["Id"]?.ToString();
                                        recordCount++;
                                        
                                        if (!string.IsNullOrEmpty(companyName) && !string.IsNullOrEmpty(companyGuid))
                                        {
                                            companyGuidMap[companyName] = companyGuid; // GUID 매핑 저장
                                            companyList.Add(companyName);
                                        }
                                    }
                                    
                                    if (recordCount == 0)
                                    {
                                        MessageBox.Show("company 테이블에 데이터가 없습니다.", 
                                            "데이터 없음", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        string errorMsg = $"데이터베이스 연결 실패\n\n연결 정보: {debugInfo}\n\nSQL 오류: {sqlEx.Message}\n오류 번호: {sqlEx.Number}";
                        MessageBox.Show(errorMsg, "DB 연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // UI 컨트롤이 있다면 회사 목록 업데이트
                if (companyList.Count > 0)
                {
                    UpdateCompanyComboBox(companyList);
                    MessageBox.Show($"{companyList.Count}개의 회사를 찾았습니다.", 
                        "회사 목록 로드 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("조회된 회사가 없습니다.", 
                        "회사 목록 없음", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"회사 목록을 가져오는데 실패했습니다.\n\n일반 오류: {ex.Message}\n\n상세 정보: {ex.StackTrace}", 
                    "회사 목록 조회 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🎯 회사 콤보박스 업데이트
        private void UpdateCompanyComboBox(List<string> companies)
        {
            // 기존 선택값 백업
            string currentSelection = cmbCompany.SelectedItem?.ToString();
            
            // 콤보박스 업데이트
            cmbCompany.Items.Clear();
            if (companies.Count > 0)
            {
                cmbCompany.Items.AddRange(companies.ToArray());
                
                // 이전 선택값이 있으면 복원, 없으면 첫 번째 항목 선택
                if (!string.IsNullOrEmpty(currentSelection) && companies.Contains(currentSelection))
                {
                    cmbCompany.SelectedItem = currentSelection;
                }
                else
                {
                    cmbCompany.SelectedIndex = 0;
                }
                
                MessageBox.Show($"회사 목록을 성공적으로 조회했습니다. ({companies.Count}개)", 
                    "회사 목록 조회", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("조회된 회사가 없습니다.", "회사 목록", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}