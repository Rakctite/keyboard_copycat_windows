using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeyboardCopycat.Windows.Win32;

public sealed class LowLevelKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int LLKHF_INJECTED = 0x00000010;

    private readonly LowLevelKeyboardProc proc;
    private nint hookId;

    public LowLevelKeyboardHook()
    {
        proc = HookCallback;
    }

    public event EventHandler<KeyboardHookEventArgs>? KeyChanged;

    public bool SuppressKeyboardInput { get; set; }

    public void Start()
    {
        if (hookId != 0)
        {
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        hookId = SetWindowsHookEx(
            WH_KEYBOARD_LL,
            proc,
            GetModuleHandle(currentModule?.ModuleName),
            0);

        if (hookId == 0)
        {
            throw new InvalidOperationException("Failed to install low-level keyboard hook.");
        }
    }

    public void Dispose()
    {
        if (hookId != 0)
        {
            UnhookWindowsHookEx(hookId);
            hookId = 0;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            var injected = (info.Flags & LLKHF_INJECTED) != 0;
            if (!injected)
            {
                var handled = false;
                var message = wParam.ToInt32();
                if (message is WM_KEYDOWN or WM_SYSKEYDOWN)
                {
                    KeyChanged?.Invoke(this, new KeyboardHookEventArgs(info.VirtualKeyCode, true));
                    handled = true;
                }
                else if (message is WM_KEYUP or WM_SYSKEYUP)
                {
                    KeyChanged?.Invoke(this, new KeyboardHookEventArgs(info.VirtualKeyCode, false));
                    handled = true;
                }

                if (handled && SuppressKeyboardInput)
                {
                    return 1;
                }
            }
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KbdLlHookStruct
    {
        public readonly int VirtualKeyCode;
        public readonly int ScanCode;
        public readonly int Flags;
        public readonly int Time;
        public readonly nint ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        nint hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
