using System;

namespace ModBusDevExpress.Models
{
    /// <summary>
    /// HF2311S 디바이스 연결 통계 정보
    /// </summary>
    public class ConnectionStatistics
    {
        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 마지막 성공한 연결 시간
        /// </summary>
        public DateTime LastSuccessfulConnection { get; set; }

        /// <summary>
        /// 마지막 하트비트 시간
        /// </summary>
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// 총 재연결 횟수
        /// </summary>
        public int TotalReconnects { get; set; }

        /// <summary>
        /// 총 오류 횟수
        /// </summary>
        public int TotalErrors { get; set; }

        /// <summary>
        /// 성공한 데이터 읽기 횟수
        /// </summary>
        public int SuccessfulReads { get; set; }

        /// <summary>
        /// 현재 연결 지속 시간
        /// </summary>
        public TimeSpan ConnectionDuration { get; set; }

        /// <summary>
        /// 디바이스 이름
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// IP 주소
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// 포트 번호
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 연결 품질 점수 (0-100)
        /// </summary>
        public int ConnectionQuality
        {
            get
            {
                if (!IsConnected) return 0;
                if (TotalReconnects == 0 && TotalErrors == 0) return 100;

                // 성공률 기반 품질 계산
                var totalOperations = SuccessfulReads + TotalErrors;
                if (totalOperations == 0) return 50;

                var successRate = (double)SuccessfulReads / totalOperations;
                var reconnectPenalty = Math.Min(TotalReconnects * 5, 50); // 재연결 1회당 5점 감소 (최대 50점)
                
                return Math.Max(0, (int)(successRate * 100) - reconnectPenalty);
            }
        }

        /// <summary>
        /// 연결 상태 설명
        /// </summary>
        public string StatusDescription
        {
            get
            {
                if (!IsConnected)
                    return "❌ 연결 끊김";

                var quality = ConnectionQuality;
                if (quality >= 90) return "✅ 연결 상태 우수";
                if (quality >= 70) return "🟡 연결 상태 양호";
                if (quality >= 50) return "🟠 연결 상태 보통";
                return "🔴 연결 상태 불안정";
            }
        }

        /// <summary>
        /// 상세 통계 문자열
        /// </summary>
        public string GetDetailedStats()
        {
            return $"📊 {DeviceName} 연결 통계\n" +
                   $"┌─────────────────────────────────────\n" +
                   $"│ 🔗 연결 상태: {StatusDescription}\n" +
                   $"│ 🌐 주소: {IPAddress}:{Port}\n" +
                   $"│ ⏰ 연결 지속: {FormatDuration(ConnectionDuration)}\n" +
                   $"│ 💓 마지막 하트비트: {GetTimeAgo(LastHeartbeat)}\n" +
                   $"│ 🔄 재연결 횟수: {TotalReconnects}회\n" +
                   $"│ ❌ 오류 횟수: {TotalErrors}회\n" +
                   $"│ ✅ 성공한 읽기: {SuccessfulReads}회\n" +
                   $"│ 📈 연결 품질: {ConnectionQuality}/100\n" +
                   $"└─────────────────────────────────────";
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}일 {duration.Hours}시간 {duration.Minutes}분";
            if (duration.TotalHours >= 1)
                return $"{duration.Hours}시간 {duration.Minutes}분";
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}분 {duration.Seconds}초";
            return $"{duration.Seconds}초";
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return "없음";

            var timeAgo = DateTime.Now - dateTime;
            if (timeAgo.TotalDays >= 1)
                return $"{(int)timeAgo.TotalDays}일 전";
            if (timeAgo.TotalHours >= 1)
                return $"{(int)timeAgo.TotalHours}시간 전";
            if (timeAgo.TotalMinutes >= 1)
                return $"{(int)timeAgo.TotalMinutes}분 전";
            return "방금 전";
        }
    }
}