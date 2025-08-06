using DevExpress.XtraEditors;
using ModBusDevExpress.Service;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModBusDevExpress.Forms
{
    public partial class ConnectionTestForm : XtraForm
    {
        public ConnectionTestForm()
        {
            InitializeComponent();
        }

        // 🛡️ 안전한 UI 업데이트 메서드 (폼 해제 시 오류 방지)
        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                    {
                        this.Invoke(action);
                    }
                }
                else
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        action();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // 폼이 이미 해제된 경우 무시
            }
            catch (InvalidOperationException)
            {
                // 핸들이 생성되지 않았거나 폼이 닫힌 경우 무시
            }
        }

        public async void TestConnection(string ipAddress, int port, byte slaveId)
        {
            lblStatus.Text = "연결 테스트 중...";
            progressBar.Visible = true;
            btnClose.Enabled = false;

            await Task.Run(() =>
            {
                try
                {
                    var modbusCtrl = new ModbusCtrl();
                    modbusCtrl.Mode = Mode.TCP_IP;
                    modbusCtrl.ResponseTimeout = 5000; // 5초로 조정 (너무 길면 타임아웃)
                    modbusCtrl.ConnectTimeout = 3000;   // 연결 타임아웃 3초

                    SafeInvoke(() =>
                    {
                        lblStatus.Text = $"🔗 TCP 연결 시도 중...\n\nIP: {ipAddress}:{port}\nSlave ID: {slaveId}";
                    });

                    var result = modbusCtrl.Connect(ipAddress, port);

                    SafeInvoke(() =>
                    {
                        if (result == Result.SUCCESS)
                        {
                            lblStatus.Text = $"✅ TCP 연결 성공!\n\nIP: {ipAddress}:{port}\nSlave ID: {slaveId}";

                            // 🔧 실제 설정과 동일한 테스트: Input Register 30번 읽기
                            Int16[] registers = new Int16[1];
                            var readResult = modbusCtrl.ReadInputRegisters(slaveId, 30, 1, registers);

                            if (readResult == Result.SUCCESS)
                            {
                                lblStatus.Text += $"\n\n✅ Input Register 30 읽기: 성공\n값: {registers[0]}";
                            }
                            else
                            {
                                lblStatus.Text += $"\n\n❌ Input Register 30 읽기 실패: {readResult}";
                                lblStatus.Text += $"\n상세 오류: {modbusCtrl.GetLastErrorString()}";
                            }
                        }
                        else
                        {
                            lblStatus.Text = $"❌ TCP 연결 실패\n\n오류: {result}\n\n{modbusCtrl.GetLastErrorString()}";
                        }

                        // 연결을 정상적으로 해제
                        try
                        {
                            modbusCtrl.Close();
                        }
                        catch (Exception closeEx)
                        {
                            // 연결 해제 오류는 로그만 기록
                            System.Diagnostics.Debug.WriteLine($"연결 해제 오류: {closeEx.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        lblStatus.Text = $"❌ 테스트 실패\n\n예외: {ex.Message}";
                    });
                }
            });

            progressBar.Visible = false;
            btnClose.Enabled = true;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}