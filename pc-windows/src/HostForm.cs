using System.ComponentModel;

namespace Mirror;

/// <summary>
/// Ventana anfitriona invisible. No se ve nunca: solo sostiene el bucle de
/// mensajes, registra el atajo global (tipo NumLock) y anima la ventana de
/// scrcpy deslizándola desde la esquina inferior derecha.
/// </summary>
public sealed class HostForm : Form
{
    private const int HOTKEY_ID = 0xA11;

    private readonly AppConfig _cfg;
    private readonly ScrcpyController _scrcpy;
    private readonly System.Windows.Forms.Timer _attachTimer;   // engancha la ventana de scrcpy
    private readonly System.Windows.Forms.Timer _animTimer;     // anima el deslizamiento
    private KeyboardHook? _kbHook;                              // Enter del numpad + NumLock

    private enum PanelState { Hidden, Showing, Visible, Hiding }
    private PanelState _state = PanelState.Hidden;
    private double _animT;          // 0..1
    private DateTime _animStart;
    private bool _reshowAfterAttach; // recordar visibilidad al voltear
    private bool _starting;          // evita doble arranque bajo demanda

    public HostForm(AppConfig cfg, ScrcpyController scrcpy)
    {
        _cfg = cfg;
        _scrcpy = scrcpy;

        // Invisible y fuera de la barra de tareas.
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-2000, -2000);
        Size = new Size(1, 1);
        Opacity = 0;

        _attachTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _attachTimer.Tick += OnAttachTick;

