using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ModBusDevExpress.Models;

namespace ModBusDevExpress.Service
{
    // 단일 프로세스 내에서 디바이스별 ReliableModBusService를 공유/참조 카운트 관리
    public static class ServiceRegistry
    {
        private class Shared
        {
            public ReliableModBusService Service { get; set; }
            public int RefCount; // field로 유지하여 Interlocked 사용 가능
        }

        private static readonly ConcurrentDictionary<string, Shared> _services = new();

        public static string MakeKey(ModbusDeviceSettings s) => $"{s.IPAddress}:{s.Port}:{s.SlaveId}";

        public static async Task<ReliableModBusService> GetOrCreateAsync(ModbusDeviceSettings settings)
        {
            string key = MakeKey(settings);
            var shared = _services.GetOrAdd(key, _ => new Shared
            {
                Service = new ReliableModBusService(settings),
                RefCount = 0
            });

            System.Threading.Interlocked.Increment(ref shared.RefCount);

            // 최초 생성 시 연결
            if (shared.RefCount == 1)
            {
                await shared.Service.ConnectAsync();
            }
            return shared.Service;
        }

        public static void Release(ModbusDeviceSettings settings)
        {
            string key = MakeKey(settings);
            if (_services.TryGetValue(key, out var shared))
            {
                if (System.Threading.Interlocked.Decrement(ref shared.RefCount) <= 0)
                {
                    try { shared.Service.Dispose(); } catch { }
                    _services.TryRemove(key, out _);
                }
            }
        }
    }
}


