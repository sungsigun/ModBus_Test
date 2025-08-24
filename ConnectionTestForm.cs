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
                    modbusCtrl.ResponseTimeout = 3000;

                    var result = modbusCtrl.Connect(ipAddress, port);

                    this.Invoke(new Action(() =>
                    {
                        if (result == Result.SUCCESS)
                        {
                            lblStatus.Text = $"✅ 연결 성공!\n\nIP: {ipAddress}:{port}\nSlave ID: {slaveId}";

                            // 간단한 읽기 테스트
                            Int16[] registers = new Int16[1];
                            var readResult = modbusCtrl.ReadHoldingRegisters(slaveId, 0, 1, registers);

                            if (readResult == Result.SUCCESS)
                            {
                                lblStatus.Text += "\n\n읽기 테스트: 성공";
                            }
                            else
                            {
                                lblStatus.Text += $"\n\n읽기 테스트: {readResult}";
                            }
                        }
                        else
                        {
                            lblStatus.Text = $"❌ 연결 실패\n\n오류: {result}\n\n{modbusCtrl.GetLastErrorString()}";
                        }

                        modbusCtrl.Close();
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = $"❌ 테스트 실패\n\n예외: {ex.Message}";
                    }));
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

    partial class ConnectionTestForm
    {
        private System.ComponentModel.IContainer components = null;
        private DevExpress.XtraEditors.LabelControl lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private DevExpress.XtraEditors.SimpleButton btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblStatus = new DevExpress.XtraEditors.LabelControl();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.SuspendLayout();

            // Form
            this.Text = "연결 테스트";
            this.Size = new System.Drawing.Size(400, 250);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // lblStatus
            this.lblStatus.Location = new System.Drawing.Point(20, 20);
            this.lblStatus.Size = new System.Drawing.Size(360, 120);
            this.lblStatus.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
            this.lblStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblStatus.Text = "연결 테스트 준비 중...";

            // progressBar
            this.progressBar.Location = new System.Drawing.Point(20, 150);
            this.progressBar.Size = new System.Drawing.Size(360, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;

            // btnClose
            this.btnClose.Location = new System.Drawing.Point(300, 190);
            this.btnClose.Size = new System.Drawing.Size(80, 30);
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            // Add controls
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnClose);

            this.ResumeLayout(false);
        }
    }
}