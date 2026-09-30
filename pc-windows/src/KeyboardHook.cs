using System.Runtime.InteropServices;

namespace Mirror;

/// <summary>
/// Hook de teclado de bajo nivel. Detecta el Enter del teclado numérico
/// (que comparte código con el Enter normal pero lleva el flag "extended")
/// y, solo si NumLock está activado, dispara el toggle y se "traga" la tecla
/// para que no inserte un salto de línea.
///
/// Con NumLock apagado, el Enter del numpad funciona con total normalidad.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const uint VK_RETURN = 0x0D;
    private const uint VK_NUMLOCK = 0x90;
    private const uint LLKHF_EXTENDED = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    private readonly LowLevelKeyboardProc _proc;   // guardado para que no lo recoja el GC
    private IntPtr _hook = IntPtr.Zero;

    /// <summary>Se dispara (en el hilo de UI) cuando toca alternar el panel.</summary>
    public event Action? Toggle;

    public KeyboardHook() => _proc = HookProc;

    public void Install()
    {
        if (_hook != IntPtr.Zero) return;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool isNumpadEnter = data.vkCode == VK_RETURN && (data.flags & LLKHF_EXTENDED) != 0;
            bool numLockOn = (GetKeyState((int)VK_NUMLOCK) & 1) != 0;

            if (isNumpadEnter && numLockOn)
            {
                Toggle?.Invoke();
                return new IntPtr(1); // consumir: no llega a las apps
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
    }
}
