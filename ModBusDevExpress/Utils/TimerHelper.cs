using System;

namespace ModBusDevExpress.Utils
{
    /// <summary>
    /// 타이머 오프셋 계산 유틸리티
    /// </summary>
    public static class TimerHelper
    {
        /// <summary>
        /// IP 주소 기반 타이머 오프셋 계산 (트래픽 분산용)
        /// </summary>
        /// <param name="ipAddress">IP 주소 (포트 포함 가능)</param>
        /// <param name="slaveId">슬레이브 ID (선택적)</param>
        /// <param name="maxOffset">최대 오프셋 값</param>
        /// <param name="intervalMs">타이머 간격 (안전 클램프용)</param>
        /// <returns>계산된 오프셋 (ms)</returns>
        public static int CalculateOffset(string ipAddress, int slaveId = 0, int maxOffset = 2000, int intervalMs = 0)
        {
            try
            {
                // IP 주소에서 포트 제거
                string cleanIp = ipAddress?.Split(':')[0] ?? "";
                string[] ipParts = cleanIp.Split('.');
                
                if (ipParts.Length >= 4 && int.TryParse(ipParts[3], out int lastOctet))
                {
                    // IP 마지막 옥텟 기반 계산
                    int baseOffset = (lastOctet * 47) % maxOffset;
                    
                    // 슬레이브 ID가 있으면 추가 분산
                    if (slaveId > 0)
                    {
                        baseOffset = (baseOffset + (slaveId * 13)) % maxOffset;
                    }
                    
                    // 타이머 간격이 주어지면 안전 클램프 적용
                    if (intervalMs > 0)
                    {
                        int safeMax = Math.Max(10, intervalMs - 50);
                        baseOffset = Math.Min(baseOffset, safeMax);
                    }
                    
                    return baseOffset;
                }
            }
            catch
            {
                // IP 파싱 실패 시 해시 기반 계산
            }
            
            // 기본값: 해시 기반 랜덤 오프셋
            var hash = (ipAddress?.GetHashCode() ?? 0) + slaveId;
            var random = new Random(Math.Abs(hash));
            int fallbackMax = intervalMs > 0 ? Math.Min(maxOffset, intervalMs / 20) : Math.Min(maxOffset, 1000);
            return random.Next(0, fallbackMax);
        }
        
        /// <summary>
        /// ReliableModBus용 초기 오프셋 계산 (기존 호환성 유지)
        /// </summary>
        public static int ComputeInitialOffsetMs(string ipAddress, int slaveId, int intervalMs)
        {
            return CalculateOffset(ipAddress, slaveId, 801, intervalMs);
        }
        
        /// <summary>
        /// Form1용 타이머 오프셋 계산 (기존 호환성 유지)
        /// </summary>
        public static int CalculateTimerOffset(string ipAddress)
        {
            return CalculateOffset(ipAddress, 0, 2000, 0);
        }
    }
}




