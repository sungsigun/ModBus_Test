using DevExpress.XtraEditors;
using ModBusDevExpress.Forms;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using ModBusDevExpress.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ModBusDevExpress
{
    // 🎯 저장 방식 열거형
    public enum SaveMethod
    {
        Periodic,           // 주기별 강제 저장
        ChangeDetection     // 변화 감지 저장
    }

    public partial class MainForm : DevExpress.XtraEditors.XtraForm
    {
        private List<ModbusDeviceSettings> activeDevices = new List<ModbusDeviceSettings>();
        private Timer globalSaveTimer;  // 🎯 전역 저장 타이머 (디바이스별 저장주기 체크용)
        private SaveMethod currentSaveMethod = SaveMethod.Periodic;  // 🎯 현재 저장 방식

        // 🔄 자동 디바이스 새로고침 시스템 (하이브리드 방식)
        private Timer autoRefreshMonitorTimer;  // 30초마다 체크
        private const bool ENABLE_AUTO_DEVICE_REFRESH = false; // 전체 자동 새로고침 비활성화
        private Dictionary<string, int> deviceFailureCounts = new Dictionary<string, int>();  // 디바이스별 연속 실패 횟수
        private Dictionary<string, DateTime> deviceLastSaveTime = new Dictionary<string, DateTime>();  // 디바이스별 마지막 저장 시간
        private DateTime lastAutoRefreshTime = DateTime.MinValue;  // 마지막 자동 새로고침 시간
        private const int MAX_CONSECUTIVE_FAILURES = Constants.MAX_CONSECUTIVE_FAILURES;  // 연속 실패 임계값
        private const double MIN_SAVE_SUCCESS_RATE = Constants.MIN_SAVE_SUCCESS_RATE;  // 최소 저장 성공률 (50%)
        private const int AUTO_REFRESH_COOLDOWN_MINUTES = Constants.AUTO_REFRESH_COOLDOWN_MINUTES;  // 자동 새로고침 간격 (10분)

        public MainForm()
        {
            InitializeComponent();
            InitializeMenu();
            InitializeGlobalSaveTimer();
            this.Resize += MainForm_Resize; // 리사이즈 시 카드 재배치/리사이즈
        }
        
        // 🎯 전역 저장 타이머 초기화 (디바이스별 저장주기 체크용)
        private void InitializeGlobalSaveTimer()
        {
            globalSaveTimer = new Timer();
            globalSaveTimer.Interval = 30 * 1000; // 30초마다 체크 (디바이스별 저장주기 확인용)
            globalSaveTimer.Tick += GlobalSaveTimer_Tick;
            globalSaveTimer.Start();

            // 🔄 자동 디바이스 새로고침 모니터링 타이머 초기화
            if (ENABLE_AUTO_DEVICE_REFRESH)
            {
                autoRefreshMonitorTimer = new Timer();
                autoRefreshMonitorTimer.Interval = 30 * 1000; // 30초마다 체크
                autoRefreshMonitorTimer.Tick += AutoRefreshMonitor_Tick;
                autoRefreshMonitorTimer.Start();
            }
            
            // 로그 기록
            LoggingHelper.LogSystem($"전역 저장 타이머 시작 - 30초마다 디바이스별 저장주기 체크" +
                (ENABLE_AUTO_DEVICE_REFRESH
                    ? "\r\n자동 새로고침 모니터링 시작 - 간격: 30초"
                    : "\r\n자동 새로고침 모니터링 비활성화"));
        }
        
        // 🎯 전역 저장 타이머 이벤트 - 각 디바이스별 저장주기에 따라 저장
        private void GlobalSaveTimer_Tick(object sender, EventArgs e)
        {
            int savedCount = 0;
            DateTime saveTime = DateTime.Now;
            
            foreach (var device in activeDevices)
            {
                if (device.DeviceForm != null && !device.DeviceForm.IsDisposed)
                {
                    // 디바이스별 저장주기 체크
                    string deviceKey = device.DeviceName;
                    int deviceSaveInterval = Math.Max(10, device.SaveInterval);
                    DateTime lastSaved = deviceLastSaveTime.ContainsKey(deviceKey) ? deviceLastSaveTime[deviceKey] : DateTime.MinValue;
                    if (lastSaved != DateTime.MinValue)
                    {
                        var seconds = (saveTime - lastSaved).TotalSeconds;
                        if (seconds < deviceSaveInterval)
                        {
                            // 아직 저장 주기 미도달
                            LoggingHelper.LogSystem($"저장 건너뜀 - {device.DeviceName}: 주기 미도달({seconds:F0}/{deviceSaveInterval}s)");
                            continue;
                        }
                    }

                    try
                    {
                        // 🔍 저장 시도 로그 추가
                        LoggingHelper.LogSystem($"저장 시도 - {device.DeviceName}: SaveDataToDatabase() 호출");
                        
                        device.DeviceForm.SaveDataToDatabase();
                        savedCount++;
                        
                        // 🔄 저장 성공 시 실패 카운트 리셋 및 마지막 저장 시간 업데이트
                        deviceFailureCounts[deviceKey] = 0;
                        deviceLastSaveTime[deviceKey] = saveTime;
                        
                        LoggingHelper.LogSystem($"저장 성공 - {device.DeviceName}");
                    }
                    catch (Exception ex)
                    {
                        // 🔄 저장 실패 시 실패 카운트 증가
                        deviceFailureCounts[deviceKey] = deviceFailureCounts.GetValueOrDefault(deviceKey, 0) + 1;
                        
                        LoggingHelper.LogSystem($"전역 저장 오류 - {device.DeviceName}: {ex.Message}\r\n상세 오류: {ex.InnerException?.Message}\r\n스택 트레이스: {ex.StackTrace} (연속실패: {deviceFailureCounts[deviceKey]}회)");
                    }
                }
            }
            
            if (savedCount > 0)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 전역 저장 완료 - {savedCount}개 디바이스 최신 데이터 저장\r\n"
                );
            }
            else
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 전역 저장 완료 - 저장할 데이터 없음(모든 디바이스)\r\n"
                );
            }
        }

        // 🔄 자동 디바이스 새로고침 모니터링 (하이브리드 방식)
        private void AutoRefreshMonitor_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!ENABLE_AUTO_DEVICE_REFRESH) return; // 전역 자동 새로고침 기능 비활성화
                DateTime now = DateTime.Now;
                bool needAutoRefresh = false;
                string refreshReason = "";

                // 🔍 조건 1: 개별 디바이스 연속 실패 체크
                foreach (var deviceKey in deviceFailureCounts.Keys.ToList())
                {
                    if (deviceFailureCounts[deviceKey] >= MAX_CONSECUTIVE_FAILURES)
                    {
                        needAutoRefresh = true;
                        refreshReason = $"디바이스 '{deviceKey}' 연속 {deviceFailureCounts[deviceKey]}회 저장 실패";
                        break;
                    }
                }

                // 🔍 조건 2: 전체 저장 성공률 체크 (최근 5분간)
                if (!needAutoRefresh && activeDevices.Count > 0)
                {
                    DateTime fiveMinutesAgo = now.AddMinutes(-5);
                    int recentSaveCount = 0;

                    foreach (var device in activeDevices)
                    {
                        string deviceKey = device.DeviceName;
                        if (deviceLastSaveTime.ContainsKey(deviceKey) && 
                            deviceLastSaveTime[deviceKey] > fiveMinutesAgo)
                        {
                            recentSaveCount++;
                        }
                    }

                    double successRate = (double)recentSaveCount / activeDevices.Count;
                    if (successRate < MIN_SAVE_SUCCESS_RATE)
                    {
                        needAutoRefresh = true;
                        refreshReason = $"전체 저장 성공률 {successRate:P0} (임계값: {MIN_SAVE_SUCCESS_RATE:P0})";
                    }
                }

                // 🔍 조건 3: 안전장치 - 마지막 새로고침 후 10분 경과 체크
                bool cooldownPassed = (now - lastAutoRefreshTime).TotalMinutes >= AUTO_REFRESH_COOLDOWN_MINUTES;

                if (needAutoRefresh && cooldownPassed)
                {
                    PerformAutoDeviceRefresh(refreshReason);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 자동 새로고침 모니터링 오류: {ex.Message}\r\n"
                );
            }
        }

        // 🔄 자동 디바이스 새로고침 실행
        private async void PerformAutoDeviceRefresh(string reason)
        {
            try
            {
                lastAutoRefreshTime = DateTime.Now;
                
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 🔄 자동 디바이스 새로고침 시작 - 사유: {reason}\r\n"
                );

                // 모든 실패 카운트 리셋
                deviceFailureCounts.Clear();

                // 디바이스 새로고침 실행 (기존 RefreshDevices 메서드 사용)
                await RefreshDevices();

                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ✅ 자동 디바이스 새로고침 완료\r\n"
                );
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ❌ 자동 디바이스 새로고침 실패: {ex.Message}\r\n"
                );
            }
        }

        private void InitializeMenu()
        {
            var menuStrip = new MenuStrip();

            // 파일 메뉴
            var fileMenu = new ToolStripMenuItem("파일(&F)");
            var exitMenu = new ToolStripMenuItem("종료(&X)");
            exitMenu.Click += (s, e) => Application.Exit();
            fileMenu.DropDownItems.Add(exitMenu);

            // 디바이스 메뉴
            var deviceMenu = new ToolStripMenuItem("디바이스(&D)");
            var deviceSettingsMenu = new ToolStripMenuItem("디바이스 설정(&S)");
            deviceSettingsMenu.Click += (s, e) => {
                var settingsForm = new DeviceSettingsForm();
                settingsForm.Owner = this;
                settingsForm.ShowDialog();
            };

            var refreshDevicesMenu = new ToolStripMenuItem("디바이스 새로고침(&R)");
            refreshDevicesMenu.Click += async (s, e) => await RefreshDevices();

            deviceMenu.DropDownItems.Add(deviceSettingsMenu);
            deviceMenu.DropDownItems.Add(new ToolStripSeparator());
            deviceMenu.DropDownItems.Add(refreshDevicesMenu);

            // 데이터 메뉴
            var dataMenu = new ToolStripMenuItem("데이터(&A)");
            var viewDataMenu = new ToolStripMenuItem("데이터 조회(&V)");
            viewDataMenu.Click += (s, e) => {
                var dataViewer = new DataViewerForm();
                dataViewer.Show();
            };
            dataMenu.DropDownItems.Add(viewDataMenu);

            // 설정 메뉴
            var settingsMenu = new ToolStripMenuItem("설정(&S)");
            var dbConfigMenu = new ToolStripMenuItem("데이터베이스 설정(&D)");
            dbConfigMenu.Click += (s, e) => {
                using (var configForm = new Forms.DatabaseConfigForm())
                {
                    if (configForm.ShowDialog() == DialogResult.OK)
                    {
                        // 🔧 설정 변경 후 SessionService 재초기화
                        try
                        {
                            SessionService.ResetInstance(); 
                            var newSessionService = SessionService.Instance;
                            
                            MessageBox.Show("데이터베이스 설정이 변경되었습니다.", "설정 변경", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"새로운 데이터베이스 연결에 실패했습니다.\n\n{ex.Message}", 
                                "연결 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            
            // 🎯 NEW: 저장 방식 설정 메뉴
            var saveMethodMenu = new ToolStripMenuItem("저장 방식 설정(&M)");
            var periodicSaveMenu = new ToolStripMenuItem("주기별 강제 저장") { Checked = true };
            var changeDetectionSaveMenu = new ToolStripMenuItem("변화 감지 저장") { Checked = false };
            
            periodicSaveMenu.Click += (s, e) => {
                SetSaveMethod(SaveMethod.Periodic);
                periodicSaveMenu.Checked = true;
                changeDetectionSaveMenu.Checked = false;
            };
            
            changeDetectionSaveMenu.Click += (s, e) => {
                SetSaveMethod(SaveMethod.ChangeDetection);
                periodicSaveMenu.Checked = false;
                changeDetectionSaveMenu.Checked = true;
            };
            
            saveMethodMenu.DropDownItems.Add(periodicSaveMenu);
            saveMethodMenu.DropDownItems.Add(changeDetectionSaveMenu);
            
            settingsMenu.DropDownItems.Add(dbConfigMenu);
            settingsMenu.DropDownItems.Add(new ToolStripSeparator());
            settingsMenu.DropDownItems.Add(saveMethodMenu);

            // 진단 메뉴
            var diagMenu = new ToolStripMenuItem("진단(&T)");
            var liveProbeMenu = new ToolStripMenuItem("라이브 모니터 (0.1초)");
            liveProbeMenu.Click += (s, e) => {
                // 활성화된 첫 번째 디바이스 설정을 사용
                var deviceSettings = activeDevices.FirstOrDefault();
                if (deviceSettings != null)
                {
                    var probe = new ModBusDevExpress.Forms.LiveProbeForm(deviceSettings);
                    probe.Show(this);
                }
                else
                {
                    XtraMessageBox.Show("활성화된 디바이스가 없습니다. 먼저 디바이스를 설정하고 새로고침하세요.", 
                        "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            diagMenu.DropDownItems.Add(liveProbeMenu);

            // 메뉴 추가
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(deviceMenu);
            menuStrip.Items.Add(dataMenu);
            menuStrip.Items.Add(settingsMenu);
            menuStrip.Items.Add(diagMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await RefreshDevices();
        }

        public async System.Threading.Tasks.Task RefreshDevices()
        {
            // 기존 디바이스 폼들 닫기
            foreach (var device in activeDevices.ToList())
            {
                if (device.DeviceForm != null && !device.DeviceForm.IsDisposed)
                {
                    device.DeviceForm.Close();
                }
            }
            activeDevices.Clear();

            // 디바이스 설정 로드
            var deviceSettings = DeviceConfigManager.LoadDeviceSettings();

            int wi = 0;
            int hi = 0;
            int deviceIndex = 0; // 디바이스 순서 추적

            foreach (var settings in deviceSettings.Where(d => d.IsActive))
            {
                try
                {
                    // 동시 표시: 지연 없이 바로 생성 및 초기화
                    // 🎯 저장주기 포함된 설정 문자열로 변환
                    string configString = settings.ToConfigString();

                    // 🎯 Form1 생성 및 초기화 (인덱스 전달)
                    Form1 form = new Form1(configString, deviceIndex);
                    form.MdiParent = this;
                    form.Show();
                    
                    // 🎯 현재 저장 방식 적용
                    form.SetSaveMethod(currentSaveMethod);

                    // 초기 위치는 임시로 배치. 실제 크기/위치는 MainForm_Resize에서 일괄 조정
                    int row = deviceIndex / 2;
                    int col = deviceIndex % 2;
                    form.Left = col * (form.Width + 10);
                    form.Top = row * (form.Height + 10);
                    
                    // 전체 크기 계산
                    if (col == 1 || deviceIndex == activeDevices.Count - 1)
                    {
                        wi = Math.Max(wi, (col + 1) * (form.Width + 5) + 16);
                    }
                    if (row > 0 || deviceIndex == activeDevices.Count - 1)
                    {
                        hi = Math.Max(hi, (row + 1) * (form.Height + 5));
                    }

                    // 활성 디바이스 목록에 추가
                    settings.DeviceForm = form;
                    activeDevices.Add(settings);
                    
                    deviceIndex++; // 다음 디바이스를 위한 인덱스 증가

                    // 🎯 로그 기록
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Application.StartupPath,
                        $"log{DateTime.Now:yyyyMMdd}.txt"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 디바이스 '{settings.DeviceName}' 로드 완료 " +
                        $"(수집주기: {settings.Interval}초, 저장주기: {settings.SaveInterval}초)\r\n"
                    );
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"디바이스 '{settings.DeviceName}' 로드 실패: {ex.Message}",
                                       "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // 오류 로그 기록
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Application.StartupPath,
                        $"log{DateTime.Now:yyyyMMdd}.txt"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 디바이스 로드 오류 - {settings.DeviceName}: {ex.Message}\r\n"
                    );
                }
            }

            // 창 크기 조정 (초기 기본 동작)
            if (activeDevices.Count > 0)
            {
                this.Width = Math.Max(wi + 10, 800);
                this.Height = Math.Max(hi + 65, 600);
            }
            else
            {
                this.Width = 800;
                this.Height = 600;
            }

            // 🎯 상태 표시 개선
            string statusText = $"데이터집계 시스템 - {activeDevices.Count}개 디바이스 활성";
            if (activeDevices.Count > 0)
            {
                var totalCollections = activeDevices.Sum(d => 60 / d.Interval); // 분당 수집 횟수
                var totalSaves = activeDevices.Sum(d => 60 / d.SaveInterval);   // 분당 저장 횟수
                statusText += $" (분당 수집: {totalCollections}회, 저장: {totalSaves}회)";
            }
            this.Text = statusText;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (activeDevices.Count == 0) return;

            int columns = 2;
            int spacing = 5;
            int menuH = this.MainMenuStrip != null ? this.MainMenuStrip.Height : 0;

            int left = 0;
            int top = 0;
            foreach (var device in activeDevices)
            {
                if (device.DeviceForm == null || device.DeviceForm.IsDisposed) continue;
                var form = device.DeviceForm;

                if (this.ClientSize.Width < left + form.Width)
                {
                    top = top + form.Height + spacing;
                    form.Left = 0;
                    left = form.Width + spacing;
                }
                else
                {
                    form.Left = left;
                    left += form.Width + spacing;
                }
                form.Top = top;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 🎯 전역 저장 타이머 정지
            globalSaveTimer?.Stop();
            globalSaveTimer?.Dispose();
            
            // 🔄 자동 새로고침 모니터링 타이머 정지
            autoRefreshMonitorTimer?.Stop();
            autoRefreshMonitorTimer?.Dispose();
            
            // 🎯 종료 시 모든 디바이스의 미저장 데이터 처리
            int unsavedCount = 0;
            foreach (var device in activeDevices)
            {
                if (device.DeviceForm != null && !device.DeviceForm.IsDisposed)
                {
                    // Form1에서 미저장 데이터 저장 요청
                    try
                    {
                        device.DeviceForm.SaveDataToDatabase(); // 이제 public 메서드로 직접 호출
                        unsavedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(
                            System.IO.Path.Combine(Application.StartupPath,
                            $"log{DateTime.Now:yyyyMMdd}.txt"),
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 종료시 데이터 저장 오류 - {device.DeviceName}: {ex.Message}\r\n"
                        );
                    }

                    device.DeviceForm.Close();
                }
            }

            if (unsavedCount > 0)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(Application.StartupPath,
                    $"log{DateTime.Now:yyyyMMdd}.txt"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 프로그램 종료 - {unsavedCount}개 디바이스의 미저장 데이터 처리 완료\r\n"
                );
            }

            base.OnFormClosing(e);
        }
        
        // 🎯 저장 방식 설정 메서드
        private void SetSaveMethod(SaveMethod method)
        {
            currentSaveMethod = method;
            
            // 모든 디바이스에 저장 방식 전달
            foreach (var device in activeDevices)
            {
                if (device.DeviceForm != null && !device.DeviceForm.IsDisposed)
                {
                    device.DeviceForm.SetSaveMethod(method);
                }
            }
            
            // 로그 기록
            string methodName = method == SaveMethod.Periodic ? "주기별 강제 저장" : "변화 감지 저장";
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(Application.StartupPath, $"log{DateTime.Now:yyyyMMdd}.txt"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 저장 방식 변경: {methodName}\r\n"
            );
            
            XtraMessageBox.Show($"저장 방식이 '{methodName}'으로 변경되었습니다.", 
                              "설정 변경", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}