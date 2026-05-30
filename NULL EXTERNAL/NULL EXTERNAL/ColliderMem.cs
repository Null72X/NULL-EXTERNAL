using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NULL_EXTERNAL
{
    internal class ColliderMem
    {
        private IntPtr hProcess = IntPtr.Zero;
        private int processId;

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int size, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("psapi.dll")]
        static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule, int cb, out int lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll")]
        static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, int nSize);

        [DllImport("psapi.dll")]
        static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, int cb);

        [DllImport("kernel32.dll")]
        public static extern UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public int SizeOfImage;
            public IntPtr EntryPoint;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            public ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        public bool OpenProcessByName(string processName)
        {
            var proc = Process.GetProcessesByName(processName);
            if (proc.Length == 0) return false;
            processId = proc[0].Id;
            hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            return hProcess != IntPtr.Zero;
        }

        public bool OpenProcessById(int pid)
        {
            processId = pid;
            hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
            return hProcess != IntPtr.Zero;
        }

        public int ReadInt32(long addr)
        {
            var buffer = new byte[4];
            ReadProcessMemory(hProcess, addr, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void WriteInt32(long addr, int value)
        {
            var buffer = BitConverter.GetBytes(value);
            WriteProcessMemory(hProcess, addr, buffer, buffer.Length, out _);
        }

        public List<long> AoBScan(string pattern, string moduleName = null)
        {
            var results = new List<long>();
            if (hProcess == IntPtr.Zero) return results;

            IntPtr[] modules = new IntPtr[1024];
            EnumProcessModulesEx(hProcess, modules, modules.Length * IntPtr.Size, out int needed, 0x03);
            int totalModules = needed / IntPtr.Size;

            for (int i = 0; i < totalModules; i++)
            {
                StringBuilder modName = new(256);
                GetModuleBaseName(hProcess, modules[i], modName, modName.Capacity);
                if (moduleName != null && modName.ToString().ToLower() != moduleName.ToLower())
                    continue;

                GetModuleInformation(hProcess, modules[i], out MODULEINFO modInfo, Marshal.SizeOf(typeof(MODULEINFO)));

                long start = modInfo.lpBaseOfDll.ToInt64();
                int size = modInfo.SizeOfImage;
                byte[] buffer = new byte[size];
                ReadProcessMemory(hProcess, start, buffer, size, out _);

                byte?[] patternBytes = ParsePattern(pattern);
                for (int j = 0; j < buffer.Length - patternBytes.Length; j++)
                {
                    bool found = true;
                    for (int k = 0; k < patternBytes.Length; k++)
                    {
                        if (patternBytes[k].HasValue && buffer[j + k] != patternBytes[k].Value)
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found) results.Add(start + j);
                }

                if (moduleName != null) break;
            }

            return results;
        }

        public Task<IEnumerable<long>> FastAoBScan(string patternStr, bool readable = true, bool writable = false, bool executable = false)
        {
            return Task.Run(() =>
            {
                var foundAddresses = new ConcurrentBag<long>();
                string[] patternParts = patternStr.Split(' ');
                byte[] pattern = new byte[patternParts.Length];
                byte[] mask = new byte[patternParts.Length];
                bool[] ignore00 = new bool[patternParts.Length];

                for (int i = 0; i < patternParts.Length; i++)
                {
                    string part = patternParts[i];
                    if (part == "??") { pattern[i] = 0x00; mask[i] = 0x00; ignore00[i] = false; }
                    else if (part == "!!") { pattern[i] = 0x00; mask[i] = 0x00; ignore00[i] = true; }
                    else { pattern[i] = Convert.ToByte(part, 16); mask[i] = 0xFF; ignore00[i] = false; }
                }

                GetSystemInfo(out SYSTEM_INFO sysInfo);
                long minAddr = sysInfo.minimumApplicationAddress.ToInt64();
                long maxAddr = sysInfo.maximumApplicationAddress.ToInt64();

                List<MEMORY_BASIC_INFORMATION> memoryRegions = new();
                UIntPtr address = new((ulong)minAddr);

                while (VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION memInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != UIntPtr.Zero && (long)address.ToUInt64() < maxAddr)
                {
                    if (memInfo.State == 0x1000 && (memInfo.Protect & 0x100) == 0 && (memInfo.Protect & 1) == 0)
                    {
                        bool isReadable = (memInfo.Protect & 0x2) > 0 && readable;
                        bool isWritable = (memInfo.Protect & 0x4) > 0 && writable;
                        bool isExecutable = (memInfo.Protect & 0x20) > 0 && executable;
                        if (isReadable || isWritable || isExecutable)
                            memoryRegions.Add(memInfo);
                    }
                    address = new UIntPtr(address.ToUInt64() + memInfo.RegionSize.ToUInt64());
                }

                Parallel.ForEach(memoryRegions, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, region =>
                {
                    byte[] buffer = new byte[(int)region.RegionSize];
                    if (!ReadProcessMemory(hProcess, (long)(ulong)region.BaseAddress, buffer, buffer.Length, out _)) return;


                    for (int i = 0; i <= buffer.Length - pattern.Length; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < pattern.Length; j++)
                        {
                            if (mask[j] != 0 && buffer[i + j] != (pattern[j] & mask[j])) { match = false; break; }
                            if (ignore00[j] && buffer[i + j] == 0x00) { match = false; break; }
                        }
                        if (match) foundAddresses.Add((long)region.BaseAddress + i);
                    }
                });

                return foundAddresses.AsEnumerable();
            });
        }
        public Task<IEnumerable<long>> FastAoBScan2(string patternStr, bool readable = true, bool writable = false, bool executable = false)
        {
            return Task.Run(() =>
            {
                var results = new ConcurrentBag<long>();

                // Parse pattern
                string[] patternParts = patternStr.Split(' ');
                byte?[] pattern = patternParts.Select(p => p == "??" ? (byte?)null : Convert.ToByte(p, 16)).ToArray();
                int patternLength = pattern.Length;

                // Get memory range
                GetSystemInfo(out SYSTEM_INFO sysInfo);
                long minAddr = (long)sysInfo.minimumApplicationAddress;
                long maxAddr = (long)sysInfo.maximumApplicationAddress;

                List<MEMORY_BASIC_INFORMATION> regions = new();
                UIntPtr addr = new((ulong)minAddr);

                while (VirtualQueryEx(hProcess, addr, out MEMORY_BASIC_INFORMATION memInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != UIntPtr.Zero &&
                       (long)addr.ToUInt64() < maxAddr)
                {
                    bool isReadable = (memInfo.Protect & 0x02) > 0;
                    bool isWritable = (memInfo.Protect & 0x04) > 0;
                    bool isExecutable = (memInfo.Protect & 0x20) > 0;

                    if (memInfo.State == 0x1000 && (memInfo.Protect & 0x100) == 0 && (memInfo.Protect & 0x01) == 0)
                    {
                        if ((readable && isReadable) || (writable && isWritable) || (executable && isExecutable))
                        {
                            regions.Add(memInfo);
                        }
                    }

                    addr = new UIntPtr(addr.ToUInt64() + memInfo.RegionSize.ToUInt64());
                }

                Parallel.ForEach(regions, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, region =>
                {
                    byte[] buffer = new byte[(int)region.RegionSize];
                    if (!ReadProcessMemory(hProcess, (long)(ulong)region.BaseAddress, buffer, buffer.Length, out _)) return;

                    int limit = buffer.Length - patternLength;
                    Span<byte> span = buffer;

                    for (int i = 0; i <= limit; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < patternLength; j++)
                        {
                            if (pattern[j].HasValue && span[i + j] != pattern[j].Value)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            results.Add((long)region.BaseAddress + i);
                        }
                    }
                });

                return results.AsEnumerable();
            });
        }
        private byte?[] ParsePattern(string pattern)
        {
            string[] parts = pattern.Split(' ');
            var bytes = new List<byte?>();
            foreach (var p in parts)
            {
                if (p == "??" || p == "?")
                    bytes.Add(null);
                else
                    bytes.Add(Convert.ToByte(p, 16));
            }
            return bytes.ToArray();
        }
    }
}