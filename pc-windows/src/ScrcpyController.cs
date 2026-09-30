using System.Diagnostics;
using System.Text;

namespace Mirror;

/// <summary>
/// Lanza y controla scrcpy (el motor de espejo/control) y su ventana:
/// la localiza por título, la protege contra captura, le quita bordes y la
/// coloca/oculta a nuestro gusto.
/// </summary>
public sealed class ScrcpyController : IDisposable
{
    private readonly AppConfig _cfg;
    private readonly string _adbPath;
    private readonly string _scrcpyPath;
    private readonly string _windowTitle;
    private Process? _proc;

    public IntPtr WindowHandle { get; private set; } = IntPtr.Zero;
    public int Orientation { get; private set; } // 0/90/180/270

    public ScrcpyController(AppConfig cfg, string toolsDir)
    {
        _cfg = cfg;
        _adbPath = Path.Combine(toolsDir, "adb.exe");
        _scrcpyPath = Path.Combine(toolsDir, "scrcpy.exe");
        // Título único para poder localizar exactamente NUESTRA ventana.
        _windowTitle = "MV_" + Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>Prepara la conexión ADB según el modo (USB / hotspot / wireless).</summary>
    public async Task<string?> PrepareConnectionAsync()
    {
        if (_cfg.Mode == ConnectionMode.Usb)
        {
            var devices = await RunAdbAsync("devices");
            if (!devices.Contains("\tdevice"))
                return "No hay ningún móvil por USB con depuración autorizada.";
            return null;
        }

        // Hotspot / Wireless: conectar por IP.
        if (string.IsNullOrWhiteSpace(_cfg.DeviceAddress))
            return "Falta la dirección del móvil (ip:puerto) para conexión inalámbrica.";

        var res = await RunAdbAsync($"connect {_cfg.DeviceAddress}");
        if (!res.Contains("connected"))
            return $"No se pudo conectar por ADB a {_cfg.DeviceAddress}: {res.Trim()}";
        return null;
    }

    public void Start()
    {
        var args = new StringBuilder();
        args.Append($"--window-title=\"{_windowTitle}\" ");
        args.Append("--window-borderless ");
        args.Append("--always-on-top ");
        args.Append("--no-audio ");                 // menos latencia; se puede activar luego
        if (_cfg.StayAwake) args.Append("--stay-awake ");
        if (_cfg.TurnScreenOff) args.Append("--turn-screen-off "); // apagado real del panel
        if (_cfg.MaxSize > 0) args.Append($"--max-size={_cfg.MaxSize} ");
        args.Append($"--video-bit-rate={_cfg.BitrateMbps}M ");
        args.Append($"--max-fps={_cfg.MaxFps} ");
        if (Orientation != 0) args.Append(string.Format(_cfg.OrientationFlagFormat, Orientation) + " ");
        if (_cfg.Mode != ConnectionMode.Usb && !string.IsNullOrWhiteSpace(_cfg.DeviceAddress))
            args.Append($"-s {_cfg.DeviceAddress} ");

        var psi = new ProcessStartInfo
        {
            FileName = _scrcpyPath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        // scrcpy usa ADB_PATH / SCRCPY_SERVER_PATH del directorio local.
        psi.Environment["ADB"] = _adbPath;

        _proc = Process.Start(psi);
        WindowHandle = IntPtr.Zero;
    }

    /// <summary>
    /// Busca la ventana de scrcpy por su título y le aplica el estilo protegido.
    /// Se reintenta desde el llamador hasta que aparece (scrcpy tarda ~1 s).
    /// </summary>
    public bool TryAttachWindow()
    {
        if (WindowHandle != IntPtr.Zero) return true;
        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((h, _) =>
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetWindowText(h, sb, sb.Capacity);
            if (sb.ToString() == _windowTitle) { found = h; return false; }
            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero) return false;
        WindowHandle = found;
        HardenWindow(found);
        return true;
    }

    private void HardenWindow(IntPtr hwnd)
    {
        // 1) Protección contra captura: las grabadoras por software ven negro.
        if (!NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE))
            Debug.WriteLine("Aviso: SetWindowDisplayAffinity falló (¿Windows < 10 2004?).");

        // 2) Quitar bordes/redimension y sacarla de Alt-Tab / barra de tareas.
        var style = (long)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_STYLE);
        style &= ~(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME);
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_STYLE, new IntPtr(style));

        var ex = (long)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        ex |= NativeMethods.WS_EX_TOOLWINDOW; // discreta: no aparece en Alt-Tab ni barra
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(ex));
    }

    public void MoveResize(int x, int y, int w, int h, bool show)
    {
        if (WindowHandle == IntPtr.Zero) return;
        uint flags = NativeMethods.SWP_NOACTIVATE | (show ? NativeMethods.SWP_SHOWWINDOW : 0);
        NativeMethods.SetWindowPos(WindowHandle, NativeMethods.HWND_TOPMOST, x, y, w, h, flags);
    }

    public void SetVisible(bool visible)
    {
        if (WindowHandle == IntPtr.Zero) return;
        NativeMethods.ShowWindow(WindowHandle,
            visible ? NativeMethods.SW_SHOWNOACTIVATE : NativeMethods.SW_HIDE);
    }

    /// <summary>Voltea/rota 90°. Reinicia scrcpy con la nueva orientación (fiable).</summary>
    public void Flip()
    {
        Orientation = (Orientation + 90) % 360;
        Stop();
        Start();
    }

    public bool IsRunning => _proc is { HasExited: false };

    private async Task<string> RunAdbAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        string outp = await p.StandardOutput.ReadToEndAsync();
        string err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return outp + err;
    }

    public void Stop()
    {
        try
        {
            if (_proc is { HasExited: false })
            {
                _proc.Kill(entireProcessTree: true);
                _proc.WaitForExit(2000);
            }
        }
        catch { /* ya cerrado */ }
        finally { _proc?.Dispose(); _proc = null; WindowHandle = IntPtr.Zero; }
    }

    public void Dispose() => Stop();
}
