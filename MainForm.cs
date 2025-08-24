using DevExpress.XtraEditors;
using ModBusDevExpress.Forms;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ModBusDevExpress
{
    public partial class MainForm : DevExpress.XtraEditors.XtraForm
    {
        private List<ModbusDeviceSettings> activeDevices = new List<ModbusDeviceSettings>();

        public MainForm()
        {
            InitializeComponent();
            InitializeMenu();
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
            refreshDevicesMenu.Click += (s, e) => RefreshDevices();

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
                using (var configForm = new DatabaseConfigForm())
                {
                    if (configForm.ShowDialog() == DialogResult.OK)
                    {
                        XtraMessageBox.Show("설정이 변경되었습니다.\n프로그램을 재시작하세요.",
                                           "설정 변경", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
            settingsMenu.DropDownItems.Add(dbConfigMenu);

            // 메뉴 추가
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(deviceMenu);
            menuStrip.Items.Add(dataMenu);
            menuStrip.Items.Add(settingsMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RefreshDevices();
        }

        public void RefreshDevices()
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

            int top = 0;
            int left = 0;
            int wi = 0;
            int hi = 0;
            bool bw = true;

            foreach (var settings in deviceSettings.Where(d => d.IsActive))
            {
                try
                {
                    // 🎯 저장주기 포함된 설정 문자열로 변환
                    string configString = settings.ToConfigString();

                    // 🎯 Form1 생성 및 초기화
                    Form1 form = new Form1(configString);
                    form.MdiParent = this;
                    form.Show();

                    // 위치 계산
                    if (this.ClientSize.Width < left + form.Width)
                    {
                        top = top + form.Height + 5;
                        form.Left = 0;
                        left = form.Width + 5;
                        bw = false;
                        hi = top + form.Height;
                    }
                    else
                    {
                        form.Left = left;
                        left += form.Width + 5;
                        if (bw)
                        {
                            wi = left + 16;
                            hi = top + form.Height;
                        }
                    }
                    form.Top = top;

                    // 활성 디바이스 목록에 추가
                    settings.DeviceForm = form;
                    activeDevices.Add(settings);

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

            // 창 크기 조정
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

            int top = 0;
            int left = 0;

            foreach (var device in activeDevices)
            {
                if (device.DeviceForm == null || device.DeviceForm.IsDisposed) continue;

                var form = device.DeviceForm;

                if (this.ClientSize.Width < left + form.Width)
                {
                    top = top + form.Height + 5;
                    form.Left = 0;
                    left = form.Width + 5;
                }
                else
                {
                    form.Left = left;
                    left += form.Width + 5;
                }
                form.Top = top;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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
    }
}