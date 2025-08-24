using System;
using System.Collections.Generic;
using System.Linq;
using ModBusDevExpress.Utils;

namespace ModBusDevExpress.Models
{
    public class ModbusDeviceSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string IPAddress { get; set; } = "";
        public int Port { get; set; } = Constants.DEFAULT_MODBUS_PORT;
        public int Interval { get; set; } = Constants.DEFAULT_COLLECT_INTERVAL_SECONDS;        // 🎯 수집 주기 (초)
        public int SaveInterval { get; set; } = Constants.DEFAULT_SAVE_INTERVAL_SECONDS;    // 🎯 저장 주기 (초) - 활용
        public int StartAddress { get; set; } = 0;
        public int DataLength { get; set; } = 10;
        public int SlaveId { get; set; } = 1;
        public string DeviceName { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public List<DeviceItem> Items { get; set; } = new List<DeviceItem>();
        public List<MemoryMapping> Mappings { get; set; } = new List<MemoryMapping>();
        public bool IsActive { get; set; } = true;

        [System.Text.Json.Serialization.JsonIgnore]
        public Form1 DeviceForm { get; set; }  // 연결된 Form1 인스턴스 (JSON 저장 시 제외)

        // 🎯 App.config 형식으로 변환 (저장주기 포함)
        public string ToConfigString()
        {
            string items = string.Join("/", Items.Select(i => i.Name));
            string mappings = string.Join("/", Mappings.Select(m => $"{m.Address}#{m.DataType}#{m.Format}"));

            // 저장주기를 포함해서 설정 문자열 생성
            return $"{IPAddress}:{Port},{Interval},{StartAddress},{DataLength},{SlaveId}," +
                   $"{DeviceName}#{DeviceCode},{items},{mappings},{SaveInterval}";
        }

        // 🎯 App.config 형식에서 파싱 (저장주기 포함)
        public static ModbusDeviceSettings FromConfigString(string configString)
        {
            try
            {
                var parts = configString.Split(',');
                if (parts.Length < 8) return null;

                var settings = new ModbusDeviceSettings();

                // IP:Port 파싱
                var ipPort = parts[0].Split(':');
                settings.IPAddress = ipPort[0];
                settings.Port = int.Parse(ipPort[1]);

                // 기본 설정
                settings.Interval = int.Parse(parts[1]);
                settings.StartAddress = int.Parse(parts[2]);
                settings.DataLength = int.Parse(parts[3]);
                settings.SlaveId = int.Parse(parts[4]);

                // 디바이스 정보
                var deviceInfo = parts[5].Split('#');
                settings.DeviceName = deviceInfo[0];
                settings.DeviceCode = deviceInfo.Length > 1 ? deviceInfo[1] : "";

                // 항목들
                if (parts.Length > 6 && !string.IsNullOrEmpty(parts[6]))
                {
                    var items = parts[6].Split('/');
                    settings.Items = items.Select((item, index) => new DeviceItem
                    {
                        Index = index + 1,
                        Name = item.Trim()
                    }).ToList();
                }

                // 메모리 맵핑
                if (parts.Length > 7 && !string.IsNullOrEmpty(parts[7]))
                {
                    var mappings = parts[7].Split('/');
                    settings.Mappings = mappings.Select(m =>
                    {
                        var mapParts = m.Split('#');
                        return new MemoryMapping
                        {
                            Address = int.Parse(mapParts[0]),
                            DataType = mapParts.Length > 1 ? mapParts[1] : "B",
                            Format = mapParts.Length > 2 ? mapParts[2] : "1"
                        };
                    }).ToList();
                }

                // 🎯 저장주기 설정 (새로 추가된 부분)
                if (parts.Length > 8 && !string.IsNullOrEmpty(parts[8]))
                {
                    settings.SaveInterval = int.Parse(parts[8]);
                }
                else
                {
                    // 기본값: 수집주기의 6배 또는 최소 60초
                    settings.SaveInterval = Math.Max(Constants.DEFAULT_SAVE_INTERVAL_SECONDS, settings.Interval * Constants.DEFAULT_SAVE_INTERVAL_MULTIPLIER);
                }

                return settings;
            }
            catch
            {
                return null;
            }
        }

        // 🎯 생성자에서 기본 저장주기 설정
        public ModbusDeviceSettings()
        {
            // 기본 저장주기는 60초
            SaveInterval = Constants.DEFAULT_SAVE_INTERVAL_SECONDS;
        }

        // 🎯 설정 유효성 검사
        public bool IsValidSettings()
        {
            return !string.IsNullOrEmpty(IPAddress) &&
                   Port > 0 && Port <= 65535 &&
                   Interval > 0 &&
                   SaveInterval >= Interval &&  // 저장주기는 수집주기보다 크거나 같아야 함
                   StartAddress >= 0 &&
                   DataLength > 0 &&
                   SlaveId > 0 && SlaveId <= 247 &&
                   !string.IsNullOrEmpty(DeviceName) &&
                   Items.Count > 0 &&
                   Mappings.Count > 0;
        }

        // 🎯 저장주기 자동 조정 (수집주기 변경 시 호출)
        public void AdjustSaveInterval()
        {
            if (SaveInterval < Interval)
            {
                SaveInterval = Math.Max(Constants.DEFAULT_SAVE_INTERVAL_SECONDS, Interval * Constants.DEFAULT_SAVE_INTERVAL_MULTIPLIER);
            }
        }
    }

    public class DeviceItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public double? MinValue { get; set; }  // 최소 유효값
        public double? MaxValue { get; set; }  // 최대 유효값
    }

    public class MemoryMapping
    {
        public int Address { get; set; }
        public string DataType { get; set; } = "B"; // B, W, F
        public string Format { get; set; } = "1"; // 1, F2, etc
    }
}