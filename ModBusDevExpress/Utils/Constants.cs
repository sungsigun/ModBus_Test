namespace ModBusDevExpress.Utils
{
    /// <summary>
    /// 프로젝트 전체에서 사용하는 공통 상수들
    /// </summary>
    public static class Constants
    {
        // ModBus 기본 설정 (UI 기본값용)
        public const int DEFAULT_MODBUS_PORT = 502;
        public const int DEFAULT_COLLECT_INTERVAL_SECONDS = 10;
        public const int DEFAULT_SAVE_INTERVAL_SECONDS = 60;  // UI 기본값 (실제로는 디바이스별 설정 사용)
        public const int DEFAULT_START_ADDRESS = 0;
        public const int DEFAULT_DATA_LENGTH = 10;
        public const int DEFAULT_SLAVE_ID = 1;
        
        // 타이머 및 간격 설정
        public const int MIN_COLLECT_INTERVAL_SECONDS = 1;
        public const int MAX_COLLECT_INTERVAL_SECONDS = 3600;
        public const int MIN_SAVE_INTERVAL_SECONDS = 10;
        public const int MAX_SAVE_INTERVAL_SECONDS = 3600;
        
        // UI 관련 상수
        public const int LIVE_FEED_MAX_ITEMS = 100;
        public const int GRID_COLUMN_WIDTH_SMALL = 60;
        public const int GRID_COLUMN_WIDTH_MEDIUM = 80;
        public const int GRID_COLUMN_WIDTH_LARGE = 100;
        public const int GRID_COLUMN_WIDTH_EXTRA_LARGE = 120;
        public const int GRID_COLUMN_WIDTH_IP = 100;
        public const int GRID_COLUMN_WIDTH_DEVICE_NAME = 120;
        
        // 페이징 관련
        public const int DEFAULT_PAGE_SIZE = 50;
        public const int ALTERNATIVE_PAGE_SIZE = 100;
        public const int MAX_FACILITY_LIST_SIZE = 100;
        
        // 연결 및 타임아웃 설정
        public const int DEFAULT_CONNECTION_TIMEOUT_MS = 1000;
        public const int DEFAULT_RESPONSE_TIMEOUT_MS = 1000;
        public const int HEARTBEAT_INTERVAL_MS = 60000;
        public const int MAX_IDLE_TIME_MS = 300000;
        public const int CONNECTION_REFRESH_THRESHOLD = 50;
        
        // 자동 복구 관련
        public const int MAX_CONSECUTIVE_FAILURES = 3;
        public const double MIN_SAVE_SUCCESS_RATE = 0.5;
        public const int AUTO_REFRESH_COOLDOWN_MINUTES = 10;
        public const int LIGHT_RECOVERY_COOLDOWN_SECONDS = 30;
        
        // 로그 관련
        public const string LOG_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        public const string LOG_FILE_DATE_FORMAT = "yyyyMMdd";
        public const string LOG_DEVICE_DATE_FORMAT = "yyyyMMdd_HH";
        
        // 기본 저장주기 계산 배수
        public const int DEFAULT_SAVE_INTERVAL_MULTIPLIER = 6;
    }
}
