using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NULL_EXTERNAL
{
    class Collider
    {
        static List<nuint> baseAddresses = new();
        static ColliderMem mem;
        static bool isInitialized = false;

        // Toggle & permanent disable flags (thread‑safe)
        private static volatile bool _enabled = false;
        private static volatile bool _permanentlyDisabled = false;
        private static readonly object _lock = new object();

        // -----------------------------------------------------------------
        // Public control methods
        // -----------------------------------------------------------------
        public static void Enable()
        {
            lock (_lock)
            {
                if (!_permanentlyDisabled)
                    _enabled = true;
            }
        }

        public static void Disable()
        {
            lock (_lock)
            {
                _enabled = false;
            }
        }

        public static bool IsEnabled
        {
            get
            {
                lock (_lock)
                {
                    return _enabled;
                }
            }
        }

        // -----------------------------------------------------------------
        // Core aimbot logic
        // -----------------------------------------------------------------
        public static void Work()
        {
            while (true)
            {
                // Wait until initialized AND enabled (permanent disable makes IsEnabled false)
                if (!isInitialized || !IsEnabled)
                {
                    Thread.Sleep(10);   // prevent busy waiting
                    continue;
                }

                foreach (var baseAddr in baseAddresses)
                {
                    WriteAimValue(baseAddr);
                }

                // Small delay to reduce CPU load
                Thread.Sleep(1);
            }
        }

        private static void WriteAimValue(nuint baseAddr)
        {
            if (baseAddr == 0 || mem == null) return;

            // Read current head/aim value
            int headValue = mem.ReadInt32((long)(baseAddr + 0x25C));
            if (headValue == 0) return;

            const int repeat = 10;

            // Write aim position multiple times for stability
            for (int i = 0; i < repeat; i++)
            {
                mem.WriteInt32((long)(baseAddr - 0x1F8), headValue);
            }

            // Final write
            mem.WriteInt32((long)(baseAddr - 0x1F8), headValue);
        }

        // -----------------------------------------------------------------
        // Initialization (scan for pattern)
        // -----------------------------------------------------------------
        public static async Task InitAimbot()
        {
            string aobPattern = "FF FF FF FF 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 ?? ?? ?? ?? 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 01 00 00 00 ?? 00 00 00 ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? 01 00 00 00 00 00 00 00 00 00 00 00 ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00";

            var process = Process.GetProcessesByName("HD-Player").FirstOrDefault();
            if (process == null) return;

            mem = new ColliderMem();
            if (!mem.OpenProcessById(process.Id)) return;

            var result = await mem.FastAoBScan2(aobPattern, readable: true, writable: true);
            baseAddresses = result.Select(r => (nuint)r).ToList();

            isInitialized = baseAddresses.Count > 0;
        }
    }
}