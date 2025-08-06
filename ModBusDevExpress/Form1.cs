using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using System;
using System.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using ColorBlend = System.Drawing.Drawing2D.ColorBlend;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Card;

namespace ModBusDevExpress
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        // 🚀 NEW: ReliableModBusService for HF2311S communication stability
        private ReliableModBusService _reliableModBusService;
        private Dictionary<int, ushort> _lastStableValues = new Dictionary<int, ushort>();  // 마지막 안정값 저장
        private ModbusDeviceSettings _deviceSettings;
        private DevExpress.XtraGrid.GridControl _gridControl;
        private DevExpress.XtraGrid.Views.Card.CardView _cardView;
        private System.Data.DataTable _dataTable;
        private int _deviceIndex = 0;  // 장치 순서 인덱스
        
        // 기존 변수들
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
        private Timer connectionMonitorTimer; // 🚀 NEW: 연결 상태 모니터링 타이머
        private int _saveInterval = 60;    // 저장 주기 (초)
        private DateTime _lastSaveTime = DateTime.MinValue;
        private DataBuffer _latestData = null;  // 🎯 최신 데이터만 저장 (버퍼링 제거)
        private bool _hasUnsavedData = false;  // 저장되지 않은 데이터 존재 여부
        private SaveMethod _saveMethod = SaveMethod.Periodic;  // 🎯 저장 방식
        private DataBuffer _previousData = null;  // 🎯 변화 감지용 이전 데이터
        
        // 상단 라이브 피드(스크롤)
        private ListBox liveFeedList;
        private const int LIVE_FEED_MAX = 100;
        private string _primaryItemName;
        // 레이아웃 상수(카드 배치용)
        private int _layoutTopMargin = 60;
        private int _layoutSpacing = 10;
        private int _layoutFeedHeight = 60;
        private int _layoutInfoHeight = 40;

        // 상단 상태 표시 (백엔드 데이터 유입 상태)
        private PictureBox _dataStatusPic;
        private System.Windows.Forms.Timer _statusTimer;
        private int _statusTimeoutSec = 5;
        private Image _statusBlueImg;
        private Image _statusRedImg;
        private Image _statusOrangeImg;
        private Image _statusGrayImg;
        private int _gaugeValueVerticalOffset = 0;
        // 자동 새로고침 비활성화 정책에 따라 관련 필드 제거/미사용
        private bool _appendFeedPerCollection = true;

        private void PositionStatusIcon()
        {
            if (_dataStatusPic == null || button1 == null) return;
            // 리셋 버튼 왼쪽 8px, 수직 중앙 정렬
            int x = Math.Max(10, button1.Left - _dataStatusPic.Width - 8);
            int y = button1.Top + (button1.Height - _dataStatusPic.Height) / 2;
            _dataStatusPic.Location = new Point(x, y);
        }

        // 디자인 색상
        private Color primaryColor = Color.FromArgb(99, 102, 241);     // 인디고 (현대적)
        private Color successColor = Color.FromArgb(16, 185, 129);     // 에메랄드
        private Color dangerColor = Color.FromArgb(239, 68, 68);       // 코랄 레드
        private Color warningColor = Color.FromArgb(245, 158, 11);     // 앰버
        private Color bgColor = Color.FromArgb(17, 24, 39);           // 다크 네이비
        private Color cardColor = Color.FromArgb(31, 41, 55);         // 다크 그레이
        private Color textColor = Color.FromArgb(243, 244, 246);      // 밝은 글자색
        private Color lightTextColor = Color.FromArgb(156, 163, 175); // 연한 글자색
        private Color accentColor = Color.FromArgb(236, 72, 153);     // 핑크 액센트

        // 🎯 데이터 버퍼 클래스
        private class DataBuffer
        {
            public DateTime Timestamp { get; set; }
            public string FacilityCode { get; set; }
            public Dictionary<string, double> Values { get; set; } = new Dictionary<string, double>();
        }

        public Form1(string setV) : this(setV, 0)
        {
        }
        
        public Form1(string setV, int deviceIndex)
        {
            _setV = setV;
            _deviceIndex = deviceIndex;
            InitializeComponent();
            ApplyModernDesign();
            InitializeSaveTimer();
            InitializeReliableModBusService(); // 🚀 NEW: ReliableModBusService 초기화
        }

        // 🎯 저장 타이머 초기화
        private void InitializeSaveTimer()
        {
            saveTimer = new Timer();
            saveTimer.Tick += SaveTimer_Tick;
            saveTimer.Enabled = false; // 연결 후 시작
        }

        // 🚀 NEW: ReliableModBusService 초기화
        private void InitializeReliableModBusService()
        {
            // UI는 데이터 이벤트만 구독하고 자체 폴링은 하지 않음
            connectionMonitorTimer = new Timer();
            connectionMonitorTimer.Interval = 10000; // 10초마다 상태 확인
            connectionMonitorTimer.Tick += ConnectionMonitorTimer_Tick;
            connectionMonitorTimer.Enabled = true;
        }

        // 🚀 NEW: 연결 상태 모니터링
        private void ConnectionMonitorTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_reliableModBusService != null)
                {
                    var stats = _reliableModBusService.GetStatistics();
                    UpdateConnectionStatus(stats);

                    // 🛡️ 워치독: 수집 중단 감지 시 가벼운 재시작 시도
                    double elapsedSec = (DateTime.Now - _lastDataTimestamp).TotalSeconds;
                    if ((!stats.IsConnected || elapsedSec > _statusTimeoutSec * 2) && _deviceSettings != null)
                    {
                        _ = _reliableModBusService.ConnectAsync();
                    }
                }
                else if (_deviceSettings != null)
                {
                    _ = InitializeReliableModBusServiceWithConfig($"{_deviceSettings.IPAddress}:{_deviceSettings.Port}", _deviceSettings.Interval);
                }
            }
            catch (ObjectDisposedException)
            {
                if (_deviceSettings != null)
                {
                    _ = InitializeReliableModBusServiceWithConfig($"{_deviceSettings.IPAddress}:{_deviceSettings.Port}", _deviceSettings.Interval);
                }
            }
            catch { }
        }

        // 🚀 NEW: 연결 상태 UI 업데이트 + 충돌 감지 및 자동 복구
        private ToolTip _dbToolTip;
        private DateTime _lastDataTimestamp = DateTime.MinValue;
        private ConnectionStatistics _lastStats;
        private volatile bool _autoRecovering = false;

        private void UpdateConnectionStatus(ConnectionStatistics stats)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ConnectionStatistics>(UpdateConnectionStatus), stats);
                return;
            }

            // 최소화된 모니터링: 상태는 내부 변수에만 기록, UI는 숫자/그래프만 갱신
            _lastStats = stats;
        }

        // 🚨 충돌 감지 및 복구 필요 여부 판단
        private bool DetectCollisionAndNeedsRecovery(ConnectionStatistics stats)
        {
            // 연결 품질이 매우 낮은 경우 (50 이하)
            if (stats.ConnectionQuality < 50)
            {
                Log($"⚠️ 연결 품질 저하 감지: {stats.ConnectionQuality}/100");
                return true;
            }
            
            // 최근 5분간 재연결이 3회 이상인 경우
            if (stats.TotalReconnects >= 3 && stats.ConnectionDuration.TotalMinutes < 5)
            {
                Log($"⚠️ 빈번한 재연결 감지: {stats.TotalReconnects}회 (지속시간: {FormatTimeSpan(stats.ConnectionDuration)})");
                return true;
            }
            
            // 오류율이 30% 이상인 경우
            int totalAttempts = stats.SuccessfulReads + stats.TotalErrors;
            if (totalAttempts > 10)
            {
                double errorRate = (double)stats.TotalErrors / totalAttempts * 100;
                if (errorRate >= 30)
                {
                    Log($"⚠️ 높은 오류율 감지: {errorRate:F1}% ({stats.TotalErrors}/{totalAttempts})");
                    return true;
                }
            }
            
            return false;
        }

        // 🔧 자동 복구 수행
        private async System.Threading.Tasks.Task PerformAutoRecovery()
        {
            if (_autoRecovering) return;
            _autoRecovering = true;
            Log("🔧 자동 복구 시작...");
            
            try
            {
                // 1. 타이머 일시 정지
                if (timer1.Enabled)
                {
                    timer1.Enabled = false;
                    Log("⏸️ 데이터 수집 타이머 일시 정지");
                }
                
                // 2. ReliableModBusService 재초기화
                if (_reliableModBusService != null)
                {
                    Log("🔄 ReliableModBusService 재초기화 중...");
                    _reliableModBusService.Dispose();
                    await System.Threading.Tasks.Task.Delay(1000); // 1초 대기
                }
                
                // 3. 새로운 타이머 오프셋으로 재시작
                if (_deviceSettings != null)
                {
                    await InitializeReliableModBusServiceWithConfig($"{_deviceSettings.IPAddress}:{_deviceSettings.Port}", _deviceSettings.Interval);
                    
                    // 새로운 오프셋으로 타이머 재설정
                    int newOffset = CalculateTimerOffset($"{_deviceSettings.IPAddress}:{_deviceSettings.Port}") + 500; // 추가 500ms 지연
                    timer1.Interval = _deviceSettings.Interval * 1000 + newOffset;
                    timer1.Enabled = true;
                    
                    Log($"✅ 자동 복구 완료 - 새 타이머 간격: {timer1.Interval}ms");
                }
                else
                {
                    Log("❌ 자동 복구 실패: 디바이스 설정이 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 자동 복구 중 오류: {ex.Message}");
                
                // 최소한 타이머라도 재시작
                timer1.Enabled = true;
            }
            finally
            {
                _autoRecovering = false;
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}일 {timeSpan.Hours}시간";
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}시간 {timeSpan.Minutes}분";
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}분 {timeSpan.Seconds}초";
            return $"{timeSpan.Seconds}초";
        }

        private void ApplyModernDesign()
        {
            // 폼 스타일
            this.BackColor = bgColor;  // 다크 네이비
            this.FormBorderStyle = FormBorderStyle.None;
            this.FormBorderEffect = DevExpress.XtraEditors.FormBorderEffect.Shadow;

            // 패널 스타일
            panel1.BackColor = bgColor;  // 다크 네이비
            panel1.Dock = DockStyle.Fill; // 데이터 표시 영역과 레이아웃 크기를 일치
            panel1.Paint += Panel1_Paint;  // Paint 이벤트 활성화

            // 설비명 레이블 - 대시보드 스타일
            lbFaci.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            lbFaci.ForeColor = Color.White;  // 흰색
            lbFaci.Location = new Point(60, 20);
            
            // 로고 아이콘 추가
            PictureBox logoIcon = new PictureBox();
            logoIcon.Name = "logoIcon";
            logoIcon.Size = new Size(32, 32);
            logoIcon.Location = new Point(20, 18);
            logoIcon.SizeMode = PictureBoxSizeMode.Zoom;
            logoIcon.Image = CreateLogoIcon();
            panel1.Controls.Add(logoIcon);

            // 연결 상태 관련 UI 제거
            label3.Visible = false;
            pic_CS.Visible = false;
            pic1.Visible = false;
            
            // 기존 수집주기 등은 하단 정보바로 이동하므로 숨김
            label1.Visible = false;
            lbItv.Visible = false;
            label2.Visible = false;

            // 저장주기 라벨도 화면에 배치하지 않음

            // 초기화 버튼 스타일
            // 초기화 버튼 스타일 - 현대적 디자인
            button1.FlatStyle = FlatStyle.Flat;
            button1.FlatAppearance.BorderSize = 1;
            button1.FlatAppearance.BorderColor = primaryColor;
            button1.BackColor = Color.Transparent;
            button1.ForeColor = primaryColor;
            button1.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            button1.Size = new Size(80, 32);
            button1.Location = new Point(panel1.Width - 100, 20);
            button1.Cursor = Cursors.Hand;
            button1.Text = "리셋";
            button1.Resize += (s, e) => PositionStatusIcon();
            button1.LocationChanged += (s, e) => PositionStatusIcon();

            // 버튼 둥근 모서리
            button1.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                Rectangle rect = new Rectangle(0, 0, button1.Width - 1, button1.Height - 1);
                using (GraphicsPath path = GetRoundedRectangle(rect, 8))
                {
                    button1.Region = new Region(path);
                    
                    // 호버 상태에 따른 배경
                    if (button1.ClientRectangle.Contains(button1.PointToClient(Cursor.Position)))
                    {
                        using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                            rect, primaryColor, Color.FromArgb(139, 92, 246), 45F))
                        {
                            g.FillPath(bgBrush, path);
                        }
                        button1.ForeColor = Color.White;
                    }
                    else
                    {
                        using (Pen borderPen = new Pen(primaryColor, 1))
                        {
                            g.DrawPath(borderPen, path);
                        }
                        button1.ForeColor = primaryColor;
                    }
                }
            };

            // 버튼 호버 효과
            button1.MouseEnter += (s, e) => button1.Invalidate();
            button1.MouseLeave += (s, e) => button1.Invalidate();

            // 데이터 항목 스타일링 - panel1이 초기화된 후에 호출
            if (panel1 != null)
            {
            StyleDataLabels();
            }
            else
            {
                Log("⚠️ Form1_Load: panel1이 null입니다.");
            }

            // 구분선 그리기를 위한 이벤트
            panel1.Resize += (s, e) => panel1.Invalidate();

            // 수집 안정 표시등은 제거 (숫자/그래프만 유지)
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 패널 배경 - 다크 네이비
            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, 0, 0, panel1.Width, panel1.Height);
            }
            
            // 상단 헤더 영역 그라디언트
            Rectangle headerRect = new Rectangle(0, 0, panel1.Width, 110);
            using (LinearGradientBrush headerBrush = new LinearGradientBrush(
                headerRect,
                Color.FromArgb(31, 41, 55),  // 다크 그레이
                bgColor,  // 다크 네이비
                90F))
            {
                g.FillRectangle(headerBrush, headerRect);
            }

            // 상단 구분선 제거 (요청사항)
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
            try
            {
                // panel1이 초기화되었는지 확인
                if (panel1 == null)
                {
                    Log("⚠️ StyleDataLabels: panel1이 아직 초기화되지 않았습니다.");
                    return;
                }
                
                // 🎨 대시보드 UI 생성
                CreateDashboardUI();
            }
            catch (Exception ex)
            {
                Log($"🚨 StyleDataLabels 오류: {ex.Message}");
            }
        }
        
                // 🎨 대시보드 UI 생성
        private void CreateDashboardUI()
        {
            // 기존 라벨 숨기기
            Label[] itemLabels = { lbItem1, lbItem2, lbItem3, lbItem4, lbItem5, lbItem6, lbItem7 };
            Label[] cntLabels = { lbCnt1, lbCnt2, lbCnt3, lbCnt4, lbCnt5, lbCnt6, lbCnt7 };

            foreach (var label in itemLabels.Concat(cntLabels))
            {
                if (label != null) label.Visible = false;
            }
            
            // 배치 기준값 (필드 상수 사용)
            int topMargin = _layoutTopMargin;
            int spacing = _layoutSpacing;
            int feedHeight = _layoutFeedHeight;   // 하단 스크롤 영역 높이
            int infoHeight = _layoutInfoHeight;   // 하단 정보바 높이

            // 하단 라이브 피드(스크롤) - 수집주기 기준으로 표시 (정보 라인 위에 위치)
            liveFeedList = new ListBox();
            liveFeedList.Name = "liveFeedList";
            liveFeedList.Size = new Size(panel1.Width - 40, feedHeight);
            liveFeedList.Location = new Point(20, panel1.Height - feedHeight - spacing - infoHeight - spacing);
            liveFeedList.ForeColor = lightTextColor;
            liveFeedList.BackColor = Color.FromArgb(20, 20, 20);
            liveFeedList.Font = new Font("Consolas", 9F);
            liveFeedList.BorderStyle = BorderStyle.None;
            liveFeedList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            liveFeedList.Visible = true;
            panel1.Controls.Add(liveFeedList);

            // 메인 대시보드 컨테이너 (게이지/차트)
            Panel dashboardPanel = new Panel();
            dashboardPanel.Name = "dashboardPanel";
            dashboardPanel.Location = new Point(20, topMargin);
            int effectiveFeedHeight = liveFeedList.Visible ? feedHeight : 0; // 초기부터 피드 높이 반영
            dashboardPanel.Size = new Size(
                panel1.Width - 40,
                panel1.Height - topMargin - infoHeight - effectiveFeedHeight - spacing * 2);
            dashboardPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dashboardPanel.BackColor = Color.Transparent;
            panel1.Controls.Add(dashboardPanel);

            // 1. 게이지 영역 (왼쪽, 같은 라인)
            Panel gaugePanel = new Panel();
            gaugePanel.Name = "gaugePanel";
            int middleHeight = 90; // 라인 높이 고정
            int hSpacing = 10;     // 두 패널 사이 간격
            int leftWidth = (dashboardPanel.Width - hSpacing) / 2;
            gaugePanel.Location = new Point(0, 0);
            gaugePanel.Size = new Size(leftWidth, middleHeight);
            gaugePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            gaugePanel.BackColor = cardColor;
            dashboardPanel.Controls.Add(gaugePanel);

            // 2. 차트 영역 (오른쪽, 같은 라인)
            Panel chartPanel = new Panel();
            chartPanel.Name = "chartPanel";
            chartPanel.Location = new Point(leftWidth + hSpacing, 0);
            chartPanel.Size = new Size(dashboardPanel.Width - leftWidth - hSpacing, middleHeight);
            chartPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            chartPanel.BackColor = cardColor;
            dashboardPanel.Controls.Add(chartPanel);
            
            // 게이지 패널 디자인
            CreateGaugeControl(gaugePanel);
            
            // 차트 패널 디자인
            CreateChartControl(chartPanel);
            
            // 상단 상태 표시 아이콘
            _dataStatusPic = new PictureBox();
            _dataStatusPic.Name = "dataStatusPic";
            _dataStatusPic.Size = new Size(32, 32);
            _dataStatusPic.SizeMode = PictureBoxSizeMode.Zoom;
            _dataStatusPic.BackColor = Color.Transparent; // 카드/헤더와 시각적 이질감 제거
            _statusBlueImg = CreateStatusIcon(primaryColor);
            _statusRedImg = CreateStatusIcon(dangerColor);
            _statusOrangeImg = CreateStatusIcon(warningColor);
            _statusGrayImg = CreateStatusIcon(Color.Gray);
            _dataStatusPic.Image = _statusRedImg; // 초기: 없음(빨강)
            panel1.Controls.Add(_dataStatusPic);
            _dataStatusPic.BringToFront();
            PositionStatusIcon();

            // 상태 타이머: 최근 수신 시각 기준으로 색상 전환
            // 상태 타이머: 수집주기 기반 미수신 횟수에 따라 자동 복구 트리거
            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 1000;
            _statusTimer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - _lastDataTimestamp).TotalSeconds;
                if (elapsed > _statusTimeoutSec * 2)
                {
                    // 3회 이상 미수신 시에도 자동 새로고침 수행하지 않음 (요청에 따라 비활성화)
                    _dataStatusPic.Image = _statusRedImg;
                }
            };
            _statusTimer.Start();

            // 3. 하단 정보 영역 (맨 아래) - 높이 보장
            Panel infoPanel = new Panel();
            infoPanel.Name = "infoPanel";
            infoPanel.Size = new Size(panel1.Width - 40, infoHeight);
            infoPanel.Location = new Point(20, panel1.Height - infoHeight - spacing);
            infoPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            infoPanel.BackColor = Color.FromArgb(31, 41, 55);
            panel1.Controls.Add(infoPanel);
            
            CreateInfoBar(infoPanel);
            
            // 둥근 모서리 적용
            ApplyRoundedCorners(gaugePanel, 12);
            ApplyRoundedCorners(chartPanel, 12);

            // 레이아웃 재계산: 패널 리사이즈 시 내부 컨트롤 크기 유지
            panel1.Resize += (s, e) =>
            {
                // 대시보드 영역 재계산
                int effectiveFeed = liveFeedList.Visible ? feedHeight : 0;
                dashboardPanel.Size = new Size(
                    panel1.Width - 40,
                    panel1.Height - topMargin - infoHeight - effectiveFeed - spacing * 2);
                // 하단 영역 재배치 (정보 라인을 항상 맨 아래, 피드는 그 위)
                infoPanel.Size = new Size(panel1.Width - 40, infoHeight);
                infoPanel.Location = new Point(20, panel1.Height - infoHeight - spacing);
                liveFeedList.Size = new Size(panel1.Width - 40, feedHeight);
                liveFeedList.Location = new Point(20, panel1.Height - feedHeight - spacing - infoHeight - spacing);
                // 상태 아이콘 위치 보정 (설비명 좌측)
                PositionStatusIcon();
                // 같은 라인 유지: 왼쪽/오른쪽 균등 분배 (높이 90 고정)
                int mh = 90;
                int hs = 10;
                int lw = (dashboardPanel.Width - hs) / 2;
                gaugePanel.Size = new Size(lw, mh);
                gaugePanel.Location = new Point(0, 0);
                chartPanel.Location = new Point(lw + hs, 0);
                chartPanel.Size = new Size(dashboardPanel.Width - lw - hs, mh);
            };
        }
        
        // 🎯 게이지 컨트롤 생성
        private void CreateGaugeControl(Panel gaugePanel)
        {
            // 현재 값 표시 라벨
            Label valueLabel = new Label();
            valueLabel.Name = "gaugeValueLabel";
            valueLabel.Font = new Font("맑은 고딕", 48F, FontStyle.Bold);
            valueLabel.ForeColor = Color.White;
            valueLabel.Size = new Size(gaugePanel.Width, gaugePanel.Height);
            valueLabel.Location = new Point(0, 0);
            valueLabel.Text = "0";
            valueLabel.TextAlign = ContentAlignment.MiddleCenter;
            gaugePanel.Controls.Add(valueLabel);
            
            // 단위 라벨 제거 (데이터 밑에 텍스트 필요 없음)
            
            // 게이지 그리기
            gaugePanel.Paint += (sender, e) =>
            {
                DrawGauge(e.Graphics, gaugePanel.ClientRectangle);
            };
            gaugePanel.Resize += (s, e) =>
            {
                // 숫자 라벨이 항상 상하 중앙을 차지하도록 전체 채움 유지
                var lbl = gaugePanel.Controls.Find("gaugeValueLabel", false).FirstOrDefault() as Label;
                if (lbl != null)
                {
                    lbl.Size = new Size(gaugePanel.Width, gaugePanel.Height);
                    lbl.Location = new Point(0, 0);
                }
            };
        }
        
        // 🎯 차트 컨트롤 생성
        private void CreateChartControl(Panel chartPanel)
        {
            // 차트 영역 (제목 없이)
            Panel chartArea = new Panel();
            chartArea.Name = "chartArea";
            chartArea.Location = new Point(10, 10);
            chartArea.Size = new Size(chartPanel.Width - 20, chartPanel.Height - 20);
            chartArea.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chartArea.BackColor = Color.FromArgb(20, 20, 20);
            chartPanel.Controls.Add(chartArea);
            
            chartArea.Paint += (sender, e) =>
            {
                DrawChart(e.Graphics, chartArea.ClientRectangle);
            };
        }
        
        // 🎯 하단 정보바 생성
        private void CreateInfoBar(Panel infoPanel)
        {
            int itemWidth = infoPanel.Width / 3;
            
            // 수집 주기
            Label collectLabel = new Label();
            collectLabel.Name = "collectLabel";
            collectLabel.Font = new Font("맑은 고딕", 9F);
            collectLabel.ForeColor = lightTextColor;
            collectLabel.Text = $"수집: {(!string.IsNullOrEmpty(lbItv.Text) ? lbItv.Text : "10")}초";
            collectLabel.Size = new Size(itemWidth, 30);
            collectLabel.Location = new Point(0, 5);
            collectLabel.TextAlign = ContentAlignment.MiddleCenter;
            infoPanel.Controls.Add(collectLabel);
            
            // 저장 주기
            Label saveLabel = new Label();
            saveLabel.Name = "saveLabel";
            saveLabel.Font = new Font("맑은 고딕", 9F);
            saveLabel.ForeColor = lightTextColor;
            saveLabel.Text = $"저장: {_saveInterval}초";
            saveLabel.Size = new Size(itemWidth, 30);
            saveLabel.Location = new Point(itemWidth, 5);
            saveLabel.TextAlign = ContentAlignment.MiddleCenter;
            infoPanel.Controls.Add(saveLabel);
            
            // 가동 시간
            Label uptimeLabel = new Label();
            uptimeLabel.Name = "uptimeLabel";
            uptimeLabel.Font = new Font("맑은 고딕", 9F);
            uptimeLabel.ForeColor = lightTextColor;
            uptimeLabel.Text = "가동: 0분";
            uptimeLabel.Size = new Size(itemWidth, 30);
            uptimeLabel.Location = new Point(itemWidth * 2, 5);
            uptimeLabel.TextAlign = ContentAlignment.MiddleCenter;
            infoPanel.Controls.Add(uptimeLabel);
        }
        
        // 둥근 모서리 적용
        private void ApplyRoundedCorners(Panel panel, int radius)
        {
            panel.Paint += (sender, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using (GraphicsPath path = GetRoundedRectangle(rect, radius))
                {
                    panel.Region = new Region(path);
                    
                    // 배경 그라디언트
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(31, 41, 55),
                        cardColor,
                        90F))
                    {
                        g.FillPath(brush, path);
                    }
                    
                    // 테두리
                    using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 1))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            };
        }
        
        // 기존 카드 생성 코드 제거
        /*
        for (int i = 0; i < cardCount; i++)
        {
            if (itemLabels[i] != null && cntLabels[i] != null)
            {
                    // 람다 캡처를 위한 로컬 변수
                    int index = i;
                    string currentItemName = _aitem != null && i < _aitem.Length ? _aitem[i] : "항목";
                    
                    // 카드 패널 생성
                    Panel cardPanel = new Panel();
                    cardPanel.Name = $"card{i + 1}";
                    cardPanel.Size = new Size(cardWidth, cardHeight);
                    cardPanel.Location = new Point(startX + (i % 2) * (cardWidth + 20), 
                                                 startY + (i / 2) * (cardHeight + cardSpacing));
                    cardPanel.BackColor = cardColor;  // 다크 그레이 배경
                    cardPanel.BorderStyle = BorderStyle.None;  // 테두리 제거
                    
                    // 둥근 모서리와 그라디언트 효과를 위한 Paint 이벤트
                    cardPanel.Paint += (sender, e) =>
                    {
                        Graphics g = e.Graphics;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        
                        // 둥근 모서리 카드
                        Rectangle rect = new Rectangle(0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
                        using (GraphicsPath path = GetRoundedRectangle(rect, 12))
                        {
                            // 항목에 따른 다른 그라디언트 색상
                            Color startColor, endColor;
                            string itemName = currentItemName.ToLower();
                            
                            if (itemName.Contains("전력") || itemName.Contains("kw") || itemName.Contains("전압"))
                            {
                                // 전력계 - 파란색 계열
                                startColor = Color.FromArgb(99, 102, 241);   // 인디고
                                endColor = Color.FromArgb(139, 92, 246);     // 보라색
                            }
                            else if (itemName.Contains("카운") || itemName.Contains("count"))
                            {
                                // 카운터 - 에메랄드 계열
                                startColor = Color.FromArgb(16, 185, 129);   // 에메랄드
                                endColor = Color.FromArgb(5, 150, 105);      // 진한 에메랄드
                            }
                            else
                            {
                                // 기타 - 핑크 계열
                                startColor = Color.FromArgb(236, 72, 153);   // 핑크
                                endColor = Color.FromArgb(219, 39, 119);     // 진한 핑크
                            }
                            
                            // 그라디언트 배경
                            using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                                rect, startColor, endColor, 45F))
                            {
                                g.FillPath(gradientBrush, path);
                            }
                            
                            // 부드러운 그림자 효과
                            using (Pen shadowPen = new Pen(Color.FromArgb(10, 0, 0, 0), 2))
                            {
                                g.DrawPath(shadowPen, path);
                            }
                            
                            // 반사광 효과 (상단)
                            Rectangle glossRect = new Rectangle(0, 0, rect.Width, rect.Height / 2);
                            using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                                glossRect,
                                Color.FromArgb(30, 255, 255, 255),
                                Color.FromArgb(5, 255, 255, 255),
                                90F))
                            {
                                using (GraphicsPath glossPath = new GraphicsPath())
                                {
                                    glossPath.AddArc(rect.X, rect.Y, 12, 12, 180, 90);
                                    glossPath.AddArc(rect.Right - 12, rect.Y, 12, 12, 270, 90);
                                    glossPath.AddLine(rect.Right, rect.Y + rect.Height / 2, rect.X, rect.Y + rect.Height / 2);
                                    glossPath.CloseFigure();
                                    g.FillPath(glossBrush, glossPath);
                                }
                            }
                        }
                    };
                    
                    // 항목명을 작게 상단에 표시
                    Label headerLabel = new Label();
                    headerLabel.Text = currentItemName;
                    headerLabel.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
                    headerLabel.ForeColor = Color.FromArgb(200, 200, 200);  // 연한 회색
                    headerLabel.Size = new Size(cardWidth, 20);
                    headerLabel.Location = new Point(0, 5);
                    headerLabel.TextAlign = ContentAlignment.TopCenter;
                    cardPanel.Controls.Add(headerLabel);
                    
                    // 값 라벨 (카드 완전 정중앙)
                    cntLabels[i].Font = new Font("맑은 고딕", 32F, FontStyle.Bold);  // 더 큰 글자
                    cntLabels[i].ForeColor = Color.White;  // 흰색
                    cntLabels[i].AutoSize = false;  // 크기 고정
                    cntLabels[i].Size = new Size(cardWidth, cardHeight);  // 전체 카드 크기 사용
                    cntLabels[i].Location = new Point(0, 0);  // 카드 전체를 덮음
                    cntLabels[i].TextAlign = ContentAlignment.MiddleCenter;  // 완벽한 중앙 정렬
                    cntLabels[i].Parent = cardPanel;
                    cntLabels[i].Visible = true;
                    cntLabels[i].BringToFront();  // 값을 최상위로
                    
                    // 항목 라벨은 숨김 (헤더에 포함됨)
                    itemLabels[i].Visible = false;
                    
                    panel1.Controls.Add(cardPanel);
                }
            }
            
        */
        
        // 🎯 게이지 그리기
        private void DrawGauge(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 최소 크기 가드: 너무 작은 영역이면 그림 생략
            if (rect.Width < 20 || rect.Height < 20)
            {
                return;
            }

            int centerX = rect.Width / 2;
            int centerY = rect.Height / 2 - 30;
            int minDim = Math.Min(rect.Width, rect.Height);
            int radius = Math.Max(minDim / 3, 10); // 최소 반지름 보장
            
            // 외부 원
            using (Pen outerPen = new Pen(Color.FromArgb(60, 60, 60), 3))
            {
                g.DrawArc(outerPen, centerX - radius, centerY - radius, radius * 2, radius * 2, 135, 270);
            }
            
            // 게이지 색상 그라디언트
            using (LinearGradientBrush gaugeBrush = new LinearGradientBrush(
                new Rectangle(centerX - radius, centerY - radius, radius * 2, radius * 2),
                successColor,
                primaryColor,
                45F))
            {
                using (Pen gaugePen = new Pen(gaugeBrush, 12))
                {
                    // 🎯 현재 값에 따른 각도 계산 (동적 스케일링)
                    float currentValue = GetCurrentValue();
                    float maxValue = Math.Max(_chartData.Count > 0 ? _chartData.Max() : 10f, 10f); // 차트 데이터 기준 또는 최소 10
                    float percentage = Math.Max(0f, Math.Min(currentValue / maxValue, 1f));
                    float sweepAngle = Math.Max(0f, Math.Min(270f * percentage, 270f));
                    int innerRadius = Math.Max(radius - 6, 1); // 음수/0 치수 방지
                    
                    g.DrawArc(gaugePen, centerX - innerRadius, centerY - innerRadius, 
                             innerRadius * 2, innerRadius * 2, 135, sweepAngle);
                }
            }
            
            // 중심점 표시
            using (SolidBrush centerBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillEllipse(centerBrush, centerX - 5, centerY - 5, 10, 10);
            }
        }
        
        // 🎯 차트 그리기
        private void DrawChart(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 차트 배경
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 20, 20)))
            {
                g.FillRectangle(bgBrush, rect);
            }
            
            // 그리드 라인
            using (Pen gridPen = new Pen(Color.FromArgb(40, 40, 40), 1))
            {
                // 수평선
                for (int i = 1; i < 5; i++)
                {
                    int y = rect.Top + (rect.Height / 5 * i);
                    g.DrawLine(gridPen, rect.Left, y, rect.Right, y);
                }
                
                // 수직선
                for (int i = 1; i < 10; i++)
                {
                    int x = rect.Left + (rect.Width / 10 * i);
                    g.DrawLine(gridPen, x, rect.Top, x, rect.Bottom);
                }
            }
            
            // 차트 데이터 (예시)
            var dataPoints = GetChartData();
            if (dataPoints != null && dataPoints.Count > 1)
            {
                using (Pen chartPen = new Pen(primaryColor, 2))
                {
                    Point[] points = new Point[dataPoints.Count];
                    // 🎯 동적 스케일링: 최대값 자동 계산
                    float maxValue = Math.Max(dataPoints.Max(), 10f); // 최소 10 이상으로 설정
                    
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        int x = rect.Left + (rect.Width / (dataPoints.Count - 1) * i);
                        int y = rect.Bottom - (int)(rect.Height * dataPoints[i] / maxValue); // 동적 스케일링
                        points[i] = new Point(x, y);
                    }
                    
                    if (points.Length > 1)
                    {
                        g.DrawLines(chartPen, points);
                    }
                    
                    // 데이터 포인트 표시
                    using (SolidBrush pointBrush = new SolidBrush(primaryColor))
                    {
                        foreach (var point in points)
                        {
                            g.FillEllipse(pointBrush, point.X - 3, point.Y - 3, 6, 6);
                        }
                    }
                }
            }
        }
        
        // 현재 값 가져오기
        private float GetCurrentValue()
        {
            var panel = panel1.Controls.Find("gaugeValueLabel", true).FirstOrDefault() as Label;
            if (panel != null && float.TryParse(panel.Text.Replace("W", "").Replace("A", "").Trim(), out float value))
            {
                return value;
            }
            return 0f;
        }
        
        // 차트 데이터 가져오기 (최근 10개 데이터)
        private List<float> _chartData = new List<float>();
        private DateTime _startTime = DateTime.Now;
        
        private List<float> GetChartData()
        {
            return _chartData;
        }
        
        // 🎨 대시보드 UI 업데이트
        private void UpdateDashboardUI(string itemName, double value, string displayValue)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string, double, string>(UpdateDashboardUI), itemName, value, displayValue);
                return;
            }
            
            // 게이지 값 업데이트
            var gaugeValueLabel = panel1.Controls.Find("gaugeValueLabel", true).FirstOrDefault() as Label;
            if (gaugeValueLabel != null)
            {
                gaugeValueLabel.Text = displayValue;
            }
            
            // 차트 데이터 추가 (대표 항목만 기록하여 메모리/그림 최소화)
            if (string.IsNullOrEmpty(_primaryItemName) || itemName == _primaryItemName)
            {
                _chartData.Add((float)value);
            }
            if (_chartData.Count > 60) // 최근 60개 데이터만 유지 (10분 @ 10초 간격)
            {
                _chartData.RemoveAt(0);
            }
            
            // 차트 영역 다시 그리기
            var chartArea = panel1.Controls.Find("chartArea", true).FirstOrDefault();
            if (chartArea != null)
            {
                chartArea.Invalidate();
            }
            
            // 게이지 패널 다시 그리기
            var gaugePanel = panel1.Controls.Find("gaugePanel", true).FirstOrDefault();
            if (gaugePanel != null)
            {
                gaugePanel.Invalidate();
            }
            
            // 가동 시간 업데이트
            var uptimeLabel = panel1.Controls.Find("uptimeLabel", true).FirstOrDefault() as Label;
            if (uptimeLabel != null)
            {
                var uptime = DateTime.Now - _startTime;
                uptimeLabel.Text = $"가동: {(int)uptime.TotalMinutes}분";
            }

            // 하단 라이브 피드 갱신 (수집주기 당 1줄, 대표 항목 기준)
            if (liveFeedList != null)
            {
                // 첫 수신 항목을 대표 항목으로 고정
                if (string.IsNullOrEmpty(_primaryItemName))
                {
                    _primaryItemName = itemName;
                }

                if (itemName == _primaryItemName)
                {
                    string line = $"{DateTime.Now:HH:mm:ss} | {itemName,-12} | {displayValue,8}";
                    // 최근 라인이 같은 수집tick에서 이미 추가되었는지 간단 체크(시간 초 정밀도 기준)
                    if (liveFeedList.Items.Count == 0 || !((string)liveFeedList.Items[liveFeedList.Items.Count - 1]).StartsWith(DateTime.Now.ToString("HH:mm:ss")))
                    {
                        liveFeedList.Items.Add(line);
                        while (liveFeedList.Items.Count > LIVE_FEED_MAX)
                        {
                            liveFeedList.Items.RemoveAt(0);
                        }
                        liveFeedList.TopIndex = liveFeedList.Items.Count - 1;
                    }
                }
            }
        }
        
        // 🎯 DevExpress CardView 생성
        private void CreateCardView()
        {
            try
            {
                // GridControl 생성 - 전체 Form 크기 활용
                var gridControl = new DevExpress.XtraGrid.GridControl();
                gridControl.Name = "gridControlData";
                gridControl.Location = new Point(10, 120);
                gridControl.Size = new Size(panel1.Width - 20, panel1.Height - 130);
                gridControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                
                // CardView 생성
                var cardView = new DevExpress.XtraGrid.Views.Card.CardView(gridControl);
                gridControl.MainView = cardView;
                gridControl.ViewCollection.Add(cardView);
            
            // CardView 설정 - 카드 크기 최대화
            cardView.CardCaptionFormat = "{0}. {1}";  // 번호. 항목명 형식
            cardView.CardInterval = 10;
            cardView.CardWidth = 300;  // 카드 너비 증가
            cardView.MaximumCardColumns = 2;  // 2열 유지
            cardView.FocusedCardTopFieldIndex = 0;
            cardView.OptionsView.ShowCardCaption = true;
            cardView.OptionsView.ShowQuickCustomizeButton = false;
            cardView.OptionsView.ShowFieldHints = false;
            cardView.OptionsView.ShowLines = false;
            cardView.OptionsBehavior.Editable = false;
            cardView.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            
            // 카드 스타일 설정
            cardView.Appearance.Card.BackColor = Color.White;
            cardView.Appearance.Card.BorderColor = Color.FromArgb(220, 220, 220);
            cardView.Appearance.Card.Options.UseBackColor = true;
            cardView.Appearance.Card.Options.UseBorderColor = true;
            
            // 카드 캡션 (상단) - 번호와 항목명
            cardView.Appearance.CardCaption.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            cardView.Appearance.CardCaption.ForeColor = textColor;
            cardView.Appearance.CardCaption.Options.UseFont = true;
            cardView.Appearance.CardCaption.Options.UseForeColor = true;
            cardView.Appearance.CardCaption.Options.UseTextOptions = true;
            cardView.Appearance.CardCaption.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            
            // 카드 값 표시 - 크게
            cardView.Appearance.FieldValue.Font = new Font("맑은 고딕", 24F, FontStyle.Bold);
            cardView.Appearance.FieldValue.ForeColor = primaryColor;
            cardView.Appearance.FieldValue.Options.UseFont = true;
            cardView.Appearance.FieldValue.Options.UseForeColor = true;
            cardView.Appearance.FieldValue.Options.UseTextOptions = true;
            cardView.Appearance.FieldValue.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            
            // 필드 캡션 숨기기
            cardView.Appearance.FieldCaption.ForeColor = Color.Transparent;
            cardView.Appearance.FieldCaption.Options.UseForeColor = true;
            
            // 데이터 소스 설정
            _dataTable = new System.Data.DataTable();
            _dataTable.Columns.Add("항목", typeof(string));
            _dataTable.Columns.Add("값", typeof(string));
            
            gridControl.DataSource = _dataTable;
            
            // panel1이 null인지 확인
            if (panel1 == null)
            {
                Log("⚠️ panel1이 초기화되지 않았습니다.");
                return;
            }
            
            // GridControl을 panel1에 직접 추가
            panel1.Controls.Add(gridControl);
            _gridControl = gridControl;
            _cardView = cardView;
            
            // 컬럼이 생성된 후에 설정
            cardView.PopulateColumns();
            
            if (cardView.Columns.Count > 0)
            {
                cardView.Columns["항목"].Visible = false;  // 캡션으로 표시
                if (cardView.Columns["값"] != null)
                {
                    cardView.Columns["값"].Caption = "";
                }
            }
            }
            catch (Exception ex)
            {
                Log($"🚨 CreateCardView 오류: {ex.Message}");
                Log($"스택 트레이스: {ex.StackTrace}");
            }
        }
        
        // 🎯 상단 패널로 컨트롤 이동
        private void MoveControlsToTopPanel(Panel topPanel)
        {
            // 상단에 표시되어야 할 컨트롤들
            var controlsToMove = new List<Control>();
            
            foreach (Control control in panel1.Controls)
            {
                if (control.Name == "lbFaci" || 
                    control.Name == "label1" || 
                    control.Name == "lbItv" ||
                    control.Name == "lbSaveItv" ||
                    control.Name == "label3" ||
                    control.Name == "pic_CS" ||
                    control.Name == "pic1" ||
                    control.Name == "button1" ||
                    control.Location.Y < 110)
                {
                    controlsToMove.Add(control);
                }
            }
            
            // 컨트롤 이동
            foreach (var control in controlsToMove)
            {
                panel1.Controls.Remove(control);
                topPanel.Controls.Add(control);
            }
        }
        
        // 🎨 CardView 데이터 업데이트
        private void UpdateCardViewData(string itemName, string value)
        {
            if (_dataTable == null || _cardView == null) return;
            
            this.Invoke((MethodInvoker)delegate
            {
                // 기존 행 찾기
                DataRow existingRow = null;
                foreach (DataRow row in _dataTable.Rows)
                {
                    if (row["항목"].ToString() == itemName)
                    {
                        existingRow = row;
                        break;
                    }
                }
                
                if (existingRow != null)
                {
                    // 기존 행 업데이트
                    existingRow["값"] = value;
                }
                else
                {
                    // 새 행 추가
                    DataRow newRow = _dataTable.NewRow();
                    newRow["항목"] = itemName;
                    newRow["값"] = value;
                    _dataTable.Rows.Add(newRow);
                }
                
                // CardView 새로고침
                _cardView.RefreshData();
            });
        }
        
        // 🎨 카드 테두리 그리기
        private void DrawCardBorder(object sender, PaintEventArgs e)
        {
            Panel card = sender as Panel;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // 카드 테두리
            using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = GetRoundedRectangle(rect, 6))
                {
                    g.DrawPath(pen, path);
                }
            }
            
            // 그림자 효과
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
            {
                Rectangle shadowRect = new Rectangle(2, 2, card.Width - 2, card.Height - 2);
                using (GraphicsPath shadowPath = GetRoundedRectangle(shadowRect, 6))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }
            }
        }

        // 🎯 전체 수집 안정 상태 표시 로직
        private void UpdateHealthIndicator() { }

        public void Log(string msg)
        {
            try
            {
                string logstr = DateTime.Now.ToString("yyyyMMdd HHmmss") + " " + msg;
                
                // 🎯 디바이스별 독립적인 로그 파일 생성 (시간별 분리)
                string deviceName = (!string.IsNullOrEmpty(_Faci) ? _Faci : "Unknown").Replace(" ", "_");
                string fileName = $"log_{deviceName}_{DateTime.Now:yyyyMMdd_HH}.txt";
                string oFile = Path.Combine(Application.StartupPath, fileName);
                
                FileInfo f = new FileInfo(oFile);
                if (f.Exists)
                {
                    using (StreamWriter sw = f.AppendText())
                    {
                    sw.WriteLine(logstr);
                    }
                }
                else
                {
                    using (StreamWriter sw = f.CreateText())
                    {
                    sw.WriteLine(logstr);
                    }
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
            
            // 초기 상태 표시등 설정
            pic_CS.BackgroundImage = offImg;
            pic1.BackgroundImage = offImg;

            string[] ast = _setV.Replace("\r\n", "").Split(',');

            // 🎯 설정 문자열 파싱
            // 새 형식: IP:Port,Interval,StartAddress,DataLength,SlaveId,DeviceName#DeviceCode,Items,Mappings,SaveInterval
            // 기존 형식: IP:Port,Interval,StartAddress,DataLength,SlaveId,DeviceName#DeviceCode,Items,Mappings[,ExtraParam]

            if (ast.Length >= 9)
            {
                // 호환성 처리:
                // - 8개: 구버전(저장주기/포맷 없음)
                // - 9개: 새 포맷(저장주기) 또는 포맷(aMem2)
                // - 10개 이상: aMem2 + 저장주기 포함
                bool ninthIsSaveInterval = int.TryParse(ast[8].Trim(), out _);
                if (ast.Length == 9)
                {
                    if (ninthIsSaveInterval)
                    {
                        // 9번째가 저장주기
                        string saveInterval = ast[8];
                        Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                                  ast[6].Split('/'), ast[7].Split('/'), Array.Empty<string>(), saveInterval);
                    }
                    else
                    {
                        // 9번째가 포맷(aMem2)
                        Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                                  ast[6].Split('/'), ast[7].Split('/'), ast[8].Split('/'));
                    }
                }
                else
                {
                    // 10개 이상: aMem2 + 저장주기
                    string saveInterval = ast[9];
                    Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                              ast[6].Split('/'), ast[7].Split('/'), ast[8].Split('/'), saveInterval);
                }
            }
            else if (ast.Length == 8) // 8개 파라미터 (더 이전 버전)
            {
                Controller(ast[0], ast[1], ast[2], ast[3], ast[4], ast[5],
                          ast[6].Split('/'), ast[7].Split('/'));
            }

            // Connect();  // ReliableModBusService 사용으로 불필요
            // getCnt();   // ReliableModBusService 사용으로 불필요
        }

        // 로고 아이콘 생성
        private Image CreateLogoIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                
                // 전력계인지 카운터인지 확인
                bool isPower = _Faci != null && (_Faci.Contains("전력") || _Faci.Contains("kw"));
                
                if (isPower)
                {
                    // 전력계 아이콘 - 번개 모양
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddPolygon(new Point[] {
                            new Point(20, 4),
                            new Point(12, 16),
                            new Point(18, 16),
                            new Point(12, 28),
                            new Point(20, 16),
                            new Point(14, 16)
                        });
                        
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            new Rectangle(0, 0, 32, 32),
                            primaryColor,
                            accentColor,
                            45F))
                        {
                            g.FillPath(brush, path);
                        }
                    }
                }
                else
                {
                    // 카운터 아이콘 - 숫자 모양
                    using (Font iconFont = new Font("맑은 고딕", 18F, FontStyle.Bold))
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new Rectangle(0, 0, 32, 32),
                        successColor,
                        Color.FromArgb(5, 150, 105),
                        45F))
                    {
                        StringFormat sf = new StringFormat();
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString("#", iconFont, brush, new Rectangle(0, 0, 32, 32), sf);
                    }
                }
            }
            return bmp;
        }

        private Image CreateStatusIcon(Color color)
        {
            Bitmap bmp = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // 네온 글로우 효과
                for (int i = 4; i >= 0; i--)
                {
                    using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(15 - i * 3, color)))
                    {
                        g.FillEllipse(glowBrush, 2 - i, 2 - i, 20 + i * 2, 20 + i * 2);
                    }
                }

                // 메인 원 - 그라디언트
                Rectangle mainRect = new Rectangle(4, 4, 16, 16);
                using (LinearGradientBrush mainBrush = new LinearGradientBrush(
                    mainRect,
                    color,
                    Color.FromArgb(200, color.R / 2, color.G / 2, color.B / 2),
                    45F))
                {
                    g.FillEllipse(mainBrush, mainRect);
                }

                // 내부 하이라이트
                using (LinearGradientBrush highlightBrush = new LinearGradientBrush(
                    new Rectangle(6, 6, 8, 8),
                    Color.FromArgb(120, 255, 255, 255),
                    Color.Transparent,
                    45F))
                {
                    g.FillEllipse(highlightBrush, 6, 6, 8, 8);
                }

                // 가장자리 림라이트
                using (Pen rimPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
                {
                    g.DrawEllipse(rimPen, 4, 4, 16, 16);
                }
            }
            return bmp;
        }

        public async void Controller(string ip, string interval, string adrr, string length, string sid, string title = "", string[] aitem = null, string[] aMem = null, string[] aMem2 = null, string saveInterval = "60")
        {
            // 설정값 그대로 사용 (하드코딩 제거)
            _adrr = Convert.ToUInt16(adrr);
            _length = Convert.ToUInt16(length);
            
            _sid = Convert.ToUInt16(sid);
            _aMem = aMem;
            _aMem2 = aMem2;
            _aitem = aitem;
            _ip = ip;
            
            lbItv.Text = interval; //초
            lbFaci.Text = title.Split('#')[0];
            _Faci = title.Split('#')[1];

            // 🎯 저장 주기 설정
            _saveInterval = Convert.ToInt32(saveInterval);
            // 상태표시 기준(수집주기)도 갱신
            if (int.TryParse(interval, out var parsedIntervalSeconds) && parsedIntervalSeconds > 0)
            {
                _statusTimeoutSec = parsedIntervalSeconds;
            }

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
            
            // 상태표시 기준은 위에서 이미 갱신됨 (중복 제거)

            // 하단 정보바 업데이트
            var collectLabel = panel1.Controls.Find("collectLabel", true).FirstOrDefault() as Label;
            if (collectLabel != null)
            {
                collectLabel.Text = $"수집: {interval}초";
            }
            
            var saveLabel = panel1.Controls.Find("saveLabel", true).FirstOrDefault() as Label;
            if (saveLabel != null)
            {
                saveLabel.Text = $"저장: {_saveInterval}초";
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

            // 🚀 NEW: ReliableModBusService 설정 및 초기화
            await InitializeReliableModBusServiceWithConfig(ip, collectionInterval);

            // 폼 자체 폴링/타이머는 사용하지 않음 (백엔드가 폴링)
            timer1.Enabled = false;
            saveTimer.Stop();
            Log($"🚀 디바이스 '{lbFaci.Text}' 초기화 완료 - 수집주기: {interval}초, 저장주기: {_saveInterval}초 (백엔드 폴링, UI 구독)");
        }

        // 🎯 IP 주소 기반 타이머 오프셋 계산 (충돌 방지)
        private int CalculateTimerOffset(string ipAddress)
        {
            try
            {
                // IP 주소의 마지막 옥텟을 기반으로 오프셋 계산
                string cleanIp = ipAddress.Split(':')[0]; // 포트 제거
                string[] ipParts = cleanIp.Split('.');
                
                if (ipParts.Length >= 4)
                {
                    int lastOctet = int.Parse(ipParts[3]);
                    
                    // 0~2000ms 범위의 오프셋 생성 (각 IP마다 고유한 값)
                    int offset = (lastOctet * 47) % 2000; // 47은 적당한 소수
                    
                    Log($"🔢 IP {cleanIp} → 오프셋 {offset}ms (마지막 옥텟: {lastOctet})");
                    return offset;
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ 오프셋 계산 오류: {ex.Message}");
            }
            
            // 기본값: IP 파싱 실패 시 0~1000ms 랜덤
            Random rand = new Random(ipAddress.GetHashCode());
            int randomOffset = rand.Next(0, 1000);
            Log($"🎲 랜덤 오프셋 사용: {randomOffset}ms");
            return randomOffset;
        }

        // 🚀 NEW: 디바이스 설정으로 ReliableModBusService 초기화
        private async Task InitializeReliableModBusServiceWithConfig(string ip, int interval)
        {
            try
            {
                string[] ipParts = ip.Split(':');
                
                // ModbusDeviceSettings 생성
                _deviceSettings = new ModbusDeviceSettings
                {
                    IPAddress = ipParts[0].Trim(),
                    Port = ipParts.Length > 1 ? Convert.ToInt32(ipParts[1].Trim()) : 502,
                    SlaveId = _sid,
                    DeviceName = _Faci,
                    Interval = interval,
                    StartAddress = _adrr,
                    DataLength = _length,
                    SaveInterval = _saveInterval,
                    IsActive = true
                };

                // 기존 서비스 해제
                _reliableModBusService?.Dispose();

                // 새로운 ReliableModBusService 생성 (전역 공유 레지스트리를 사용하여 진단과 세션 공유)
                _reliableModBusService = await ServiceRegistry.GetOrCreateAsync(_deviceSettings);
                // 런타임 폴링 주기를 디바이스 수집주기와 동기화
                _reliableModBusService.SetPollingIntervalSeconds(interval);

                // 이벤트 핸들러 등록
                _reliableModBusService.ConnectionStatusChanged += OnConnectionStatusChanged;
                _reliableModBusService.DataReceived += OnDataReceived;
                _reliableModBusService.ErrorOccurred += OnErrorOccurred;

                // 비동기 연결 시작
                bool connected = await _reliableModBusService.ConnectAsync();
                
                if (connected)
                {
                    Log($"✅ ReliableModBusService 연결 성공: {_deviceSettings.IPAddress}:{_deviceSettings.Port}");
                }
                else
                {
                    Log($"⚠️ ReliableModBusService 초기 연결 실패 - 자동 재연결이 진행됩니다.");
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 ReliableModBusService 초기화 오류: {ex.Message}");
            }
        }

        // 🚀 NEW: 연결 상태 변경 이벤트 핸들러 (상세 로깅)
        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, ConnectionStatusEventArgs>(OnConnectionStatusChanged), sender, e);
                return;
            }

            var timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Log($"🔗 [{timeStamp}] 연결 상태 변경: {e.Message}");
            Log($"   - IP: {_ip}, 디바이스: {_Faci}");
            Log($"   - 현재 레지스터: {_adrr}~{_adrr + _length - 1} (길이: {_length})");
            Log($"   - 연결 상태: {(e.IsConnected ? "연결됨" : "연결 끊어짐")}");
            
            // 연결 상태에 따른 UI 업데이트
            this.pic_CS.BackgroundImage = e.IsConnected ? onImg : offImg;
        }

        // 🚀 NEW: 데이터 수신 이벤트 핸들러  
        private void OnDataReceived(object sender, ModBusDataEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, ModBusDataEventArgs>(OnDataReceived), sender, e);
                return;
            }

            // 기존 UI 업데이트 로직 재사용
            UpdateUIWithData(e.Data, e.Timestamp);
            _lastDataTimestamp = e.Timestamp;
            UpdateHealthIndicator();

            // 수집주기 기준 상태 표시: 최근 도착은 파랑으로 즉시 전환
            _dataStatusPic.Image = _statusBlueImg;
        }

        // 🚀 NEW: 오류 발생 이벤트 핸들러
        private void OnErrorOccurred(object sender, ModBusErrorEventArgs e)
        {
            Log($"❌ ModBus 오류: {e.Message}");
        }

        // 🚀 NEW: 수신된 데이터로 UI 업데이트
        private void UpdateUIWithData(ushort[] data, DateTime timestamp)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    Log("⚠️ 빈 데이터 수신");
                    return;
                }

                // 연결 상태 표시 업데이트
                pic1.BackgroundImage = onImg;

                // 🎯 데이터 버퍼 생성: 화면에 표시된 값 그대로 저장하기 위한 최신 스냅샷
                var buffer = new DataBuffer
                {
                    Timestamp = timestamp,
                    FacilityCode = _Faci
                };

                // 🚀 FIX: 설정된 아이템만 데이터 표시
                
                // 모든 라벨을 먼저 숨김 (기본값으로 초기화)
                for (int j = 1; j <= 7; j++)
                {
                    UpdateValueLabel(j, "");
                    UpdateItemLabel(j, "");
                }
                
                // 설정된 아이템만 처리
                if (_aitem != null)
                {
                    int displayIndex = 1; // UI 표시 순서
                    
                    for (int i = 0; i < _aitem.Length && i < data.Length && displayIndex <= 7; i++)
                    {
                        string itemName = _aitem[i]?.Trim();
                        
                        // 빈 항목이거나 null인 경우 건너뛰기
                        if (string.IsNullOrEmpty(itemName))
                            continue;
                            
                        // 🔋 누적 전력량 처리: DDS6619의 경우 32bit 값 결합 (상세 로깅)
                        double value;
                        string timeStamp = timestamp.ToString("HH:mm:ss.fff");
                        
                        if (_adrr == 4126 && i == 0 && data.Length >= 2) // 누적 전력량 모드
                        {
                            // 2개 레지스터를 32bit로 결합 (Little Endian: 하위+상위)
                            uint lowWord = data[0];   // 하위 16bit
                            uint highWord = data[1];  // 상위 16bit
                            uint combined = (highWord << 16) | lowWord;
                            
                            // DDS6619 스케일링: 보통 0.01 kWh 단위
                            value = combined * 0.01;
                            
                            Log($"🔋 [{timeStamp}] 누적 전력량 계산 상세:");
                            Log($"   - 원시 데이터: R0={lowWord}(0x{lowWord:X4}), R1={highWord}(0x{highWord:X4})");
                            Log($"   - 32bit 결합: {combined}(0x{combined:X8})");
                            Log($"   - 스케일링: {combined} × 0.01 = {value:F3} kWh");
                        }
                        else
                        {
                            // 일반 16bit 값
                            value = data[i];
                            Log($"📊 [{timeStamp}] 일반 데이터: R{i}={data[i]}(0x{data[i]:X4}) → {value:F3}");
                        }
                        
                        string displayValue = value.ToString("F0"); // 정수로 표시 (소수점 없음)
                        
                        // 🔍 이전 값과 비교 로깅
                        var previousKey = $"{itemName}_previous";
                        if (this.Tag is Dictionary<string, double> previousValues)
                        {
                            if (previousValues.ContainsKey(previousKey))
                            {
                                double previousValue = previousValues[previousKey];
                                double difference = value - previousValue;
                                if (Math.Abs(difference) > 0.001) // 0.001 이상 차이날 때만 로깅
                                {
                                    Log($"🔄 [{timeStamp}] {itemName} 값 변화: {previousValue:F3} → {value:F3} (차이: {difference:+F3})");
                                }
                            }
                            previousValues[previousKey] = value;
                        }
                        else
                        {
                            this.Tag = new Dictionary<string, double> { { previousKey, value } };
                        }
                        
                        // 🎨 대시보드 UI 업데이트 (표시값 갱신 → 이후 저장은 이 표시값 기준)
                        UpdateDashboardUI(itemName, value, displayValue);
                        
                        // 버퍼에 추가
                        buffer.Values[itemName] = value;
                        
                        Log($"📈 {itemName}: {displayValue} (레지스터 {i} → UI {displayIndex})");
                        
                        displayIndex++; // 다음 UI 위치로
                        
                        // 🔋 누적 전력량 모드에서는 첫 번째 값만 처리하고 종료
                        if (_adrr == 4126 && i == 0)
                        {
                            Log($"🔋 누적 전력량 모드: 첫 번째 값만 표시 완료");
                            break;
                        }
                    }
                    
                    Log($"📊 총 {displayIndex - 1}개 항목 표시됨");
                }

                // 🎯 저장 방식에 따른 데이터 처리
                if (_saveMethod == SaveMethod.Periodic)
                {
                    // 주기별 강제 저장: 항상 최신 데이터로 교체
                    _latestData = buffer;
                _hasUnsavedData = true;
                    Log($"📊 데이터 수신 완료 - {data.Length}개 레지스터, 시간: {timestamp:HH:mm:ss} (주기별 저장 대기)");
                }
                else if (_saveMethod == SaveMethod.ChangeDetection)
                {
                    // 변화 감지 저장: 이전 데이터와 비교
                    bool hasChanged = HasDataChanged(buffer, _previousData);
                    if (hasChanged)
                    {
                        _latestData = buffer;
                        _hasUnsavedData = true;
                        Log($"📊 데이터 변화 감지 - {data.Length}개 레지스터, 시간: {timestamp:HH:mm:ss} (변화 저장 대기)");
                    }
                    else
                    {
                        Log($"📊 데이터 수신 완료 - {data.Length}개 레지스터, 시간: {timestamp:HH:mm:ss} (변화 없음, 저장 생략)");
                    }
                    _previousData = buffer; // 다음 비교를 위해 저장
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 데이터 처리 오류: {ex.Message}");
            }
        }

        // 레지스터 값 변환 (기존 로직 기반)
        private double ConvertRegisterValue(ushort registerValue, int index)
                {
                    try
                    {
                // 메모리 매핑 정보가 있으면 활용
                if (_aMem != null && index < _aMem.Length && !string.IsNullOrEmpty(_aMem[index]))
                {
                    string memType = _aMem[index].ToUpper();
                    
                    switch (memType)
                    {
                        case "F": // Float
                            return Convert.ToSingle(registerValue);
                        case "D": // Double  
                            return Convert.ToDouble(registerValue);
                        case "I": // Integer
                            return Convert.ToInt32(registerValue);
                        default:
                            return registerValue;
                    }
                }
                
                return registerValue;
            }
            catch
            {
                return registerValue;
            }
        }

        // 값 포맷팅
        private string FormatValue(double value, int index)
        {
            try
            {
                if (_aMem2 != null && index < _aMem2.Length && !string.IsNullOrEmpty(_aMem2[index]))
                {
                    return string.Format(_aMem2[index], value);
                }
                
                return value.ToString("F2");
            }
            catch
            {
                return value.ToString();
            }
        }

        // UI 라벨 업데이트 (값 라벨 + 아이템 라벨)
        private void UpdateValueLabel(int index, string value)
        {
            try
            {
                Label valueLabel = index switch
                {
                    1 => lbCnt1,
                    2 => lbCnt2, 
                    3 => lbCnt3,
                    4 => lbCnt4,
                    5 => lbCnt5,
                    6 => lbCnt6,
                    7 => lbCnt7,
                    _ => null
                };
                
                Label itemLabel = index switch
                {
                    1 => lbItem1,
                    2 => lbItem2,
                    3 => lbItem3,
                    4 => lbItem4,
                    5 => lbItem5,
                    6 => lbItem6,
                    7 => lbItem7,
                    _ => null
                };

                                    if (valueLabel != null)
                    {
                        valueLabel.Text = value;
                        
                        // 빈 값인 경우 값 라벨과 아이템 라벨 모두 숨기기
                        if (string.IsNullOrEmpty(value))
                        {
                            valueLabel.Visible = false;
                            if (itemLabel != null) itemLabel.Visible = false;
                        }
                        else
                        {
                            valueLabel.Visible = true;
                            // 🎨 카드 UI에 맞는 색상 사용 (파란색)
                            valueLabel.ForeColor = primaryColor;
                            if (itemLabel != null) itemLabel.Visible = true;
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                Log($"UI 업데이트 오류: {ex.Message}");
            }
        }

        // 아이템 라벨 업데이트
        private void UpdateItemLabel(int index, string itemName)
        {
            try
            {
                Label itemLabel = index switch
                {
                    1 => lbItem1,
                    2 => lbItem2,
                    3 => lbItem3,
                    4 => lbItem4,
                    5 => lbItem5,
                    6 => lbItem6,
                    7 => lbItem7,
                    _ => null
                };

                if (itemLabel != null)
                {
                    itemLabel.Text = itemName ?? "";
                }
            }
            catch (Exception ex)
            {
                Log($"아이템 라벨 업데이트 오류: {ex.Message}");
            }
        }

        bool bfirst = true;
        bool breset = false;

        private static DateTime _lastTimerCall = DateTime.MinValue;
        
        private async void timer1_Tick(object sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            var timeStamp = currentTime.ToString("HH:mm:ss.fff");
            
            // 🔍 타이머 간격 분석
            // 상세 타이머 로그는 과다 I/O를 유발하므로 24/7 운용 시 비활성화
            // 필요 시 샘플링 로깅으로 전환하세요.
            _lastTimerCall = currentTime;

            // 🚀 NEW: ReliableModBusService를 사용한 비동기 데이터 수집
            if (_reliableModBusService != null)
            {
                // 연결 상태 체크
                var stats = _reliableModBusService.GetStatistics();
                Log($"🔗 [{timeStamp}] 연결 상태: {(stats.IsConnected ? "연결됨" : "연결 끊어짐")} - 품질: {stats.ConnectionQuality}/100");
                
                await GetDataAsync();
                
                // 리셋 로직 (기존 유지)
                if (breset == false && DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
                {
                    Log($"🔄 [{timeStamp}] 자동 리셋 시작");
                    // TODO: WriteSingleRegister 기능을 ReliableModBusService에 추가 필요
                    breset = true;
                }
                if (breset && DateTime.Now.Hour == 23 && DateTime.Now.Minute == 59)
                {
                    Log($"🔄 [{timeStamp}] 자동 리셋 완료");
                    breset = false;
                }
            }
            else
            {
                Log($"⚠️ [{timeStamp}] ReliableModBusService 없음 - 기존 방식 사용");
                // 🔄 Fallback: ReliableModBusService가 없으면 기존 방식 사용
                getCnt();
            }
        }
        
        // 🚀 NEW: ReliableModBusService를 사용한 비동기 데이터 수집 (단순화)
        private async Task GetDataAsync()
        {
            var timestamp = DateTime.Now;
            var millisecond = timestamp.ToString("HH:mm:ss.fff");
            
            try
            {
                if (_reliableModBusService == null)
                {
                    Log($"⚠️ [{millisecond}] ReliableModBusService가 초기화되지 않았습니다.");
                    return;
                }

            // UI는 더 이상 직접 읽지 않음 (백엔드 이벤트로 갱신)
            var data = Array.Empty<ushort>();
                
                if (data != null && data.Length > 0)
                {
                    // 🔍 원시 데이터 상세 로깅
                    string rawDataLog = string.Join(", ", data.Select((val, idx) => $"R{idx}={val}"));
                    Log($"📊 [{millisecond}] 원시 데이터: [{rawDataLog}]");
                    
                    // 🚫 중복 방지: OnDataReceived 이벤트가 자동으로 UpdateUIWithData를 호출하므로 여기서는 제거
                    // UpdateUIWithData(data, timestamp); // 제거됨
                    Log($"✅ [{millisecond}] 데이터 처리 완료: {data.Length}개 레지스터");

                    // 수집 상태 최신화
                    _lastDataTimestamp = timestamp;
                    UpdateHealthIndicator();
                    _dataStatusPic.Image = _statusBlueImg; // 폴백
                }
                else
                {
                    // 읽기는 백엔드에서 수행
                }
            }
            catch (Exception ex)
            {
                pic1.BackgroundImage = offImg;
                Log($"🚨 [{millisecond}] 데이터 수집 오류: {ex.Message}");
                Log($"🔍 [{millisecond}] 스택 트레이스: {ex.StackTrace}");
            }
        }

        // 🚫 개별 DB 저장 타이머 이벤트 - 비활성화됨 (MainForm 전역 타이머 사용)
        private void SaveTimer_Tick(object sender, EventArgs e) { }

        ushort p = 0;
        bool bconnect = false;

        // 🚀 DEPRECATED: 기존 Connect 메서드는 ReliableModBusService로 대체됨
        public void Connect()
        {
            Log("⚠️ 기존 Connect 메서드는 더 이상 사용되지 않습니다. ReliableModBusService를 사용합니다.");
            // ReliableModBusService가 자동으로 연결을 관리하므로 별도 처리 불필요
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

        // 🚀 DEPRECATED: 기존 getData 메서드는 ReliableModBusService로 대체됨
        private void getData(int gb)
        {
            Log("⚠️ 기존 getData 메서드는 더 이상 사용되지 않습니다. GetDataAsync를 사용합니다.");
            // ReliableModBusService의 GetDataAsync가 이 기능을 대체
            return;
            
            /* DEPRECATED CODE - 참고용으로 보존
            try
            {
                Int16[] Registers = new Int16[_length];
                PictureBox pic = pic1;
                byte[] intBytes = BitConverter.GetBytes(_sid); //국번
                Result Result = Result.DEMO_TIMEOUT;
                string rdt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // REMOVED: Result = modbusCtrl.ReadInputRegisters(intBytes[0], _adrr, _length, Registers);

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

                        // 🚀 DEPRECATED: ModbusCtrl 대신 ReliableModBusService 사용
                        /*
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
                        */

                        // 🎯 실시간 UI 업데이트 (애니메이션 효과)
                        Label targetLabel = null;
                        if (p == 1) targetLabel = lbCnt1;
                        else if (p == 2) targetLabel = lbCnt2;
                        else if (p == 3) targetLabel = lbCnt3;
                        else if (p == 4) targetLabel = lbCnt4;
                        else if (p == 5) targetLabel = lbCnt5;
                        else if (p == 6) targetLabel = lbCnt6;
                        else if (p == 7) targetLabel = lbCnt7;

                        // 🚀 DEPRECATED: cValue 변수는 더 이상 사용하지 않습니다
                        /*
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
                        */

                        // 🎯 DEPRECATED: 아래 코드는 더 이상 사용하지 않습니다
                        /*
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
            */
            
        }

        // 🎯 DB 저장 상태 표시 업데이트
        private void UpdateDbSaveStatus()
        {
            var pic_DB = panel1.Controls.Find("pic_DB", false).FirstOrDefault() as PictureBox;
            if (pic_DB != null)
            {
                if (_hasUnsavedData && _latestData != null)
                {
                    pic_DB.BackgroundImage = pendingImg;
                    // 툴팁으로 저장 대기 상태 알림
                    _dbToolTip ??= new ToolTip();
                    int remainingSeconds = _saveInterval - (int)(DateTime.Now - _lastSaveTime).TotalSeconds;
                    _dbToolTip.SetToolTip(pic_DB, $"최신 데이터 저장 대기 중... (다음 저장: {Math.Max(0, remainingSeconds)}초 후)");
                }
                else
                {
                    pic_DB.BackgroundImage = saveImg;
                }
            }
        }

        // 🎯 데이터베이스 저장 메서드 - 최신 데이터 1개만 저장
        public void SaveDataToDatabase()
        {
            if (!_hasUnsavedData || _latestData == null)
                return;

            try
            {
                Log($"💾 DB 저장 시작 - 최신 데이터 1개 (시간: {_latestData.Timestamp:HH:mm:ss})");

                // 최신 데이터의 각 항목별로 저장
                foreach (var kvp in _latestData.Values)
                    {
                        var acquiredData = new AcquiredData(SessionService.Instance.UOW)
                        {
                        FacilityCode = _latestData.FacilityCode,
                            NumericData = kvp.Value,
                            StringData = kvp.Key,
                            IPAddres = _ip,
                        CreatedDateTime = _latestData.Timestamp
                        };
                }

                SessionService.Instance.InsertOrUpdate();

                // 최신 데이터 클리어
                _latestData = null;
                _hasUnsavedData = false;
                _lastSaveTime = DateTime.Now;

                Log($"✅ DB 저장 완료 - {_Faci} 최신 데이터");

                // 상태 표시 업데이트
                UpdateDbSaveStatus();
            }
            catch (Exception ex)
            {
                Log($"❌ DB 저장 오류 - {_Faci}: {ex.Message}");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("HF2311S를 리스타트하시겠습니까?", "HF2311S 리스타트", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Log("🔄 HF2311S 리스타트 시작...");
                await AttemptHF2311SRestart();
            }
        }
        
        // HF2311S 리스타트 시도 (셋팅프로그램 방식 재현)
        private async Task AttemptHF2311SRestart()
        {
            string deviceIP = _ip.Split(':')[0];
            Log($"🎯 HF2311S 리스타트 대상: {deviceIP}");
            
            bool success = false;
            
            // 방법 1: HTTP POST 기반 리스타트 (가장 일반적)
            success = await TryHttpRestart(deviceIP);
            if (success) return;
            
            // 방법 2: HTTP GET 기반 리스타트
            success = await TryHttpGetRestart(deviceIP);
            if (success) return;
            
            // 방법 3: TCP Socket 기반 리스타트 명령
            success = await TryTcpRestart(deviceIP);
            if (success) return;
            
            // 방법 4: UDP 기반 리스타트 명령
            success = await TryUdpRestart(deviceIP);
            if (success) return;
            
            // 방법 5: ModBus 특수 레지스터 기반 리스타트
            success = await TryModbusRestart();
            if (success) return;
            
            Log("❌ 모든 리스타트 방법 실패 - 수동 전원 재시작 필요");
        }
        
        // HTTP POST 방식 리스타트
        private async Task<bool> TryHttpRestart(string deviceIP)
        {
            try
            {
                Log("🌐 방법 1: HTTP POST 리스타트 시도");
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // HF2311S에서 자주 사용되는 POST 엔드포인트들
                    string[] postUrls = {
                        $"http://{deviceIP}/restart",
                        $"http://{deviceIP}/reboot",
                        $"http://{deviceIP}/reset",
                        $"http://{deviceIP}/admin/restart",
                        $"http://{deviceIP}/cgi-bin/restart"
                    };
                    
                    foreach (string url in postUrls)
                    {
                        try
                        {
                            Log($"🔗 POST 시도: {url}");
                            var content = new System.Net.Http.StringContent("restart=1", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                            var response = await client.PostAsync(url, content);
                            
                            Log($"📝 응답: {response.StatusCode}");
                            if (response.IsSuccessStatusCode)
                            {
                                Log($"✅ HTTP POST 리스타트 성공: {url}");
                                Log("⏳ HF2311S 재시작 대기 중... (30초)");
                                await Task.Delay(30000);
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ {url}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 HTTP POST 리스타트 실패: {ex.Message}");
            }
            return false;
        }
        
        // HTTP GET 방식 리스타트
        private async Task<bool> TryHttpGetRestart(string deviceIP)
        {
            try
            {
                Log("🌐 방법 2: HTTP GET 리스타트 시도");
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    string[] getUrls = {
                        $"http://{deviceIP}/restart?cmd=1",
                        $"http://{deviceIP}/reboot?action=restart",
                        $"http://{deviceIP}/reset?type=soft",
                        $"http://{deviceIP}/?cmd=restart",
                        $"http://{deviceIP}/admin?action=reboot"
                    };
                    
                    foreach (string url in getUrls)
                    {
                        try
                        {
                            Log($"🔗 GET 시도: {url}");
                            var response = await client.GetAsync(url);
                            
                            Log($"📝 응답: {response.StatusCode}");
                            if (response.IsSuccessStatusCode)
                            {
                                Log($"✅ HTTP GET 리스타트 성공: {url}");
                                Log("⏳ HF2311S 재시작 대기 중... (30초)");
                                await Task.Delay(30000);
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ {url}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 HTTP GET 리스타트 실패: {ex.Message}");
            }
            return false;
        }
        
        // TCP Socket 방식 리스타트
        private async Task<bool> TryTcpRestart(string deviceIP)
        {
            try
            {
                Log("📡 방법 3: TCP Socket 리스타트 시도");
                
                // 일반적인 TCP 포트들 시도
                int[] tcpPorts = { 23, 80, 502, 8080, 9999, 10001 };
                string[] commands = { 
                    "RESTART\r\n", 
                    "REBOOT\r\n", 
                    "RESET\r\n",
                    "AT+RST\r\n",
                    "HF-RESTART\r\n"
                };
                
                foreach (int port in tcpPorts)
                {
                    foreach (string cmd in commands)
                    {
                        try
                        {
                            Log($"🔗 TCP 시도: {deviceIP}:{port} - {cmd.Trim()}");
                            using (var client = new System.Net.Sockets.TcpClient())
                            {
                                await client.ConnectAsync(deviceIP, port);
                                using (var stream = client.GetStream())
                                using (var writer = new System.IO.StreamWriter(stream))
                                {
                                    await writer.WriteAsync(cmd);
                                    await writer.FlushAsync();
                                    
                                    Log($"✅ TCP 명령 전송 성공: {deviceIP}:{port}");
                                    Log("⏳ HF2311S 재시작 대기 중... (30초)");
                                    await Task.Delay(30000);
                                    return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ TCP {deviceIP}:{port}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 TCP 리스타트 실패: {ex.Message}");
            }
            return false;
        }
        
        // UDP 방식 리스타트
        private async Task<bool> TryUdpRestart(string deviceIP)
        {
            try
            {
                Log("📡 방법 4: UDP 리스타트 시도");
                
                using (var client = new System.Net.Sockets.UdpClient())
                {
                    var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(deviceIP), 48899);
                    
                    string[] udpCommands = {
                        "HF-RESTART",
                        "RESTART",
                        "REBOOT", 
                        "RESET",
                        "AT+RST"
                    };
                    
                    foreach (string cmd in udpCommands)
                    {
                        try
                        {
                            Log($"🔗 UDP 시도: {deviceIP}:48899 - {cmd}");
                            byte[] data = System.Text.Encoding.ASCII.GetBytes(cmd);
                            await client.SendAsync(data, data.Length, endpoint);
                            
                            Log($"✅ UDP 명령 전송: {cmd}");
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ UDP {cmd}: {ex.Message}");
                        }
                    }
                    
                    Log("⏳ UDP 명령 전송 완료 - HF2311S 재시작 대기 중... (30초)");
                    await Task.Delay(30000);
                    return true; // UDP는 응답 확인이 어려우므로 전송 성공을 리턴
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 UDP 리스타트 실패: {ex.Message}");
            }
            return false;
        }
        
        // ModBus 특수 레지스터 방식 리스타트
        private async Task<bool> TryModbusRestart()
        {
            try
            {
                Log("🔧 방법 5: ModBus 특수 레지스터 리스타트 시도");
                
                if (_reliableModBusService != null)
                {
                    // HF2311S에서 사용될 수 있는 특수 레지스터들
                    var restartCommands = new[]
                    {
                        new { Register = 0xFFFF, Value = (ushort)0x1234, Desc = "일반적인 리스타트 레지스터" },
                        new { Register = 0x0000, Value = (ushort)0xAAAA, Desc = "시스템 제어 레지스터" },
                        new { Register = 0x1000, Value = (ushort)0x0001, Desc = "리셋 명령 레지스터" },
                        new { Register = 58, Value = (ushort)0, Desc = "기존 리셋 레지스터" }
                    };
                    
                    foreach (var cmd in restartCommands)
                    {
                        try
                        {
                            Log($"🔧 ModBus 시도: 레지스터 {cmd.Register} = {cmd.Value} ({cmd.Desc})");
                            
                            // TODO: ReliableModBusService에 WriteSingleRegister 추가 필요
                            // var result = await _reliableModBusService.WriteSingleRegisterAsync(_sid, cmd.Register, cmd.Value);
                            Log($"⚠️ WriteSingleRegister 기능이 ReliableModBusService에 구현되지 않음");
                            
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ ModBus 레지스터 {cmd.Register}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Log("❌ ReliableModBusService가 초기화되지 않음");
                }
            }
            catch (Exception ex)
            {
                Log($"🚨 ModBus 리스타트 실패: {ex.Message}");
            }
            return false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // 🎯 폼 종료 시 정리
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 🚫 중복 저장 방지: MainForm에서 이미 SaveDataToDatabase() 호출함
            // MainForm.OnFormClosing()에서 모든 디바이스의 SaveDataToDatabase()를 일괄 처리
            // if (_hasUnsavedData) { SaveDataToDatabase(); } // 주석 처리

            // 🚀 NEW: ReliableModBusService 정리
            if (_reliableModBusService != null)
            {
                try
                {
                    _reliableModBusService.ConnectionStatusChanged -= OnConnectionStatusChanged;
                    _reliableModBusService.DataReceived -= OnDataReceived;
                    _reliableModBusService.ErrorOccurred -= OnErrorOccurred;
                    ServiceRegistry.Release(_deviceSettings);
                    _reliableModBusService = null;
                    
                    Log("🛑 ReliableModBusService 정리 완료");
                }
                catch (Exception ex)
                {
                    Log($"ReliableModBusService 정리 중 오류: {ex.Message}");
                }
            }

            // 타이머 정리
            timer1?.Stop();
            saveTimer?.Stop();
            connectionMonitorTimer?.Stop();
            _statusTimer?.Stop();
            timer1?.Dispose();
            saveTimer?.Dispose();
            connectionMonitorTimer?.Dispose();
            _statusTimer?.Dispose();

            base.OnFormClosing(e);
        }
        
        // 🎯 저장 방식 설정 메서드 (MainForm에서 호출)
        public void SetSaveMethod(SaveMethod method)
        {
            _saveMethod = method;
            string methodName = method == SaveMethod.Periodic ? "주기별 강제 저장" : "변화 감지 저장";
            Log($"🔧 저장 방식 변경: {methodName} ({_Faci})");
            
            // 변화 감지 모드로 변경 시 이전 데이터 초기화
            if (method == SaveMethod.ChangeDetection)
            {
                _previousData = null;
                Log($"🔄 변화 감지 모드 시작 - 이전 데이터 초기화 ({_Faci})");
            }
        }
        
        // 🎯 데이터 변화 감지 메서드
        private bool HasDataChanged(DataBuffer newData, DataBuffer previousData)
        {
            try
            {
                // 이전 데이터가 없으면 변화 있음으로 처리
                if (previousData == null)
                {
                    Log($"🔍 변화 감지: 이전 데이터 없음 → 변화 있음 ({_Faci})");
                    return true;
                }
                
                // 데이터 항목 수가 다르면 변화 있음
                if (newData.Values.Count != previousData.Values.Count)
                {
                    Log($"🔍 변화 감지: 데이터 항목 수 변화 {previousData.Values.Count} → {newData.Values.Count} ({_Faci})");
                    return true;
                }
                
                // 🎯 카운터의 경우 최소값 기반 변화 감지 적용
                double tolerance = 0.001; // 0.1% 허용 오차
                double minChange = 0.5;   // 카운터용 최소 변화량 (0.5 이상 차이)
                foreach (var kvp in newData.Values)
                {
                    string key = kvp.Key;
                    double newValue = kvp.Value;
                    
                    if (previousData.Values.ContainsKey(key))
                    {
                        double oldValue = previousData.Values[key];
                        double difference = Math.Abs(newValue - oldValue);
                        double percentageChange = oldValue != 0 ? (difference / Math.Abs(oldValue)) : difference;
                        
                        // 🎯 카운터 데이터는 최소 변화량과 비율 변화 둘 다 체크
                        bool hasPercentageChange = percentageChange > tolerance;
                        bool hasAbsoluteChange = difference >= minChange;
                        bool isCounterData = key.ToLower().Contains("counter") || key.ToLower().Contains("카운트");
                        
                        // 카운터 데이터는 절대값 변화도 고려, 다른 데이터는 비율 변화만 고려
                        bool hasSignificantChange = isCounterData ? (hasAbsoluteChange || hasPercentageChange) : hasPercentageChange;
                        
                        if (hasSignificantChange)
                        {
                            string changeType = isCounterData ? $"절대값:{difference:F1}, 비율:{percentageChange:P2}" : $"비율:{percentageChange:P2}";
                            Log($"🔍 변화 감지: {key} 값 변화 {oldValue:F3} → {newValue:F3} ({changeType}) ({_Faci})");
                            return true;
                        }
                    }
                    else
                    {
                        Log($"🔍 변화 감지: 새로운 데이터 항목 '{key}' 추가 ({_Faci})");
                        return true;
                    }
                }
                
                Log($"🔍 변화 감지: 유의미한 변화 없음 (허용 오차: {tolerance:P1}) ({_Faci})");
                return false;
            }
            catch (Exception ex)
            {
                Log($"🚨 변화 감지 오류: {ex.Message} ({_Faci})");
                return true; // 오류 시 안전하게 변화 있음으로 처리
            }
        }
    }
}