        _animTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _animTimer.Tick += OnAnimTick;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_cfg.Trigger == ToggleTrigger.NumpadEnterNumLock)
        {
            _kbHook = new KeyboardHook();
            _kbHook.Toggle += () => BeginInvoke(new Action(TogglePanel));
            _kbHook.Install();
        }
        else
        {
            RegisterToggleHotkey();
        }
        // No se arranca scrcpy aquí: se arranca BAJO DEMANDA al pulsar el toggle.
        // Así la app puede estar abierta sin el móvil conectado y sin ningún aviso.
    }

    private void RegisterToggleHotkey()
    {
        uint mods = NativeMethods.MOD_NOREPEAT;
        var hk = _cfg.ToggleHotkey;
        if (hk.Ctrl) mods |= NativeMethods.MOD_CONTROL;
        if (hk.Alt) mods |= NativeMethods.MOD_ALT;
        if (hk.Shift) mods |= NativeMethods.MOD_SHIFT;
        NativeMethods.RegisterHotKey(Handle, HOTKEY_ID, mods, hk.VirtualKey);
    }

    // Espera a que scrcpy cree su ventana; al engancharla, la protege y la coloca oculta.
    private void OnAttachTick(object? sender, EventArgs e)
    {
        if (_scrcpy.TryAttachWindow())
        {
            _attachTimer.Stop();
            var (x, y, w, h) = TargetRect();
            // Empieza fuera de pantalla (abajo) y oculta.
            _scrcpy.MoveResize(x, ScreenBottom(), w, h, show: false);
            _state = PanelState.Hidden;
            // Al arrancar bajo demanda (o tras voltear) se muestra automáticamente.
            if (_reshowAfterAttach) { _reshowAfterAttach = false; TogglePanel(); }
        }
        else if (!_scrcpy.IsRunning)
        {
            // scrcpy murió (p. ej. el móvil se desconectó): dejarlo listo para
            // reintentar en el próximo toggle, en silencio.
            _attachTimer.Stop();
            _reshowAfterAttach = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            TogglePanel();
            return;
        }
        base.WndProc(ref m);
    }

    private void TogglePanel()
    {
        // Caso 1: ya hay ventana enganchada -> alternar mostrar/ocultar con animación.
        if (_scrcpy.WindowHandle != IntPtr.Zero && _scrcpy.IsRunning)
        {
            switch (_state)
            {
                case PanelState.Hidden:
                case PanelState.Hiding:
                    _state = PanelState.Showing;
                    _scrcpy.SetVisible(true);
                    StartAnim();
                    break;
                case PanelState.Visible:
                case PanelState.Showing:
                    _state = PanelState.Hiding;
                    StartAnim();
                    break;
            }
            return;
        }

        // Caso 2: scrcpy ya se está arrancando (esperando su ventana) -> no hacer nada.
        if (_starting || _attachTimer.Enabled) return;

        // Caso 3: no está corriendo -> intentar arrancar bajo demanda.
        // Si el móvil no está conectado, no ocurre nada (sin avisos).
        TryStartOnDemand();
    }

    /// <summary>
    /// Arranca scrcpy solo si hay móvil conectado. Silencioso: si no lo hay,
    /// no muestra ningún aviso y no pasa nada. Al enganchar la ventana, se muestra.
    /// </summary>
    private void TryStartOnDemand()
    {
        _starting = true;
        try
        {
            string? err = _scrcpy.PrepareConnectionAsync().GetAwaiter().GetResult();
            if (err != null) return;      // no hay móvil / no conecta -> silencio
            _reshowAfterAttach = true;    // al enganchar la ventana, mostrarla
            _scrcpy.Start();
            _attachTimer.Start();
        }
        catch { /* cualquier fallo -> en silencio, sin avisos */ }
        finally { _starting = false; }
    }

    private void StartAnim()
    {
        _animStart = DateTime.UtcNow;
        _animT = 0;
        _animTimer.Start();
    }

    private void OnAnimTick(object? sender, EventArgs e)
    {
        double dur = Math.Max(1, _cfg.AnimationMs);
        _animT = Math.Clamp((DateTime.UtcNow - _animStart).TotalMilliseconds / dur, 0, 1);
        double eased = EaseOutCubic(_animT);

        var (x, targetY, w, h) = TargetRect();
        int fromY, toY;
        if (_state == PanelState.Showing) { fromY = ScreenBottom(); toY = targetY; }
        else { fromY = targetY; toY = ScreenBottom(); }

        int y = (int)Math.Round(fromY + (toY - fromY) * eased);
        _scrcpy.MoveResize(x, y, w, h, show: true);

        if (_animT >= 1.0)
        {
            _animTimer.Stop();
            if (_state == PanelState.Showing) _state = PanelState.Visible;
            else { _state = PanelState.Hidden; _scrcpy.SetVisible(false); }
        }
    }

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    /// <summary>Rectángulo destino: esquina inferior derecha del área de trabajo.</summary>
    private (int x, int y, int w, int h) TargetRect()
    {
        var wa = Screen.PrimaryScreen!.WorkingArea;
        int w = _cfg.WindowWidth, h = _cfg.WindowHeight;
        // Al voltear a horizontal, intercambia ancho/alto.
        if (_scrcpy.Orientation is 90 or 270) (w, h) = (h, w);
        int x = wa.Right - w - _cfg.MarginRight;
        int y = wa.Bottom - h - _cfg.MarginBottom;
        return (x, y, w, h);
    }

    private int ScreenBottom() => Screen.PrimaryScreen!.Bounds.Bottom;

    /// <summary>Alternar mostrar/ocultar desde fuera (menú de bandeja).</summary>
    public void TogglePanelPublic() => TogglePanel();

    public void FlipPanel()
    {
        // Solo tiene sentido si el espejo está en marcha.
        if (!_scrcpy.IsRunning || _scrcpy.WindowHandle == IntPtr.Zero) return;
        _reshowAfterAttach = _state is PanelState.Visible or PanelState.Showing;
        _animTimer.Stop();
        _scrcpy.Flip();
        // Tras reiniciar scrcpy hay que volver a enganchar la ventana.
        _attachTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_cfg.Trigger == ToggleTrigger.Hotkey)
            NativeMethods.UnregisterHotKey(Handle, HOTKEY_ID);
        _kbHook?.Dispose();
        _scrcpy.Stop();
        base.OnFormClosing(e);
    }

    // No mostrar nunca la ventana anfitriona.
    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);
}
