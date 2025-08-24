using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using ModBusDevExpress.Models;

namespace ModBusDevExpress.Service
{
    /// <summary>
    /// HF2311S WiFi-RS485 컨버터를 위한 안정적인 ModBus 통신 서비스
    /// - 자동 재연결 메커니즘
    /// - 연결 상태 모니터링
    /// - 하트비트 및 백오프 전략
    /// - 큐 기반 안정적 데이터 처리
    /// </summary>
    public class ReliableModBusService : IDisposable
    {
        private readonly ModbusCtrl _modbus;
        private readonly ModbusDeviceSettings _deviceSettings;
        private CancellationTokenSource _cancellationTokenSource; // readonly 제거 - 재생성 가능
        private readonly SemaphoreSlim _connectionSemaphore;
        
        // 🔄 재연결 관리
        private readonly int[] _retryDelays = { 1000, 3000, 5000, 10000, 30000, 60000 }; // 백오프 전략
        private int _currentRetryIndex = 0;
        private bool _isConnected = false;
        private DateTime _lastSuccessfulConnection = DateTime.MinValue;
        private DateTime _lastHeartbeat = DateTime.MinValue;
        
        // 📊 통계
        private int _totalReconnects = 0;
        private int _totalErrors = 0;
        private int _successfulReads = 0;
        
        // 💓 하트비트 설정 (HF2311S WiFi-RS485 장기 연결 최적화)
        private readonly Timer _heartbeatTimer;
        private readonly Timer _reconnectTimer;
        private System.Threading.Timer _pollTimer; // 백그라운드 주기 수집
        private int _currentPollIntervalMs = 0;
        private int _initialOffsetMs = 0;
        private readonly object _pollGate = new object();
        private bool _isPolling = false;
        private const int HEARTBEAT_INTERVAL = 60000;  // 60초 (WiFi 안정성을 위해 증가)
        private const int CONNECTION_TIMEOUT = 10000;  // 10초 (WiFi 지연 고려)
        private const int MAX_IDLE_TIME = 300000;      // 5분 (300초) 무응답 시 재연결
        private const int HARDWARE_RESET_THRESHOLD = 900000; // 15분 (900초) 무응답 시 하드웨어 리셋 권고
        private const int PREVENTIVE_RESTART_INTERVAL = 1800000; // 30분 예방적 재시작
        
        // 🚨 485 통신 무응답 감지
        private int _consecutiveFailures = 0;
        private DateTime _lastSuccessfulRead = DateTime.Now;
        private DateTime _lastRemoteRebootAttempt = DateTime.MinValue;

        // ⚙️ 가벼운 자동 복구 설정
        private const int LightRecoveryStep1Failures = 2;  // 2회 연속: 타임아웃/주기 완화
        private const int LightRecoveryStep2Failures = 3;  // 3회 연속: 타이머 재시작 + 연결 새로고침
        private const int LightRecoveryStep3Failures = 6;  // 6회 연속: Modbus 스택 리셋
        private DateTime _lastLightRecovery = DateTime.MinValue;
        private const int LightRecoveryCooldownSec = 30;   // 최소 30초 쿨다운
        
        // 🔄 예방적 재시작
        private readonly Timer _preventiveRestartTimer;
        private bool _useShortLivedConnections = false;     // 매 폴링 Connect→Read→Close 모드
        private DateTime _shortLivedModeUntil = DateTime.MinValue; // 자동 해제 시각
        private DateTime _lastPreventiveRestart = DateTime.Now;
        
        // 🔄 연결 풀링 (연결 고착화 방지)
        private int _connectionRefreshCounter = 0;
        private const int CONNECTION_REFRESH_THRESHOLD = 50; // 50회 읽기마다 연결 새로 고침
        
        // 📡 데이터 수집 큐
        private readonly ConcurrentQueue<ModBusDataRequest> _dataRequestQueue;
        private readonly ConcurrentQueue<ModBusDataResult> _dataResultQueue;
        private Task _dataProcessingTask;
        
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;
        public event EventHandler<ModBusDataEventArgs> DataReceived;
        public event EventHandler<ModBusErrorEventArgs> ErrorOccurred;

        public ReliableModBusService(ModbusDeviceSettings deviceSettings)
        {
            _deviceSettings = deviceSettings ?? throw new ArgumentNullException(nameof(deviceSettings));
            _modbus = new ModbusCtrl();
            _cancellationTokenSource = new CancellationTokenSource();
            _connectionSemaphore = new SemaphoreSlim(1, 1);
            
            _dataRequestQueue = new ConcurrentQueue<ModBusDataRequest>();
            _dataResultQueue = new ConcurrentQueue<ModBusDataResult>();
            
            // 🔄 하트비트 타이머 설정 (연결 상태 지속 모니터링)
            _heartbeatTimer = new Timer(PerformHeartbeat, null, Timeout.Infinite, HEARTBEAT_INTERVAL);
            _reconnectTimer = new Timer(AttemptReconnect, null, Timeout.Infinite, Timeout.Infinite);
            
            // 🔄 예방적 재시작 타이머 (1시간마다 TCP 연결 새로 고침)
            _preventiveRestartTimer = new Timer(PerformPreventiveRestart, null, PREVENTIVE_RESTART_INTERVAL, PREVENTIVE_RESTART_INTERVAL);
            
            // 📊 백그라운드 데이터 처리 시작 (안정적 큐 기반 처리)
            _dataProcessingTask = Task.Run(ProcessDataQueue, _cancellationTokenSource.Token);
            
            LogMessage($"🚀 ReliableModBusService 초기화 완료 - 디바이스: {_deviceSettings.DeviceName}");
        }

