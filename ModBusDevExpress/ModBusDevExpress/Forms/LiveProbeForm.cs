using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;

namespace ModBusDevExpress.Forms
{
    public class LiveProbeForm : Form
    {
        private readonly RichTextBox _logBox;
        private readonly Button _startButton;
        private readonly Button _stopButton;
        private readonly NumericUpDown _intervalUpDown;
        private readonly Label _statusLabel;

        private readonly List<DeviceProbe> _probes = new();
        private bool _isRunning = false;
        private CancellationTokenSource _cts;

        public LiveProbeForm()
        {
            Text = "라이브 데이터 모니터 (0.1초)";
            Width = 900;
            Height = 600;

            _startButton = new Button { Text = "시작", Left = 10, Top = 10, Width = 80 };
            _stopButton = new Button { Text = "중지", Left = 100, Top = 10, Width = 80, Enabled = false };
            _intervalUpDown = new NumericUpDown { Left = 200, Top = 12, Width = 80, Minimum = 50, Maximum = 1000, Value = 100, Increment = 10 };
            var intervalLabel = new Label { Left = 290, Top = 15, Width = 200, Text = "간격(ms) (기본 100)" };
            _statusLabel = new Label { Left = 520, Top = 15, Width = 340, Text = "대기 중" };

            _logBox = new RichTextBox { Left = 10, Top = 45, Width = 860, Height = 500, ReadOnly = true, WordWrap = false, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            Controls.Add(_startButton);
            Controls.Add(_stopButton);
            Controls.Add(_intervalUpDown);
            Controls.Add(intervalLabel);
            Controls.Add(_statusLabel);
            Controls.Add(_logBox);

            _startButton.Click += async (s, e) => await StartAsync();
            _stopButton.Click += (s, e) => Stop();

            FormClosed += (s, e) => Stop();
        }

        private async Task StartAsync()
        {
            try
            {
                Stop();
                _logBox.Clear();
                _isRunning = true;
                _cts = new CancellationTokenSource();
                _startButton.Enabled = false;
                _stopButton.Enabled = true;
                int intervalMs = (int)_intervalUpDown.Value;

                // 디바이스 설정 로드
                var devices = DeviceConfigManager.LoadDeviceSettings().Where(d => d.IsActive).ToList();
                if (devices.Count == 0)
                {
                    Append($"[{Now()}] 활성화된 디바이스가 없습니다.");
                    return;
                }

                // 요청된 요구사항: .96/.91 → 주소 30, .98/.99 → 주소 1003
                foreach (var d in devices)
                {
                    int addr = d.StartAddress; // 기본값으로 기존 설정 사용
                    try
                    {
                        var lastOctet = d.IPAddress?.Split('.')?.LastOrDefault();
                        if (lastOctet == "96" || lastOctet == "91") addr = 30;
                        else if (lastOctet == "98" || lastOctet == "99") addr = 1003;
                    }
                    catch { /* 안전 무시 */ }

                    var probe = new DeviceProbe(this, d, addr, intervalMs, _cts.Token);
                    _probes.Add(probe);
                }

                _statusLabel.Text = $"실행 중 - 디바이스 {_probes.Count}개, {intervalMs}ms 간격";

                // 연결 후 폴링 시작
                foreach (var p in _probes)
                {
                    await p.StartAsync();
                }
            }
            catch (Exception ex)
            {
                Append($"[{Now()}] 시작 오류: {ex.Message}");
                Stop();
            }
        }

        private void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            try { _cts?.Cancel(); } catch { }
            foreach (var p in _probes) p.Dispose();
            _probes.Clear();
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
            _statusLabel.Text = "중지";
        }

        internal void Append(string line)
        {
            try
            {
                if (IsDisposed || _logBox == null || _logBox.IsDisposed) return;
                if (InvokeRequired)
                {
                    if (!IsDisposed && IsHandleCreated)
                        BeginInvoke(new Action<string>(Append), line);
                    return;
                }
                _logBox.AppendText(line + Environment.NewLine);
            }
            catch (ObjectDisposedException)
            {
                // 폼/컨트롤이 닫히는 중이면 무시
            }
        }

        private static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

        private sealed class DeviceProbe : IDisposable
        {
            private readonly LiveProbeForm _owner;
            private ReliableModBusService _service;
            private readonly ModbusDeviceSettings _settings;
            private readonly int _address;
            private readonly System.Windows.Forms.Timer _timer;
            private bool _inFlight = false;
            private int _consecutiveFailures = 0;
            private readonly CancellationToken _token;

            public DeviceProbe(LiveProbeForm owner, ModbusDeviceSettings settings, int address, int intervalMs, CancellationToken token)
            {
                _owner = owner;
                _settings = settings; // 원본 설정을 그대로 사용 (키 동기화)
                _service = null; // 공유 레지스트리를 통해 획득
                _address = address;
                _timer = new System.Windows.Forms.Timer { Interval = Math.Max(50, intervalMs) };
                _timer.Tick += async (s, e) => await TickAsync();
                _token = token;
            }

            public async Task StartAsync()
            {
                // 공유 서비스 획득(모니터링과 동일 세션 사용)
                _service = await ServiceRegistry.GetOrCreateAsync(_settings);
                // 고속 폴링: 짧은 타임아웃으로 설정
                _service.ConfigureTimeouts(responseTimeoutMs: Math.Max(100, _timer.Interval * 2), connectTimeoutMs: 1000);
                bool ok = true; // 공유 서비스는 이미 연결 시도됨
                _owner.Append($"[{DateTime.Now:HH:mm:ss.fff}] 연결 {(ok ? "성공" : "실패")}: {_settings.DeviceName} {_settings.IPAddress}:{_settings.Port} → 번지 {_address}");
                _timer.Start();
            }

            private async Task TickAsync()
            {
                if (_token.IsCancellationRequested || _owner.IsDisposed) return;
                if (_inFlight) return;
                _inFlight = true;
                try
                {
                    if (_token.IsCancellationRequested || _owner.IsDisposed) return;
                    var data = await _service.ReadRegistersAsync(_address, 1);
                    if (data != null && data.Length > 0)
                    {
                        _owner.Append($"[{DateTime.Now:HH:mm:ss.fff}] {_settings.IPAddress}({_settings.DeviceName}) A:{_address} → {data[0]}");
                        _consecutiveFailures = 0;
                    }
                    else
                    {
                        _consecutiveFailures++;
                        _owner.Append($"[{DateTime.Now:HH:mm:ss.fff}] {_settings.IPAddress}({_settings.DeviceName}) A:{_address} → 읽기 실패({_consecutiveFailures})");

                        // 백오프: 3회 이상 연속 실패 시 잠시 간격 확대 후 자동 복구
                        if (_consecutiveFailures >= 3)
                        {
                            int old = _timer.Interval;
                            _timer.Interval = Math.Min(500, old + 100); // 최대 500ms까지 완화
                            _owner.Append($"[{DateTime.Now:HH:mm:ss.fff}] 백오프 적용: 간격 {_timer.Interval}ms");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _owner.Append($"[{DateTime.Now:HH:mm:ss.fff}] {_settings.IPAddress}({_settings.DeviceName}) 오류: {ex.Message}");
                }
                finally
                {
                    _inFlight = false;
                }
            }

            public void Dispose()
            {
                try { _timer.Stop(); _timer.Dispose(); } catch { }
                try { if (_service != null) ServiceRegistry.Release(_settings); } catch { }
            }
        }
    }
}


