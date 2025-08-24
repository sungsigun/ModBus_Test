using ModBusDevExpress.Models;
using ModBusDevExpress.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace ModBusDevExpress.Service
{
    public static class DeviceConfigManager
    {
        private static string ConfigFilePath => FilePermissionHelper.GetSafeConfigPath("devices.json");

        public static List<ModbusDeviceSettings> LoadDeviceSettings()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    // devices.json이 없으면 빈 리스트로 시작 (더 이상 App.config 마이그레이션 안함)
                    return new List<ModbusDeviceSettings>();
                }

                string jsonContent = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<List<ModbusDeviceSettings>>(jsonContent)
                       ?? new List<ModbusDeviceSettings>();
            }
            catch
            {
                return new List<ModbusDeviceSettings>();
            }
        }

        public static void SaveDeviceSettings(List<ModbusDeviceSettings> settings)
        {
            try
            {
                string configPath = ConfigFilePath;
                string directory = Path.GetDirectoryName(configPath);
                
                // Documents 폴더를 사용하는 경우에만 디렉토리 생성
                if (!configPath.StartsWith(Application.StartupPath))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonContent = JsonSerializer.Serialize(settings,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(configPath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"디바이스 설정 저장 실패: {ex.Message}", ex);
            }
        }

        private static List<ModbusDeviceSettings> MigrateFromAppConfig()
        {
            var devices = new List<ModbusDeviceSettings>();

            try
            {
                // App.config에서 기존 설정 읽기
                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                    System.Configuration.ConfigurationUserLevel.None);

                var setValue = config.AppSettings.Settings["setvalue"]?.Value;
                if (!string.IsNullOrEmpty(setValue))
                {
                    // 줄바꿈과 불필요한 공백 제거
                    setValue = setValue.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
                    setValue = System.Text.RegularExpressions.Regex.Replace(setValue, @"\s+", " ");
                    
                    string[] deviceStrings = setValue.Split('^');
                    foreach (string deviceString in deviceStrings)
                    {
                        if (string.IsNullOrWhiteSpace(deviceString)) continue;

                        var device = ModbusDeviceSettings.FromConfigString(deviceString.Trim());
                        if (device != null)
                        {
                            devices.Add(device);
                        }
                    }
                }

                // 마이그레이션된 설정 저장
                if (devices.Count > 0)
                {
                    SaveDeviceSettings(devices);
                }
            }
            catch
            {
                // 마이그레이션 실패 시 빈 리스트 반환
            }

            return devices;
        }

        public static void AddDevice(ModbusDeviceSettings device)
        {
            var devices = LoadDeviceSettings();
            devices.Add(device);
            SaveDeviceSettings(devices);
        }

        public static void UpdateDevice(Guid deviceId, ModbusDeviceSettings updatedDevice)
        {
            var devices = LoadDeviceSettings();
            var index = devices.FindIndex(d => d.Id == deviceId);
            if (index >= 0)
            {
                devices[index] = updatedDevice;
                SaveDeviceSettings(devices);
            }
        }

        public static void RemoveDevice(Guid deviceId)
        {
            var devices = LoadDeviceSettings();
            devices.RemoveAll(d => d.Id == deviceId);
            SaveDeviceSettings(devices);
        }

        public static ModbusDeviceSettings GetDevice(Guid deviceId)
        {
            var devices = LoadDeviceSettings();
            return devices.FirstOrDefault(d => d.Id == deviceId);
        }

        /// <summary>
        /// App.config에서 devices.json으로 일회성 마이그레이션 (수동 호출용)
        /// </summary>
        public static bool MigrateFromAppConfigManual()
        {
            try
            {
                var migratedDevices = MigrateFromAppConfig();
                if (migratedDevices.Count > 0)
                {
                    SaveDeviceSettings(migratedDevices);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}