        /// <summary>
        /// 백그라운드 수집 시작 (장치 설정의 Interval/StartAddress/DataLength 사용)
        /// </summary>
        private void StartPollingIfNeeded()
        {
            if (_pollTimer != null) return;
            int intervalMs = Math.Max(1000, _deviceSettings.Interval * 1000);
            _currentPollIntervalMs = intervalMs;
            _initialOffsetMs = ComputeInitialOffsetMs(intervalMs);
            _pollTimer = new System.Threading.Timer(async _ =>
            {
                // 단기 연결 모드에서는 _isConnected 여부에 관계없이 1회성 연결을 사용
                if (!_useShortLivedConnections && !_isConnected) return;
                if (_cancellationTokenSource.Token.IsCancellationRequested) return;
                if (_isPolling) return;
                lock (_pollGate)
                {
                    if (_isPolling) return;
                    _isPolling = true;
                }
                try
                {
                    // 장치 설정을 기준으로 자동 수집
                    if (_useShortLivedConnections)
                    {
                        await PollOnceWithTransientConnectionAsync(_deviceSettings.StartAddress, _deviceSettings.DataLength);
                    }
                    else
                    {
                        await ReadRegistersAsync(_deviceSettings.StartAddress, _deviceSettings.DataLength);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"🚨 폴링 오류: {ex.Message}");
                }
                finally
                {
                    _isPolling = false;
                }
            }, null, _initialOffsetMs, intervalMs); // 첫 주기 오프셋 실행
            LogMessage($"▶️ 폴링 시작 - 주기: {intervalMs}ms, 오프셋: {_initialOffsetMs}ms, 주소: {_deviceSettings.StartAddress}, 길이: {_deviceSettings.DataLength}");
        }

        private void StopPolling()
        {
            try
            {
                _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _pollTimer?.Dispose();
                _pollTimer = null;
            _currentPollIntervalMs = 0;
            }
            catch { }
        }

        /// <summary>
        /// Modbus 통신 타임아웃을 런타임에 조정 (고속 폴링 진단용)
        /// </summary>
        public void ConfigureTimeouts(int responseTimeoutMs = 1000, int connectTimeoutMs = 1000)
        {
            try
            {
                _modbus.ResponseTimeout = Math.Clamp(responseTimeoutMs, 100, 30000);
                _modbus.ConnectTimeout = Math.Clamp(connectTimeoutMs, 100, 30000);
                LogMessage($"⏱️ 타임아웃 설정 - Response:{_modbus.ResponseTimeout}ms Connect:{_modbus.ConnectTimeout}ms");
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 타임아웃 설정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔁 런타임에 폴링 주기(초)를 변경. 이미 실행 중이면 즉시 적용
        /// </summary>
        public void SetPollingIntervalSeconds(int seconds)
        {
            int newInterval = Math.Max(1000, seconds * 1000);
            if (newInterval == _currentPollIntervalMs && _pollTimer != null) return;

            _currentPollIntervalMs = newInterval;

            if (_pollTimer != null)
            {
                try
                {
                    _pollTimer.Change(_initialOffsetMs, newInterval);
                    LogMessage($"⏱️ 폴링 주기 변경: {newInterval}ms (오프셋 유지 {_initialOffsetMs}ms)");
                }
                catch (ObjectDisposedException)
                {
                    // 타이머가 이미 정리되었으면 다시 시작 시점에 반영됨
                }
            }
            else
            {
                // 아직 시작되지 않았으면 연결 시점에 적용됨
            }
        }

        // 📏 디바이스별 초기 오프셋 계산 (트래픽 분산)
        private int ComputeInitialOffsetMs(int intervalMs)
        {
            try
            {
                int baseOffset = 0;
                if (!string.IsNullOrEmpty(_deviceSettings.IPAddress))
                {
                    var parts = _deviceSettings.IPAddress.Split('.');
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int last))
                    {
                        baseOffset = (last * 47) % 801; // 0~800ms
                    }
                }
                baseOffset = (baseOffset + (_deviceSettings.SlaveId * 13)) % 801;
                // 주기보다 크지 않도록 안전 클램프
                int safeMax = Math.Max(10, intervalMs - 50);
                return Math.Min(baseOffset, safeMax);
            }
            catch
            {
                return Math.Min(100, Math.Max(10, intervalMs / 20)); // 기본 5% 정도
            }
        }

        /// <summary>
        /// 단기 연결 모드: 매 폴링마다 Connect → Read → Close 수행
        /// </summary>
        private async Task<ushort[]> PollOnceWithTransientConnectionAsync(int startAddress, int count)
        {
            try
            {
                // 연결 시도
                var result = _modbus.Connect(_deviceSettings.IPAddress, _deviceSettings.Port);
                if (result != Result.SUCCESS)
                {
                    _totalErrors++;
                    _consecutiveFailures++;
                    LogMessage($"🔌 단기연결 Connect 실패: {result}");
                    TryApplyLightRecovery();
                    return null;
                }

                // 읽기
                ushort[] uRegisters = null;
                using (var timeoutCts = new CancellationTokenSource(15000))
                {
                    await Task.Run(() =>
                    {
                        short[] registers = new short[count];
                        var readRes = _modbus.ReadInputRegisters((byte)_deviceSettings.SlaveId, (ushort)startAddress, (ushort)count, registers);
                        if (readRes == Result.SUCCESS)
                        {
                            uRegisters = new ushort[registers.Length];
                            for (int i = 0; i < registers.Length; i++) uRegisters[i] = (ushort)registers[i];
                        }
                        else
                        {
                            LogMessage($"📖 단기연결 읽기 실패: {readRes}");
                        }
                    }, timeoutCts.Token);
                }

                if (uRegisters != null)
                {
                    _successfulReads++;
                    _lastHeartbeat = DateTime.Now;
                    _lastSuccessfulRead = DateTime.Now;
                    _consecutiveFailures = 0;
                    _connectionRefreshCounter = 0;
                    OnDataReceived(startAddress, uRegisters);
                    // 안정화되면 자동 해제
                    if (_useShortLivedConnections && DateTime.Now > _shortLivedModeUntil)
                    {
                        _useShortLivedConnections = false;
                        LogMessage("🔁 단기 연결 모드 자동 해제");
                    }
                }
                else
                {
                    _totalErrors++;
                    _consecutiveFailures++;
                    TryApplyLightRecovery();
                }

                return uRegisters;
            }
            catch (Exception ex)
            {
                _totalErrors++;
                _consecutiveFailures++;
                LogMessage($"📖 단기연결 예외: {ex.Message}");
                TryApplyLightRecovery();
                return null;
            }
            finally
            {
                try { _modbus.Close(); } catch { }
            }
        }

