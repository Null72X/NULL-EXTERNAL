using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int WM_HOTKEY = 0x0312;

    // MOUSE BUTTONS
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_MBUTTON = 0x04;
    private const int VK_XBUTTON1 = 0x05;
    private const int VK_XBUTTON2 = 0x06;

    private readonly Form _form;

    private readonly Dictionary<Control, int> _buttonToId = new();

    private readonly Dictionary<int,
        (Control button, Keys key, Action action)> _idToData = new();

    private readonly Dictionary<Control, string> _originalTexts = new();

    private readonly Dictionary<Control, Action> _buttonActions = new();

    private readonly Dictionary<Control, Func<Task>>
        _buttonDownActions = new();

    private readonly Dictionary<Control, Func<Task>>
        _buttonUpActions = new();

    private readonly Dictionary<int,
        (Control button, Action action)> _mouseActions = new();

    // PREVENT DOUBLE TRIGGER
    private readonly HashSet<Keys> _pressedKeys = new();

    private readonly System.Windows.Forms.Timer _assignTimer;
    private readonly System.Windows.Forms.Timer _mouseTimer;

    private readonly GlobalKeyboardHook _keyboardHook;

    private int _nextId = 1;

    private Control _currentAssigning = null;

    private bool _disposed = false;

    public HotkeyManager(Form form)
    {
        _form = form ??
            throw new ArgumentNullException(nameof(form));

        _form.KeyPreview = true;

        _form.KeyDown += OnKeyDown;

        _form.FormClosing += OnFormClosing;

        Application.AddMessageFilter(
            new HotkeyMessageFilter(this));

        // GLOBAL KEYBOARD HOOK
        _keyboardHook = new GlobalKeyboardHook();

        _keyboardHook.KeyDown += KeyboardHook_KeyDown;
        _keyboardHook.KeyUp += KeyboardHook_KeyUp;

        // ASSIGN TIMER
        _assignTimer = new System.Windows.Forms.Timer();

        _assignTimer.Interval = 20;

        _assignTimer.Tick += AssignTimer_Tick;

        // MOUSE TIMER
        _mouseTimer = new System.Windows.Forms.Timer();

        _mouseTimer.Interval = 20;

        _mouseTimer.Tick += MouseTimer_Tick;

        _mouseTimer.Start();
    }

    // =========================================================
    // MESSAGE FILTER
    // =========================================================
    private class HotkeyMessageFilter : IMessageFilter
    {
        private readonly HotkeyManager _manager;

        public HotkeyMessageFilter(HotkeyManager manager)
        {
            _manager = manager;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                _manager.OnHotkeyReceived(
                    m.WParam.ToInt32());
            }

            return false;
        }
    }

    // =========================================================
    // HOTKEY RECEIVED
    // =========================================================
    private void OnHotkeyReceived(int id)
    {
        if (_idToData.TryGetValue(id, out var data))
        {
            // PREVENT DOUBLE TRIGGER
            if (_pressedKeys.Contains(data.key))
                return;

            _pressedKeys.Add(data.key);

            Task.Delay(100).ContinueWith(_ =>
            {
                _pressedKeys.Remove(data.key);
            });
        }
    }

    // =========================================================
    // KEY DOWN
    // =========================================================
    private async void KeyboardHook_KeyDown(Keys key)
    {
        if (_pressedKeys.Contains(key))
            return;

        _pressedKeys.Add(key);

        try
        {
            foreach (var kv in _idToData)
            {
                if (kv.Value.key == key)
                {
                    // NORMAL ACTION
                    kv.Value.action?.Invoke();

                    // DOWN ACTION
                    if (_buttonDownActions.TryGetValue(
                        kv.Value.button,
                        out var action))
                    {
                        await action();
                    }
                }
            }
        }
        finally
        {
            await Task.Delay(100);

            _pressedKeys.Remove(key);
        }
    }

    // =========================================================
    // KEY UP
    // =========================================================
    private async void KeyboardHook_KeyUp(Keys key)
    {
        foreach (var kv in _idToData)
        {
            if (kv.Value.key == key)
            {
                if (_buttonUpActions.TryGetValue(
                    kv.Value.button,
                    out var action))
                {
                    await action();
                }
            }
        }

        _pressedKeys.Remove(key);

        await Task.CompletedTask;
    }

    // =========================================================
    // KEYBOARD ASSIGN
    // =========================================================
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_currentAssigning == null)
            return;

        // ESC CLEAR
        if (e.KeyCode == Keys.Escape)
        {
            ClearHotkey(_currentAssigning);

            CancelAssign();

            e.SuppressKeyPress = true;

            return;
        }

        Keys key = e.KeyCode;

        if (key == Keys.ControlKey ||
            key == Keys.ShiftKey ||
            key == Keys.Menu ||
            key == Keys.LWin ||
            key == Keys.RWin)
        {
            e.SuppressKeyPress = true;

            return;
        }

        // DUPLICATE CHECK
        foreach (var kvp in _idToData)
        {
            if (kvp.Value.button != _currentAssigning &&
                kvp.Value.key == key)
            {
                CancelAssign();

                e.SuppressKeyPress = true;

                return;
            }
        }

        AssignKeyboardHotkey(_currentAssigning, key);

        CancelAssign();

        e.SuppressKeyPress = true;
    }

    // =========================================================
    // MOUSE ASSIGN TIMER
    // =========================================================
    private void AssignTimer_Tick(
        object sender,
        EventArgs e)
    {
        if (_currentAssigning == null)
            return;

        CheckMouseAssign(VK_LBUTTON, "L BUTTON");
        CheckMouseAssign(VK_RBUTTON, "R BUTTON");
        CheckMouseAssign(VK_MBUTTON, "M BUTTON");
        CheckMouseAssign(VK_XBUTTON1, "X1 BUTTON");
        CheckMouseAssign(VK_XBUTTON2, "X2 BUTTON");
    }

    private void CheckMouseAssign(
        int vk,
        string name)
    {
        if ((GetAsyncKeyState(vk) & 0x8000) != 0)
        {
            AssignMouseHotkey(
                _currentAssigning,
                vk,
                name);

            CancelAssign();
        }
    }

    // =========================================================
    // MOUSE HOTKEYS
    // =========================================================
    private void MouseTimer_Tick(
        object sender,
        EventArgs e)
    {
        foreach (var kv in _mouseActions)
        {
            if ((GetAsyncKeyState(kv.Key) & 0x8000) != 0)
            {
                kv.Value.action?.Invoke();
            }
        }
    }

    // =========================================================
    // BEGIN ASSIGN
    // =========================================================
    public void BeginAssignHotkey(Control button)
    {
        if (button == null)
            return;

        _currentAssigning = button;

        if (!_originalTexts.ContainsKey(button))
        {
            _originalTexts[button] = button.Text;
        }

        button.Text = "...";

        _form.Focus();

        _assignTimer.Start();
    }

    // =========================================================
    // ASSIGN KEYBOARD
    // =========================================================
    private void AssignKeyboardHotkey(
        Control button,
        Keys key)
    {
        ClearHotkey(button);

        int newId = _nextId++;

        // REGISTER HOTKEY
        RegisterHotKey(
            _form.Handle,
            newId,
            0,
            (uint)key);

        Action action =
            _buttonActions.TryGetValue(
                button,
                out var act)
            ? act
            : null;

        _buttonToId[button] = newId;

        _idToData[newId] =
            (button, key, action);

        button.Text = $"[{key}]";
    }

    // =========================================================
    // ASSIGN MOUSE
    // =========================================================
    private void AssignMouseHotkey(
        Control button,
        int mouseVk,
        string name)
    {
        ClearHotkey(button);

        Action action =
            _buttonActions.TryGetValue(
                button,
                out var act)
            ? act
            : null;

        _mouseActions[mouseVk] =
            (button, action);

        button.Text = $"[{name}]";
    }

    // =========================================================
    // CLEAR HOTKEY
    // =========================================================
    private void ClearHotkey(Control button)
    {
        // KEYBOARD
        if (_buttonToId.TryGetValue(button, out int id))
        {
            UnregisterHotKey(_form.Handle, id);

            _buttonToId.Remove(button);

            _idToData.Remove(id);
        }

        // MOUSE
        int mouseKeyToRemove = -1;

        foreach (var kv in _mouseActions)
        {
            if (kv.Value.button == button)
            {
                mouseKeyToRemove = kv.Key;

                break;
            }
        }

        if (mouseKeyToRemove != -1)
        {
            _mouseActions.Remove(mouseKeyToRemove);
        }

        // TEXT
        if (_originalTexts.ContainsKey(button))
        {
            button.Text = _originalTexts[button];
        }
        else
        {
            button.Text = "None";
        }
    }

    // =========================================================
    // NORMAL ACTION
    // =========================================================
    public void SetHotkeyAction(
        Control button,
        Action action)
    {
        _buttonActions[button] = action;

        // UPDATE KEYBOARD
        if (_buttonToId.TryGetValue(button, out int id))
        {
            if (_idToData.ContainsKey(id))
            {
                var data = _idToData[id];

                _idToData[id] =
                    (data.button, data.key, action);
            }
        }

        // UPDATE MOUSE
        foreach (var kv in _mouseActions)
        {
            if (kv.Value.button == button)
            {
                _mouseActions[kv.Key] =
                    (button, action);
            }
        }
    }

    // =========================================================
    // DOWN ACTION
    // =========================================================
    public void SetHotkeyDownAction(
        Control button,
        Func<Task> action)
    {
        _buttonDownActions[button] = action;
    }

    // =========================================================
    // UP ACTION
    // =========================================================
    public void SetHotkeyUpAction(
        Control button,
        Func<Task> action)
    {
        _buttonUpActions[button] = action;
    }

    // =========================================================
    // CANCEL ASSIGN
    // =========================================================
    private void CancelAssign()
    {
        _assignTimer.Stop();

        _currentAssigning = null;
    }

    // =========================================================
    // FORM CLOSING
    // =========================================================
    private void OnFormClosing(
        object sender,
        FormClosingEventArgs e)
    {
        Dispose();
    }

    // =========================================================
    // DISPOSE
    // =========================================================
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var id in _buttonToId.Values)
        {
            UnregisterHotKey(_form.Handle, id);
        }

        _assignTimer?.Stop();
        _mouseTimer?.Stop();

        _assignTimer?.Dispose();
        _mouseTimer?.Dispose();

        _keyboardHook?.Dispose();

        _buttonToId.Clear();
        _idToData.Clear();
        _mouseActions.Clear();
        _buttonActions.Clear();
        _pressedKeys.Clear();

        _disposed = true;
    }
}

