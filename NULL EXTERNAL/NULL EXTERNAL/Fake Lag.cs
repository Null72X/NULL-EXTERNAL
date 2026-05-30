using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

public static class FakeLag
{
    private const string DllName = "WinDivert.dll";
    private const string SysName = "WinDivert64.sys";

    private static readonly string BasePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fake Lag Dlls");

    private static readonly string DllPath = Path.Combine(BasePath, DllName);
    private static readonly string SysPath = Path.Combine(BasePath, SysName);

    private static volatile bool _running;
    private static volatile bool _dllLoaded;

    private static Thread _workerThread;
    private static IntPtr _handle = IntPtr.Zero;

    public static bool IsRunning => _running;

    // Recommended: 1-15
    public static int LagMilliseconds { get; set; } = 5;

    #region WinDivert Imports

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr WinDivertOpen(
        string filter,
        int layer,
        short priority,
        ulong flags);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WinDivertClose(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WinDivertRecv(
        IntPtr handle,
        byte[] pPacket,
        int packetLen,
        int flags,
        IntPtr pAddr,
        ref int readLen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WinDivertSend(
        IntPtr handle,
        byte[] pPacket,
        int packetLen,
        int flags,
        IntPtr pAddr);

    #endregion

    static FakeLag()
    {
        Directory.CreateDirectory(BasePath);
    }

    public static bool IsDllLoaded()
    {
        return _dllLoaded;
    }

    private static bool LoadWinDivert()
    {
        if (_dllLoaded)
            return true;

        try
        {
            if (!File.Exists(DllPath))
                return false;

            if (!File.Exists(SysPath))
                return false;

            IntPtr module = LoadLibrary(DllPath);

            if (module == IntPtr.Zero)
                return false;

            _dllLoaded = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool Start()
    {
        if (_running)
            return true;

        if (!LoadWinDivert())
            return false;

        try
        {
            // Better filter
            string filter = "inbound and udp.PayloadLength >= 25";

            _handle = WinDivertOpen(filter, 0, 0, 0);

            if (_handle == IntPtr.Zero || _handle == new IntPtr(-1))
            {
                _handle = IntPtr.Zero;
                return false;
            }

            _running = true;

            _workerThread = new Thread(PacketLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };

            _workerThread.Start();

            return true;
        }
        catch
        {
            Stop();
            return false;
        }
    }

    public static void Stop()
    {
        if (!_running)
            return;

        _running = false;

        try
        {
            if (_handle != IntPtr.Zero)
            {
                WinDivertClose(_handle);
                _handle = IntPtr.Zero;
            }

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(100);
            }
        }
        catch
        {
        }
        finally
        {
            _workerThread = null;
        }
    }

    private static void PacketLoop()
    {
        byte[] packetBuffer = new byte[65535];
        IntPtr addressBuffer = Marshal.AllocHGlobal(64);

        try
        {
            while (_running)
            {
                int packetLength = 0;

                bool success = WinDivertRecv(
                    _handle,
                    packetBuffer,
                    packetBuffer.Length,
                    0,
                    addressBuffer,
                    ref packetLength);

                if (!success || packetLength <= 0)
                {
                    Thread.Yield();
                    continue;
                }

                int delay = LagMilliseconds;

                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }

                WinDivertSend(
                    _handle,
                    packetBuffer,
                    packetLength,
                    0,
                    addressBuffer);
            }
        }
        catch
        {
        }
        finally
        {
            Marshal.FreeHGlobal(addressBuffer);
        }
    }
}