        /// <summary>
        /// 🔌 비동기 연결 시작
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            return await ConnectInternalAsync(isReconnect: false);
        }

        /// <summary>
        /// 🔌 내부 연결 로직 (재연결 지원)
        /// </summary>
        private async Task<bool> ConnectInternalAsync(bool isReconnect)
        {
            await _connectionSemaphore.WaitAsync();
            try
            {
                if (_isConnected && !isReconnect)
                {
                    LogMessage("✅ 이미 연결된 상태입니다.");
                    return true;
                }

                LogMessage($"🔗 ModBus 연결 시도 중... ({_deviceSettings.IPAddress}:{_deviceSettings.Port})");
                
                // 기존 연결 해제
                if (_isConnected)
                {
                    _modbus.Close();
                    _isConnected = false;
                }

                // 네트워크 ping 테스트
                if (!await IsNetworkReachableAsync())
                {
                    LogMessage("❌ 네트워크 연결 불가 - HF2311S 장치에 도달할 수 없습니다.");
                    return false;
                }

                // ModBus 연결 시도
                using (var timeoutCts = new CancellationTokenSource(CONNECTION_TIMEOUT))
                {
                    await Task.Run(() => 
                    {
                        var result = _modbus.Connect(_deviceSettings.IPAddress, _deviceSettings.Port);
                        if (result == Result.SUCCESS)
                        {
                            _isConnected = true;
                            _lastSuccessfulConnection = DateTime.Now;
                            _lastHeartbeat = DateTime.Now;
                            _currentRetryIndex = 0; // 성공 시 재시도 인덱스 초기화
                            
                            if (isReconnect)
                                _totalReconnects++;
                        }
                    }, timeoutCts.Token);
                }

                if (_isConnected)
                {
                    LogMessage($"✅ ModBus 연결 성공! (재연결: {_totalReconnects}회)");
                    
                    // 하트비트 타이머 시작 (연결 상태 지속 감시)
                    _heartbeatTimer.Change(HEARTBEAT_INTERVAL, HEARTBEAT_INTERVAL);
                    // 폴링 시작
                    StartPollingIfNeeded();
                    
                    // 연결 상태 이벤트 발생
                    OnConnectionStatusChanged(true, $"연결 성공 - {_deviceSettings.DeviceName}");
                    
                    return true;
                }
                else
                {
                    LogMessage($"❌ ModBus 연결 실패 - 재시도 대기 중...");
                    ScheduleReconnect();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _totalErrors++;
                LogMessage($"🚨 연결 중 예외 발생: {ex.Message}");
                OnErrorOccurred("연결 오류", ex);
                ScheduleReconnect();
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// 🌐 네트워크 연결 상태 확인 (HF2311S 장치 ping)
        /// </summary>
        private async Task<bool> IsNetworkReachableAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(_deviceSettings.IPAddress, 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 💓 하트비트 수행 (HF2311S WiFi-RS485 장기 연결 안정성 강화)
        /// </summary>
        private async void PerformHeartbeat(object state)
        {
            if (!_isConnected || _cancellationTokenSource.Token.IsCancellationRequested)
                return;

            try
            {
                // 🔍 무응답 시간 체크 (5분 이상 무응답 시 강제 재연결)
                var timeSinceLastHeartbeat = DateTime.Now - _lastHeartbeat;
                if (timeSinceLastHeartbeat.TotalMilliseconds > MAX_IDLE_TIME)
                {
                    LogMessage($"💔 장기간 무응답 감지 ({timeSinceLastHeartbeat.TotalMinutes:F1}분) - 강제 재연결");
                    await HandleConnectionLoss();
                    return;
                }

                // 🔄 TCP Keep-Alive 방식의 가벼운 연결 확인 (실제 데이터 읽기 대신 ping)
                bool networkReachable = await IsNetworkReachableAsync();
                if (!networkReachable)
                {
                    LogMessage("💔 네트워크 연결 끊김 감지 - 재연결 필요");
                    await HandleConnectionLoss();
                    return;
                }

                // 🎯 실제 ModBus 통신 테스트 (매우 간단한 읽기)
                try
                {
                    using (var timeoutCts = new CancellationTokenSource(5000)) // 5초 타임아웃
                    {
                        var testResult = await Task.Run(() => 
                        {
                            // 가장 안전한 방법: 연결된 슬레이브의 첫 번째 레지스터 읽기
                            short[] testRegisters = new short[1];
                            return _modbus.ReadInputRegisters((byte)_deviceSettings.SlaveId, 0, 1, testRegisters);
                        }, timeoutCts.Token);

                        if (testResult == Result.SUCCESS)
                        {
                            _lastHeartbeat = DateTime.Now;
                            LogMessage($"💓 하트비트 정상 - 연결 지속: {GetConnectionDuration()} (슬레이브ID: {_deviceSettings.SlaveId})");
                        }
                        else
                        {
                            LogMessage($"💔 ModBus 응답 실패 (결과: {testResult}) - 재연결 필요");
                            await HandleConnectionLoss();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogMessage("💔 하트비트 타임아웃 - 재연결 필요");
                    await HandleConnectionLoss();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"💔 하트비트 오류: {ex.Message}");
                await HandleConnectionLoss();
            }
        }

        /// <summary>
        /// 🔄 연결 재시도 스케줄링 (백오프 전략)
        /// </summary>
        private void ScheduleReconnect()
        {
            // 🔧 CancellationTokenSource 상태 안전 체크
            if (_cancellationTokenSource?.Token.IsCancellationRequested == true || _cancellationTokenSource == null)
            {
                LogMessage("🔄 CancellationTokenSource 재생성 중...");
                RecreateCancellationToken();
            }

            var delay = _retryDelays[Math.Min(_currentRetryIndex, _retryDelays.Length - 1)];
            _currentRetryIndex = Math.Min(_currentRetryIndex + 1, _retryDelays.Length - 1);
            
            LogMessage($"⏰ {delay/1000}초 후 재연결 시도 예정 (시도 {_currentRetryIndex}/{_retryDelays.Length})");
            
            _reconnectTimer.Change(delay, Timeout.Infinite);
        }

        /// <summary>
        /// 🔧 CancellationTokenSource 안전 재생성
        /// </summary>
        private void RecreateCancellationToken()
        {
            try
            {
                // 기존 토큰 안전하게 해제
                if (_cancellationTokenSource != null)
                {
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                    _cancellationTokenSource.Dispose();
                }

                // 새로운 토큰 생성
                _cancellationTokenSource = new CancellationTokenSource();
                LogMessage("✅ CancellationTokenSource 재생성 완료");
            }
            catch (Exception ex)
            {
                LogMessage($"🚨 CancellationTokenSource 재생성 오류: {ex.Message}");
                // 최후 수단: 새 인스턴스 강제 생성
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// 🔧 안전한 CancellationToken 가져오기
        /// </summary>
        private CancellationToken GetSafeToken()
        {
            try
            {
                if (_cancellationTokenSource?.Token.IsCancellationRequested != false)
                {
                    RecreateCancellationToken();
                }
                return _cancellationTokenSource.Token;
            }
            catch
            {
                // 오류 시 새 토큰 생성
                _cancellationTokenSource = new CancellationTokenSource();
                return _cancellationTokenSource.Token;
            }
        }

        /// <summary>
        /// 🔧 토큰 취소 요청 여부 안전 체크
        /// </summary>
        private bool IsCancellationRequested()
        {
            try
            {
                return _cancellationTokenSource?.Token.IsCancellationRequested == true;
            }
            catch
            {
                return true; // 오류 시 취소된 것으로 간주
            }
        }

        /// <summary>
        /// 🔄 자동 재연결 시도
        /// </summary>
        private async void AttemptReconnect(object state)
        {
            LogMessage("🔄 자동 재연결 시도 중...");
            
            // 🔧 안전한 토큰 상태 체크
            if (IsCancellationRequested())
            {
                RecreateCancellationToken();
            }
            
            await ConnectInternalAsync(isReconnect: true);
        }

        /// <summary>
        /// 🔄 예방적 재시작 (WiFi-RS485 컨버터 장기 운영 안정성 확보)
        /// </summary>
        private async void PerformPreventiveRestart(object state)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            var timeSinceLastRestart = DateTime.Now - _lastPreventiveRestart;
            
            // 1시간 이상 연결이 지속되었고, 현재 연결 상태인 경우에만 실행
            if (timeSinceLastRestart.TotalMilliseconds >= PREVENTIVE_RESTART_INTERVAL && _isConnected)
            {
                LogMessage($"🔄 예방적 재시작 실행 - {_deviceSettings.DeviceName} (연결 지속: {timeSinceLastRestart.TotalHours:F1}시간)");
                LogMessage("   💡 WiFi-RS485 컨버터 장기 운영 안정성을 위한 예방 조치");
                
                _lastPreventiveRestart = DateTime.Now;
                
                // 부드러운 재연결 (기존 연결을 정리하고 새로 연결)
                await HandleConnectionLoss();
            }
        }

        /// <summary>
        /// 🔄 연결 새로 고침 (연결 고착화 방지)
        /// </summary>
        private async Task RefreshConnection()
        {
            if (!_isConnected || _cancellationTokenSource.Token.IsCancellationRequested)
                return;

            LogMessage($"🔄 연결 새로 고침 시작 - {_deviceSettings.DeviceName}");
            
            try
            {
                // 기존 연결을 부드럽게 정리하고 새로 연결
                _modbus?.Close();
                await Task.Delay(1000); // 1초 대기 (WiFi-RS485 컨버터 정리 시간)
                
                // 새로운 연결 시도
                var result = _modbus.Connect(_deviceSettings.IPAddress, _deviceSettings.Port);
                if (result == Result.SUCCESS)
                {
                    LogMessage($"✅ 연결 새로 고침 성공 - {_deviceSettings.DeviceName}");
                }
                else
                {
                    LogMessage($"❌ 연결 새로 고침 실패 - {_deviceSettings.DeviceName}: {result}");
                    await HandleConnectionLoss();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"🚨 연결 새로 고침 중 오류 - {_deviceSettings.DeviceName}: {ex.Message}");
                await HandleConnectionLoss();
            }
        }

        /// <summary>
        /// 💔 연결 손실 처리 (HF2311S WiFi-RS485 안정성 강화)
        /// </summary>
        private async Task HandleConnectionLoss()
        {
            if (!_isConnected)
                return;

            LogMessage($"🚨 연결 손실 감지 - 디바이스: {_deviceSettings.DeviceName} (IP: {_deviceSettings.IPAddress})");

            _isConnected = false;
            _heartbeatTimer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            
            // 🔄 ModBus 연결 정리 (중요: WiFi-RS485에서 깔끔한 종료 필요)
            try
            {
                _modbus?.Close();
                LogMessage("🔄 ModBus 연결 정리 완료");
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ ModBus 연결 정리 중 오류: {ex.Message}");
            }
            
            OnConnectionStatusChanged(false, $"연결 끊김 - {_deviceSettings.DeviceName} 재연결 시도 중...");
            
            // 🎯 WiFi-RS485 특성 고려한 지연 후 재연결 (즉시 재연결하면 충돌 가능)
            await Task.Delay(2000); // 2초 대기
            
            // 비동기 재연결 시도
            await Task.Run(() => ScheduleReconnect());
        }

        /// <summary>
        /// 📖 레지스터 읽기 (HF2311S WiFi-RS485 타임아웃 및 안정성 강화)
        /// </summary>
        public async Task<ushort[]> ReadRegistersAsync(int startAddress, int count)
        {
            if (!_isConnected)
            {
                LogMessage($"❌ 연결되지 않은 상태에서 읽기 시도 - {_deviceSettings.DeviceName}");
                return null;
            }

            try
            {
                // 🔄 타임아웃 설정 (WiFi 지연 고려)
                using (var timeoutCts = new CancellationTokenSource(15000)) // 15초 타임아웃
                {
                    var data = await Task.Run(() =>
                    {
                        short[] registers = new short[count];
                        var result = _modbus.ReadInputRegisters((byte)_deviceSettings.SlaveId, (ushort)startAddress, (ushort)count, registers);
                        
                        if (result == Result.SUCCESS)
                        {
                            // short[]를 ushort[]로 변환
                            ushort[] uRegisters = new ushort[registers.Length];
                            for (int i = 0; i < registers.Length; i++)
                            {
                                uRegisters[i] = (ushort)registers[i];
                            }
                            
                            _successfulReads++;
                            _lastHeartbeat = DateTime.Now; // 성공적인 읽기 시 하트비트 업데이트
                            _lastSuccessfulRead = DateTime.Now; // 485 통신 성공 시간 업데이트
                            _consecutiveFailures = 0; // 연속 실패 카운터 리셋
                            
                            // 🔄 연결 풀링: 일정 횟수마다 연결 새로 고침 (연결 고착화 방지)
                            _connectionRefreshCounter++;
                            if (_connectionRefreshCounter >= CONNECTION_REFRESH_THRESHOLD)
                            {
                                LogMessage($"🔄 연결 새로 고침 예약 - {_deviceSettings.DeviceName} ({CONNECTION_REFRESH_THRESHOLD}회 읽기 완료)");
                                _connectionRefreshCounter = 0;
                                
                                // 비동기로 연결 새로 고침 (현재 요청은 정상 처리)
                                _ = Task.Run(async () => 
                                {
                                    await Task.Delay(5000); // 5초 후 실행
                                    await RefreshConnection();
                                });
                            }
                            
                            OnDataReceived(startAddress, uRegisters);
                            return uRegisters;
                        }
                        else
                        {
                            _totalErrors++;
                            _consecutiveFailures++;
                            LogMessage($"📖 ModBus 읽기 실패 - {_deviceSettings.DeviceName}: {result} (슬레이브ID: {_deviceSettings.SlaveId}, 주소: {startAddress}, 길이: {count}, 연속실패: {_consecutiveFailures})");
                            
                            // 🔍 상세 오류 분석
                            string errorDetail = GetModBusErrorDetail(result);
                            LogMessage($"   🔍 오류 상세: {errorDetail}");
                            
                            // 빠른 재시도를 위해 아주 짧은 대기(스파이크 흡수)
                            Thread.Sleep(10);

                            // ⚙️ 가벼운 자동 복구 단계 적용 (쿨다운 고려)
                            TryApplyLightRecovery();
                            return null;
                        }
                    }, timeoutCts.Token);
                    
                    // 🚨 Task.Run 외부에서 485 통신 실패 처리
                    if (data == null && _consecutiveFailures > 0)
                    {
                        await HandleRS485CommunicationFailure();
                    }
                    
                    return data;
                }
            }
            catch (OperationCanceledException)
            {
                _totalErrors++;
                _consecutiveFailures++;
                LogMessage($"📖 읽기 타임아웃 - {_deviceSettings.DeviceName} (15초 초과, 연속실패: {_consecutiveFailures})");
                OnErrorOccurred("읽기 타임아웃", new TimeoutException("ModBus 읽기 타임아웃"));
                
                // 🚨 타임아웃은 485 통신 문제일 가능성이 높음
                await HandleRS485CommunicationFailure();
                TryApplyLightRecovery();
                return null;
            }
            catch (Exception ex)
            {
                _totalErrors++;
                _consecutiveFailures++;
                LogMessage($"📖 읽기 예외 - {_deviceSettings.DeviceName}: {ex.Message} (연속실패: {_consecutiveFailures})");
                OnErrorOccurred("데이터 읽기 오류", ex);
                
                // 🚨 485 통신 실패 처리
                await HandleRS485CommunicationFailure();
                TryApplyLightRecovery();
                return null;
            }
        }

        /// <summary>
        /// 🔄 WiFi-RS485 컨버터 원격 리부팅 시도
        /// </summary>
        private async Task AttemptRemoteReboot()
        {
            LogMessage($"🔄 원격 리부팅 시작 - {_deviceSettings.DeviceName} ({_deviceSettings.IPAddress})");
            
            try
            {
                // HTTP 클라이언트로 컨버터 웹 인터페이스 접근
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 1단계: 일반적인 리부팅 URL들 시도
                    string[] rebootUrls = {
                        $"http://{_deviceSettings.IPAddress}/reboot.cgi",
                        $"http://{_deviceSettings.IPAddress}/reboot",
                        $"http://{_deviceSettings.IPAddress}/admin/reboot",
                        $"http://{_deviceSettings.IPAddress}/cgi-bin/reboot",
                        $"http://{_deviceSettings.IPAddress}/system/reboot"
                    };
                    
                    foreach (string url in rebootUrls)
                    {
                        try
                        {
                            LogMessage($"🔗 시도: {url}");
                            var response = await httpClient.GetAsync(url);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                LogMessage($"✅ 원격 리부팅 성공 - {url}");
                                LogMessage($"⏳ 컨버터 재시작 대기 중... (30초)");
                                await Task.Delay(30000); // 30초 대기
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"⚠️ {url} 실패: {ex.Message}");
                        }
                    }
                    
                    // 2단계: POST 방식으로 리부팅 시도
                    try
                    {
                        var postData = new System.Net.Http.StringContent("action=reboot", 
                            System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                        var response = await httpClient.PostAsync($"http://{_deviceSettings.IPAddress}/", postData);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            LogMessage($"✅ POST 방식 원격 리부팅 성공");
                            await Task.Delay(30000);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"⚠️ POST 리부팅 실패: {ex.Message}");
                    }
                    
                    LogMessage($"❌ 모든 원격 리부팅 방법 실패 - {_deviceSettings.DeviceName}");
                    LogMessage($"💡 수동 전원 재시작이 필요할 수 있습니다.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"🚨 원격 리부팅 중 오류 - {_deviceSettings.DeviceName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔍 ModBus 오류 코드 상세 분석
        /// </summary>
        private string GetModBusErrorDetail(Result result)
        {
            return result switch
            {
                Result.SUCCESS => "성공",
                Result.ILLEGAL_FUNCTION => "지원하지 않는 기능 코드",
                Result.ILLEGAL_DATA_ADDRESS => "잘못된 레지스터 주소 - 장치에서 지원하지 않는 주소",
                Result.ILLEGAL_DATA_VALUE => "잘못된 데이터 값",
                Result.SLAVE_DEVICE_FAILURE => "슬레이브 장치 내부 오류",
                Result.ACKNOWLEDGE => "확인 응답 - 처리 중",
                Result.SLAVE_DEVICE_BUSY => "슬레이브 장치 사용 중",
                Result.NEGATIVE_ACKNOWLEDGE => "부정 확인 응답",
                Result.MEMORY_PARITY_ERROR => "메모리 패리티 오류",
                Result.GATEWAY_PATH_UNAVAILABLE => "게이트웨이 경로 사용 불가",
                Result.GATEWAY_DEVICE_FAILED => "게이트웨이 대상 장치 응답 실패",
                Result.CONNECT_ERROR => "연결 오류",
                Result.CONNECT_TIMEOUT => "연결 타임아웃",
                Result.WRITE => "쓰기 오류",
                Result.READ => "읽기 오류",
                Result.RESPONSE_TIMEOUT => "응답 타임아웃 - 슬레이브 장치 응답 지연 또는 RS485 통신 문제",
                Result.ISCLOSED => "연결이 닫혀있음",
                Result.CRC => "CRC 오류",
                Result.RESPONSE => "예상하지 못한 응답 - 슬레이브 장치가 잘못된 응답",
                Result.BYTECOUNT => "바이트 카운트 오류",
                Result.QUANTITY => "수량 범위 초과",
                Result.FUNCTION => "기능 코드 오류",
                Result.TRANSACTIONID => "트랜잭션 ID 불일치",
                Result.DEMO_TIMEOUT => "데모 타임아웃",
                _ => $"알 수 없는 오류 코드: {result}"
            };
        }

        /// <summary>
        /// 🚨 RS485 통신 실패 처리 (ModBus 스택 좀비 상태 해결)
        /// </summary>
        private async Task HandleRS485CommunicationFailure()
        {
            var timeSinceLastSuccess = DateTime.Now - _lastSuccessfulRead;
            
            // 🔥 즉시 ModBus 스택 리셋 시도 (3회 실패 시)
            if (_consecutiveFailures >= 3)
            {
                LogMessage($"🔥 ModBus 스택 좀비 상태 감지 - {_deviceSettings.DeviceName} (연속실패: {_consecutiveFailures})");
                await PerformModBusStackReset();
            }
            
            // 📊 연속 실패가 10회 이상이고, 마지막 성공 후 3분 이상 경과한 경우  
            if (_consecutiveFailures >= 10 && timeSinceLastSuccess.TotalMilliseconds > 180000) // 3분
            {
                LogMessage($"🚨 ModBus 스택 완전 정지 감지 - {_deviceSettings.DeviceName}");
                LogMessage($"   📊 연속 실패: {_consecutiveFailures}회, 마지막 성공: {timeSinceLastSuccess.TotalMinutes:F1}분 전");
                
                // 🔄 강력한 연결 리셋 (TCP + ModBus 스택 전체 재시작)
                await PerformDeepReset();
            }
            
            // 🔄 7분 무응답 시 WiFi-RS485 컨버터 원격 리부팅 시도
            if (timeSinceLastSuccess.TotalMilliseconds > 300000) // 5분
            {
                var timeSinceLastReboot = DateTime.Now - _lastRemoteRebootAttempt;
                if (timeSinceLastReboot.TotalMinutes >= 30) // 30분 간격으로만 시도
                {
                    LogMessage($"🔄 WiFi-RS485 컨버터 원격 리부팅 시도 - {_deviceSettings.DeviceName} ({timeSinceLastSuccess.TotalMinutes:F1}분간 무응답)");
                    await AttemptRemoteReboot();
                    _lastRemoteRebootAttempt = DateTime.Now;
                }
            }
            
            // 🚨 12분 이상 무응답 시 하드웨어 리셋 권고 (원격 리부팅 후에도 실패)
            if (timeSinceLastSuccess.TotalMilliseconds > 720000) // 12분
            {
                string alertMessage = $"⚠️ 하드웨어 리셋 권고: {_deviceSettings.DeviceName}\n" +
                                    $"📊 마지막 성공: {timeSinceLastSuccess.TotalMinutes:F0}분 전\n" +
                                    $"🔧 전력계/카운터 전원을 껐다 켜주세요.\n" +
                                    $"💡 또는 WiFi-RS485 컨버터 수동 전원 재시작 필요.";
                
                LogMessage(alertMessage);
                OnErrorOccurred("하드웨어 리셋 필요", new Exception(alertMessage));
                
                // 30분에 한 번만 알림 (스팸 방지)
                if (_consecutiveFailures % 30 == 0)
                {
                    OnConnectionStatusChanged(false, $"🚨 {_deviceSettings.DeviceName} 하드웨어 리셋 필요 (전원 껐다 켜기)");
                }
            }
        }

        /// <summary>
        /// ⚙️ 가벼운 자동 복구 단계 적용 (연속 실패 카운트 기반, 쿨다운 30초)
        /// </summary>
        private void TryApplyLightRecovery()
        {
            var now = DateTime.Now;
            if ((now - _lastLightRecovery).TotalSeconds < LightRecoveryCooldownSec)
                return;

            try
            {
                if (_consecutiveFailures >= LightRecoveryStep3Failures)
                {
                    _lastLightRecovery = now;
                    LogMessage("🧰 LightRecovery#3: Modbus 스택 리셋 시도");
                    _ = Task.Run(async () => await PerformModBusStackReset());
                }
                else if (_consecutiveFailures >= LightRecoveryStep2Failures)
                {
                    _lastLightRecovery = now;
                    LogMessage("🧰 LightRecovery#2: 타이머 재시작 + 연결 새로고침");
                    try { _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
                    try { _pollTimer?.Change(_initialOffsetMs, _currentPollIntervalMs); } catch { }
                    _ = Task.Run(async () => await RefreshConnection());
                    // 10분간 단기 연결 모드로 전환하여 세션 고착화 우회
                    _useShortLivedConnections = true;
                    _shortLivedModeUntil = DateTime.Now.AddMinutes(10);
                    LogMessage("🔁 단기 연결 모드 활성화(10분)");
                }
                else if (_consecutiveFailures >= LightRecoveryStep1Failures)
                {
                    _lastLightRecovery = now;
                    LogMessage("🧰 LightRecovery#1: 타임아웃/주기 완화 (임시)");
                    // 응답 타임아웃 +500ms (최대 3000ms)
                    int newResponseTimeout = Math.Min(3000, _modbus.ResponseTimeout + 500);
                    ConfigureTimeouts(responseTimeoutMs: newResponseTimeout, connectTimeoutMs: _modbus.ConnectTimeout);
                    // 폴링 주기 +200ms (주기보다 크지 않게)
                    int newInterval = Math.Min(_currentPollIntervalMs + 200, _currentPollIntervalMs + 1000);
                    SetPollingIntervalSeconds(Math.Max(1, newInterval / 1000));
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ LightRecovery 적용 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 ModBus 스택 리셋 (소프트웨어적 좀비 상태 해결)
        /// </summary>
        private async Task PerformModBusStackReset()
        {
            LogMessage($"🔥 ModBus 스택 리셋 시작 - {_deviceSettings.DeviceName}");
            
            try
            {
                // 1️⃣ 기존 ModBus 연결 강제 종료
                _modbus?.Close();
                LogMessage("   🔸 1단계: ModBus 연결 강제 종료");
                
                // 2️⃣ 소켓 정리 대기 (중요: OS 레벨 소켓 정리 시간 확보)
                await Task.Delay(2000);
                LogMessage("   🔸 2단계: 소켓 정리 대기 (2초)");
                
                // 3️⃣ 새로운 ModBus 연결 시도
                var result = _modbus.Connect(_deviceSettings.IPAddress, _deviceSettings.Port);
                if (result == Result.SUCCESS)
                {
                    LogMessage($"   ✅ 3단계: ModBus 스택 리셋 성공 - {_deviceSettings.DeviceName}");
                    _consecutiveFailures = Math.Max(0, _consecutiveFailures - 3); // 성공 시 일부 실패 카운터 감소
                }
                else
                {
                    LogMessage($"   ❌ 3단계: ModBus 스택 리셋 실패 - {_deviceSettings.DeviceName}: {result}");
                    LogMessage($"   🔍 오류 상세: {GetModBusErrorDetail(result)}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"🚨 ModBus 스택 리셋 중 예외 - {_deviceSettings.DeviceName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 🚨 Deep Reset (TCP + ModBus 완전 재시작)
        /// </summary>
        private async Task PerformDeepReset()
        {
            LogMessage($"🚨 Deep Reset 시작 - {_deviceSettings.DeviceName} (좀비 상태 완전 해결)");
            
            try
            {
                // 1️⃣ 완전한 연결 해제
                _isConnected = false;
                _modbus?.Close();
                LogMessage("   🔸 1단계: 연결 완전 해제");
                
                // 2️⃣ 긴 대기 시간 (WiFi-RS485 컨버터 내부 상태 정리)
                await Task.Delay(5000); // 5초
                LogMessage("   🔸 2단계: 장치 상태 정리 대기 (5초)");
                
                // 3️⃣ 네트워크 연결성 재확인
                bool networkOk = await IsNetworkReachableAsync();
                LogMessage($"   🔸 3단계: 네트워크 상태 확인 - {(networkOk ? "정상" : "비정상")}");
                
                if (!networkOk)
                {
                    LogMessage("   ❌ 네트워크 연결 실패 - Deep Reset 중단");
                    return;
                }
                
                // 4️⃣ 새로운 ModBus 연결 (완전 초기화)
                var result = _modbus.Connect(_deviceSettings.IPAddress, _deviceSettings.Port);
                if (result == Result.SUCCESS)
                {
                    _isConnected = true;
                    _lastSuccessfulConnection = DateTime.Now;
                    _lastHeartbeat = DateTime.Now;
                    _consecutiveFailures = 0; // 완전 리셋
                    
                    LogMessage($"   ✅ 4단계: Deep Reset 성공 - {_deviceSettings.DeviceName}");
                    LogMessage("   💡 ModBus 스택 좀비 상태 완전 해결");
                    
                    // 하트비트 재시작
                    _heartbeatTimer.Change(HEARTBEAT_INTERVAL, HEARTBEAT_INTERVAL);
                    
                    OnConnectionStatusChanged(true, $"Deep Reset 성공 - {_deviceSettings.DeviceName}");
                }
                else
                {
                    LogMessage($"   ❌ 4단계: Deep Reset 실패 - {_deviceSettings.DeviceName}: {result}");
                    LogMessage($"   🔍 오류 상세: {GetModBusErrorDetail(result)}");
                    
                    // 실패 시 재연결 스케줄링
                    ScheduleReconnect();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"🚨 Deep Reset 중 예외 - {_deviceSettings.DeviceName}: {ex.Message}");
                ScheduleReconnect();
            }
        }

        /// <summary>
        /// 📊 큐 기반 데이터 처리
        /// </summary>
        private async Task ProcessDataQueue()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_dataRequestQueue.TryDequeue(out var request))
                    {
                        var data = await ReadRegistersAsync(request.StartAddress, request.Count);
                        
                        var result = new ModBusDataResult
                        {
                            RequestId = request.RequestId,
                            StartAddress = request.StartAddress,
                            Data = data,
                            Timestamp = DateTime.Now,
                            Success = data != null
                        };
                        
                        _dataResultQueue.Enqueue(result);
                    }
                    
                    await Task.Delay(100, _cancellationTokenSource.Token); // CPU 사용률 조절
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage($"📊 데이터 처리 오류: {ex.Message}");
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// 📈 연결 통계 정보
        /// </summary>
        public ConnectionStatistics GetStatistics()
        {
            return new ConnectionStatistics
            {
                IsConnected = _isConnected,
                LastSuccessfulConnection = _lastSuccessfulConnection,
                LastHeartbeat = _lastHeartbeat,
                TotalReconnects = _totalReconnects,
                TotalErrors = _totalErrors,
                SuccessfulReads = _successfulReads,
                ConnectionDuration = GetConnectionDuration(),
                DeviceName = _deviceSettings.DeviceName,
                IPAddress = _deviceSettings.IPAddress,
                Port = _deviceSettings.Port
            };
        }

        private TimeSpan GetConnectionDuration()
        {
            return _isConnected ? DateTime.Now - _lastSuccessfulConnection : TimeSpan.Zero;
        }

        #region 이벤트 발생
        
        private void OnConnectionStatusChanged(bool isConnected, string message)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
            {
                IsConnected = isConnected,
                Message = message,
                Timestamp = DateTime.Now,
                DeviceName = _deviceSettings.DeviceName
            });
        }

        private void OnDataReceived(int startAddress, ushort[] data)
        {
            DataReceived?.Invoke(this, new ModBusDataEventArgs
            {
                StartAddress = startAddress,
                Data = data,
                Timestamp = DateTime.Now,
                DeviceName = _deviceSettings.DeviceName
            });
        }

        private void OnErrorOccurred(string message, Exception exception)
        {
            ErrorOccurred?.Invoke(this, new ModBusErrorEventArgs
            {
                Message = message,
                Exception = exception,
                Timestamp = DateTime.Now,
                DeviceName = _deviceSettings.DeviceName
            });
        }

        #endregion

        /// <summary>
        /// 📝 로그 메시지 출력
        /// </summary>
        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] [{_deviceSettings.DeviceName}] {message}";
            
            // 콘솔 출력은 운영환경에서 과도할 수 있어 비활성화하거나 필요 시만 사용
            // Console.WriteLine(logMessage);
            
            // 파일 로그도 기록
            try
            {
                var baseName = $"reliable_modbus_{DateTime.Now:yyyyMMdd}";
                var logFile = baseName + ".log";

                // 기존 파일이 BOM 없이 만들어졌다면 즉시 별도 파일로 회전하여 BOM 적용
                try
                {
                    if (System.IO.File.Exists(logFile))
                    {
                        using (var fs = new System.IO.FileStream(logFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                        {
                            byte[] head = new byte[Math.Min(3, (int)fs.Length)];
                            fs.Read(head, 0, head.Length);
                            bool hasBom = head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
                            if (!hasBom)
                            {
                                logFile = baseName + "_utf8.log"; // BOM 적용 파일로 회전
                            }
                        }
                    }
                }
                catch { }

                // UTF-8 with BOM으로 기록하여 한글 깨짐 방지
                var utf8Bom = new System.Text.UTF8Encoding(true);
                using (var writer = new System.IO.StreamWriter(logFile, append: true, encoding: utf8Bom))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch { /* 로그 실패는 무시 */ }
        }

        public void Dispose()
        {
            try
            {
                // 🔧 안전한 토큰 취소
                if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
                
                // 타이머들 해제
                _heartbeatTimer?.Dispose();
                _reconnectTimer?.Dispose();
                _preventiveRestartTimer?.Dispose();
                StopPolling();
                
                // ModBus 연결 해제
                if (_isConnected)
                {
                    _modbus?.Close();
                }
                
                // 리소스 해제
                _connectionSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();
                
                LogMessage("🛑 ReliableModBusService 해제 완료");
            }
            catch (Exception ex)
            {
                LogMessage($"🚨 ReliableModBusService 해제 중 오류: {ex.Message}");
            }
        }
    }

    #region 데이터 모델

    public class ModBusDataRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public int StartAddress { get; set; }
        public int Count { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
    }

    public class ModBusDataResult
    {
        public Guid RequestId { get; set; }
        public int StartAddress { get; set; }
        public ushort[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; }
    }

    public class ModBusDataEventArgs : EventArgs
    {
        public int StartAddress { get; set; }
        public ushort[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; }
    }

    public class ModBusErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; }
    }

    #endregion
}