// =========================================================
// GLOBAL KEYBOARD HOOK
// =========================================================
public class GlobalKeyboardHook : IDisposable
{
    public event Action<Keys> KeyPressed;
    public event Action<Keys> KeyDown;
    public event Action<Keys> KeyUp;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private LowLevelKeyboardProc _proc;

    private IntPtr _hookID = IntPtr.Zero;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;

        _hookID = SetHook(_proc);

        Application.ApplicationExit +=
            (s, e) => Dispose();
    }

    private IntPtr SetHook(
        LowLevelKeyboardProc proc)
    {
        using (Process curProcess =
            Process.GetCurrentProcess())

        using (ProcessModule curModule =
            curProcess.MainModule)
        {
            return SetWindowsHookEx(
                WH_KEYBOARD_LL,
                proc,
                GetModuleHandle(curModule.ModuleName),
                0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    private IntPtr HookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            Keys key = (Keys)vkCode;

            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                KeyDown?.Invoke(key);

                KeyPressed?.Invoke(key);
            }
            else if (wParam == (IntPtr)WM_KEYUP)
            {
                KeyUp?.Invoke(key);
            }
        }

        return CallNextHookEx(
            _hookID,
            nCode,
            wParam,
            lParam);
    }

    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]

    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]

    private static extern bool UnhookWindowsHookEx(
        IntPtr hhk);

    [DllImport("user32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]

    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]

    private static extern IntPtr GetModuleHandle(
        string lpModuleName);

    public void Dispose()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);

            _hookID = IntPtr.Zero;
        }
    }
}