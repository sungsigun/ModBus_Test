using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Windows.Forms;

namespace ModBusDevExpress
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        ModbusCtrl modbusCtrl;
        string[] acnt = new string[20];
        ushort _adrr = 0;
        ushort _sid = 1;
        ushort _length = 10;
        string[] _aMem = null;
        string[] _aMem2 = null;
        string _Faci = "";
        string[] _aitem = null;
        string _ip = "";
        string _setV = "";

        // 🎯 실시간 표시와 DB 저장을 위한 추가 변수들
        private Timer saveTimer;           // DB 저장용 타이머
        private int _saveInterval = 60;    // 저장 주기 (초)
        private DateTime _lastSaveTime = DateTime.MinValue;
        private List<DataBuffer> _dataBuffer = new List<DataBuffer>();  // 데이터 버퍼
        private bool _hasUnsavedData = false;  // 저장되지 않은 데이터 존재 여부

        // 디자인 색상
        private Color primaryColor = Color.FromArgb(52, 152, 219);    // 파란색
        private Color successColor = Color.FromArgb(46, 204, 113);    // 초록색
        private Color dangerColor = Color.FromArgb(231, 76, 60);      // 빨간색
        private Color warningColor = Color.FromArgb(243, 156, 18);    // 주황색
        private Color bgColor = Color.FromArgb(245, 247, 250);        // 연한 회색
        private Color cardColor = Color.White;                        // 카드 배경
        private Color textColor = Color.FromArgb(44, 62, 80);         // 진한 글자색
        private Color lightTextColor = Color.FromArgb(127, 140, 141); // 연한 글자색

        // 🎯 데이터 버퍼 클래스
        private class DataBuffer
        {
            public DateTime Timestamp { get; set; }
            public string FacilityCode { get; set; }
            public Dictionary<string, double> Values { get; set; } = new Dictionary<string, double>();
        }

        public Form1(string setV)
        {
            _setV = setV;
            InitializeComponent();
            ApplyModernDesign();
            InitializeSaveTimer();
        }

        // 🎯 저장 타이머 초기화
        private void InitializeSaveTimer()
        {
            saveTimer = new Timer();
            saveTimer.Tick += SaveTimer_Tick;
            saveTimer.Enabled = false; // 연결 후 시작
        }

        private void ApplyModernDesign()
        {
            // 폼 스타일
            this.BackColor = bgColor;
            this.FormBorderStyle = FormBorderStyle.None;
            this.FormBorderEffect = DevExpress.XtraEditors.FormBorderEffect.Shadow;

            // 패널 스타일
            panel1.BackColor = cardColor;
            panel1.Paint += Panel1_Paint;

            // 설비명 레이블
            lbFaci.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
            lbFaci.ForeColor = textColor;
            lbFaci.Location = new Point(20, 15);

            // 연결 상태 섹션
            label1.Text = "수집주기";
            label1.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            label1.ForeColor = lightTextColor;
            label1.Location = new Point(20, 55);

            lbItv.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            lbItv.ForeColor = textColor;
            lbItv.Location = new Point(20, 75);

            label2.Font = new Font("맑은 고딕", 9F);
            label2.ForeColor = lightTextColor;
            label2.Location = new Point(55, 78);

            // 🎯 저장주기 표시 라벨 추가
            var lblSaveInterval = new Label();
            lblSaveInterval.Text = "저장주기";
            lblSaveInterval.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            lblSaveInterval.ForeColor = lightTextColor;
            lblSaveInterval.Location = new Point(230, 55);
            lblSaveInterval.AutoSize = true;
            panel1.Controls.Add(lblSaveInterval);

            var lbSaveItv = new Label();
            lbSaveItv.Name = "lbSaveItv";
            lbSaveItv.Text = _saveInterval.ToString();
            lbSaveItv.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            lbSaveItv.ForeColor = textColor;
            lbSaveItv.Location = new Point(230, 75);
            lbSaveItv.AutoSize = true;
            panel1.Controls.Add(lbSaveItv);

            var lblSaveUnit = new Label();
            lblSaveUnit.Text = "초";
            lblSaveUnit.Font = new Font("맑은 고딕", 9F);
            lblSaveUnit.ForeColor = lightTextColor;
            lblSaveUnit.Location = new Point(265, 78);
            lblSaveUnit.AutoSize = true;
            panel1.Controls.Add(lblSaveUnit);

            // Interval 섹션
            label3.Text = "연결상태";
            label3.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            label3.ForeColor = lightTextColor;
            label3.Location = new Point(150, 55);

            // 상태 표시등 위치 조정
            pic_CS.Location = new Point(150, 75);
            pic_CS.Size = new Size(30, 30);
            pic_CS.SizeMode = PictureBoxSizeMode.Zoom;

            pic1.Location = new Point(180, 75);
            pic1.Size = new Size(30, 30);
            pic1.SizeMode = PictureBoxSizeMode.Zoom;

            // 🎯 DB 저장 상태 표시등 추가
            var pic_DB = new PictureBox();
            pic_DB.Name = "pic_DB";
            pic_DB.Location = new Point(300, 75);
            pic_DB.Size = new Size(20, 20);
            pic_DB.SizeMode = PictureBoxSizeMode.Zoom;
            panel1.Controls.Add(pic_DB);

            // 초기화 버튼 스타일
            button1.FlatStyle = FlatStyle.Flat;
            button1.FlatAppearance.BorderSize = 0;
            button1.BackColor = primaryColor;
            button1.ForeColor = Color.White;
            button1.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            button1.Size = new Size(80, 32);
            button1.Location = new Point(320, 15);
            button1.Cursor = Cursors.Hand;
            button1.Text = "리셋";

            // 버튼 호버 효과
            button1.MouseEnter += (s, e) =>
            {
                button1.BackColor = Color.FromArgb(41, 128, 185);
            };
            button1.MouseLeave += (s, e) =>
            {
                button1.BackColor = primaryColor;
            };

            // 데이터 항목 스타일링
            StyleDataLabels();

            // 구분선 그리기를 위한 이벤트
            panel1.Resize += (s, e) => panel1.Invalidate();
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 상단 색상 바
            using (SolidBrush brush = new SolidBrush(primaryColor))
            {
                g.FillRectangle(brush, 0, 0, panel1.Width, 4);
            }

            // 카드 효과 (둥근 모서리)
            Rectangle rect = new Rectangle(0, 0, panel1.Width - 1, panel1.Height - 1);
            using (GraphicsPath path = GetRoundedRectangle(rect, 8))
            {
                panel1.Region = new Region(path);
            }

            // 수평 구분선
            using (Pen pen = new Pen(Color.FromArgb(236, 240, 241), 1))
            {
                g.DrawLine(pen, 20, 110, panel1.Width - 20, 110);
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void StyleDataLabels()
        {
            // 항목 레이블 스타일
            Label[] itemLabels = { lbItem1, lbItem2, lbItem3, lbItem4, lbItem5, lbItem6, lbItem7 };
            Label[] cntLabels = { lbCnt1, lbCnt2, lbCnt3, lbCnt4, lbCnt5, lbCnt6, lbCnt7 };

            int startY = 130;
            int spacing = 35;
            int column1X = 20;
            int column2X = 220;

            for (int i = 0; i < itemLabels.Length; i++)
            {
                if (itemLabels[i] != null)
                {
                    int x = i < 4 ? column1X : column2X;
                    int y = startY + ((i % 4) * spacing);

                    itemLabels[i].Font = new Font("맑은 고딕", 15F, FontStyle.Regular);
                    itemLabels[i].ForeColor = lightTextColor;
                    itemLabels[i].Location = new Point(x, y);
                    itemLabels[i].AutoSize = true;

                    if (cntLabels[i] != null)
                    {
                        cntLabels[i].Font = new Font("맑은 고딕", 20F, FontStyle.Bold);
                        cntLabels[i].ForeColor = textColor;
                        cntLabels[i].Location = new Point(x + 80, y - 2);
                        cntLabels[i].AutoSize = true;
                        cntLabels[i].TextAlign = ContentAlignment.MiddleRight;
                        cntLabels[i].MinimumSize = new Size(80, 0);
                    }
                }
            }
        }

        public void Log(string msg)
        {
            try
            {
                string logstr = DateTime.Now.ToString("yyyyMMdd HHmmss") + " " + msg;
                string oFile = Path.Combine(Application.StartupPath, "log" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
                FileInfo f = new FileInfo(oFile);
                if (f.Exists)
                {
                    StreamWriter sw = f.AppendText();
                    sw.WriteLine(logstr);
                    sw.Close();
                }
                else
                {
                    StreamWriter sw = f.CreateText();
                    sw.WriteLine(logstr);
                    sw.Close();
                }

            }
            catch { }
        }

        public Image onImg;
        public Image offImg;
        public Image saveImg;
        public Image pendingImg;

        private void Form1_Load(object sender, EventArgs e)
        {
            // 현대적인 상태 아이콘 생성
            onImg = CreateStatusIcon(successColor);
            offImg = CreateStatusIcon(dangerColor);
            saveImg = CreateStatusIcon(successColor);
            pendingImg = CreateStatusIcon(warningColor);

            string[] ast = _setV.Replace("\r\n", "").Split(',');

            // 🎯 설정 문자열 파싱
            // 새 형식: IP:Port,Interval,StartAddress,DataLength,SlaveId,DeviceName#DeviceCode,Items,Mappings,SaveInterval
            // 기존 형식: IP:Port,Interval,StartAddress,DataLength,SlaveId,DeviceName#DeviceCode,Items,Mappings[,ExtraParam]

            if (ast.Length >= 9)
            {
                if (ast.Length == 9) // 기존 9개 파라미터 (저장주기 없음)
                {
                    Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                              ast[6].Split('/'), ast[7].Split('/'), ast[8].Split('/'));
                }
                else // 10개 파라미터 (저장주기 포함)
                {
                    string saveInterval = ast[9]; // 10번째 파라미터가 저장주기
                    Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                              ast[6].Split('/'), ast[7].Split('/'), ast[8].Split('/'), saveInterval);
                }
            }
            else if (ast.Length == 8) // 8개 파라미터 (더 이전 버전)
            {
                Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                          ast[6].Split('/'), ast[7].Split('/'));
            }

            Connect();
            getCnt();
        }

        private Image CreateStatusIcon(Color color)
        {
            Bitmap bmp = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // 외부 원 (연한 색)
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, color)))
                {
                    g.FillEllipse(brush, 2, 2, 16, 16);
                }

                // 내부 원 (진한 색)
                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 5, 5, 10, 10);
                }
            }
            return bmp;
        }

        public void Controller(string ip, string interval, string adrr, string length, string sid, string title = "", string[] aitem = null, string[] aMem = null, string[] aMem2 = null, string saveInterval = "60")
        {
            _adrr = Convert.ToUInt16(adrr);
            _sid = Convert.ToUInt16(sid);
            _length = Convert.ToUInt16(length);
            _aMem = aMem;
            _aMem2 = aMem2;
            _aitem = aitem;
            _ip = ip;
            modbusCtrl = new ModbusCtrl();
            lbItv.Text = interval; //초
            lbFaci.Text = title.Split('#')[0];
            _Faci = title.Split('#')[1];

            // 🎯 저장 주기 설정
            _saveInterval = Convert.ToInt32(saveInterval);

            // 저장주기가 수집주기보다 작으면 자동 조정
            int collectionInterval = Convert.ToInt32(interval);
            if (_saveInterval < collectionInterval)
            {
                _saveInterval = Math.Max(60, collectionInterval * 6);
            }

            // UI 업데이트
            var lbSaveItv = panel1.Controls.Find("lbSaveItv", false).FirstOrDefault() as Label;
            if (lbSaveItv != null)
            {
                lbSaveItv.Text = _saveInterval.ToString();
            }

            try
            {
                lbItem1.Text = aitem[0].Trim();
                lbItem2.Text = aitem[1].Trim();
                lbItem3.Text = aitem[2].Trim();
                lbItem4.Text = aitem[3].Trim();
                lbItem5.Text = aitem[4].Trim();
                lbItem6.Text = aitem[5].Trim();
                lbItem7.Text = aitem[6].Trim();

            }
            catch { }

            // 🎯 수집 타이머 설정
            timer1.Interval = collectionInterval * 1000;
            timer1.Enabled = true;

            // 🎯 저장 타이머 설정
            saveTimer.Interval = _saveInterval * 1000;
            saveTimer.Enabled = true;

            Log($"디바이스 '{lbFaci.Text}' 초기화 완료 - 수집주기: {interval}초, 저장주기: {_saveInterval}초");
        }

        bool bfirst = true;
        bool breset = false;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (breset == false && DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
            {
                modbusCtrl.WriteSingleRegister(1, 58, 0);
                breset = true;
            }
            if (breset && DateTime.Now.Hour == 23 && DateTime.Now.Minute == 59)
            {
                breset = false;
            }

            getCnt();
        }

        // 🎯 DB 저장 타이머 이벤트
        private void SaveTimer_Tick(object sender, EventArgs e)
        {
            SaveDataToDatabase();
        }

        ushort p = 0;
        bool bconnect = false;

        public void Connect()
        {
            try
            {
                Result Result;
                modbusCtrl.Mode = Mode.TCP_IP;
                modbusCtrl.ResponseTimeout = 1000;
                string[] ar = _ip.Split(':');
                Result = modbusCtrl.Connect(ar[0].Trim(), Convert.ToInt32(ar[1].Trim()));
                if (Result != Result.SUCCESS)
                {
                    this.pic_CS.BackgroundImage = offImg;
                    return;
                }
                this.pic_CS.BackgroundImage = onImg;
                if (bfirst)
                {
                    try
                    {
                        bfirst = false;
                    }
                    catch (Exception ex)
                    {
                        Log("DB error = " + ex.Message);
                    }
                }

                bconnect = true;

            }
            catch (Exception ex)
            {
                Log("Connect error = " + ex.Message);
            }
        }

        public void getCnt()
        {
            if (bconnect == false)
            {
                Connect();
                return;
            }
            getData(1);
        }

        private void getData(int gb)
        {
            try
            {
                Int16[] Registers = new Int16[_length];
                PictureBox pic = pic1;
                byte[] intBytes = BitConverter.GetBytes(_sid); //국번
                Result Result = Result.DEMO_TIMEOUT;
                string rdt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Result = modbusCtrl.ReadInputRegisters(intBytes[0], _adrr, _length, Registers);

                if (Result == Result.SUCCESS)
                {
                    pic.BackgroundImage = onImg;

                    // 🎯 데이터 버퍼 생성
                    var dataBuffer = new DataBuffer
                    {
                        Timestamp = DateTime.Now,
                        FacilityCode = _Faci
                    };

                    int p = 0;
                    foreach (string cv in _aMem)
                    {
                        ++p;
                        if (cv.Trim() == "")
                            continue;
                        string[] am = cv.Split('#');
                        int m = Convert.ToInt32(am[0]);
                        string ctype = am[1].ToUpper();
                        string cValue = "";
                        double numericValue = 0;

                        if (ctype == "B")
                        {
                            var value = modbusCtrl.RegisterToUInt16(Registers[m]);
                            cValue = string.Format("{0:D}", value);
                            numericValue = value;
                        }
                        else if (ctype == "W")
                        {
                            var value = modbusCtrl.RegistersToInt32(Registers[m + 1], Registers[m]);
                            cValue = string.Format("{0:D}", value);
                            numericValue = value;
                        }
                        else if (ctype.StartsWith("F"))
                        {
                            var value = modbusCtrl.RegistersToFloat(Registers[m + 1], Registers[m]);
                            cValue = string.Format("{0:" + ctype + "}", value);
                            numericValue = value;
                        }

                        // 🎯 실시간 UI 업데이트 (애니메이션 효과)
                        Label targetLabel = null;
                        if (p == 1) targetLabel = lbCnt1;
                        else if (p == 2) targetLabel = lbCnt2;
                        else if (p == 3) targetLabel = lbCnt3;
                        else if (p == 4) targetLabel = lbCnt4;
                        else if (p == 5) targetLabel = lbCnt5;
                        else if (p == 6) targetLabel = lbCnt6;
                        else if (p == 7) targetLabel = lbCnt7;

                        if (targetLabel != null && targetLabel.Text != cValue)
                        {
                            // 값이 변경될 때 색상 애니메이션
                            targetLabel.ForeColor = primaryColor;
                            targetLabel.Text = cValue;
                            Timer colorTimer = new Timer();
                            colorTimer.Interval = 500;
                            colorTimer.Tick += (s, args) =>
                            {
                                targetLabel.ForeColor = textColor;
                                colorTimer.Stop();
                                colorTimer.Dispose();
                            };
                            colorTimer.Start();
                        }

                        // 🎯 데이터 버퍼에 저장 (DB 저장은 별도 타이머에서)
                        dataBuffer.Values[$"Item{p}"] = numericValue;
                    }

                    // 🎯 데이터 버퍼에 추가
                    _dataBuffer.Add(dataBuffer);
                    _hasUnsavedData = true;

                    // 🎯 DB 저장 상태 표시 업데이트
                    UpdateDbSaveStatus();
                }
                else
                {
                    pic.BackgroundImage = offImg;
                    if (Result == Result.ISCLOSED)
                    {
                        this.pic_CS.BackgroundImage = offImg;
                        bconnect = false;
                    }
                    else
                        Log("Control error = " + Result);
                }
            }
            catch (Exception ex)
            {
                Log("getData error = " + ex.Message);
            }
        }

        // 🎯 DB 저장 상태 표시 업데이트
        private void UpdateDbSaveStatus()
        {
            var pic_DB = panel1.Controls.Find("pic_DB", false).FirstOrDefault() as PictureBox;
            if (pic_DB != null)
            {
                if (_hasUnsavedData)
                {
                    pic_DB.BackgroundImage = pendingImg;
                    // 툴팁으로 저장 대기 상태 알림
                    var tooltip = new ToolTip();
                    tooltip.SetToolTip(pic_DB, $"저장 대기 중... (다음 저장: {_saveInterval - (int)(DateTime.Now - _lastSaveTime).TotalSeconds}초 후)");
                }
                else
                {
                    pic_DB.BackgroundImage = saveImg;
                }
            }
        }

        // 🎯 데이터베이스 저장 메서드 (public으로 변경하여 MainForm에서 호출 가능)
        public void SaveDataToDatabase()
        {
            if (!_hasUnsavedData || _dataBuffer.Count == 0)
                return;

            try
            {
                Log($"DB 저장 시작 - {_dataBuffer.Count}개 데이터");

                foreach (var buffer in _dataBuffer)
                {
                    // 각 항목별로 저장
                    foreach (var kvp in buffer.Values)
                    {
                        var acquiredData = new AcquiredData(SessionService.Instance.UOW)
                        {
                            FacilityCode = buffer.FacilityCode,
                            NumericData = kvp.Value,
                            StringData = kvp.Key,
                            IPAddres = _ip,
                            CreatedDateTime = buffer.Timestamp
                        };
                    }
                }

                SessionService.Instance.InsertOrUpdate();

                // 버퍼 클리어
                _dataBuffer.Clear();
                _hasUnsavedData = false;
                _lastSaveTime = DateTime.Now;

                Log($"DB 저장 완료");

                // 상태 표시 업데이트
                UpdateDbSaveStatus();
            }
            catch (Exception ex)
            {
                Log("DB 저장 오류 = " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Result Result = modbusCtrl.WriteSingleRegister(1, 58, 0);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // 🎯 폼 종료 시 정리
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 남은 데이터 저장
            if (_hasUnsavedData)
            {
                SaveDataToDatabase();
            }

            // 타이머 정리
            timer1?.Stop();
            saveTimer?.Stop();
            timer1?.Dispose();
            saveTimer?.Dispose();

            base.OnFormClosing(e);
        }
    }
}