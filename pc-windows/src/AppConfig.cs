using System.Text.Json;

namespace Mirror;

/// <summary>
/// Configuración del front-end. Se guarda en %APPDATA%\Mirror\config.json.
/// No guarda la contraseña en claro: solo un verificador derivado (ver AuthGate).
/// </summary>
public sealed class AppConfig
{
    /// <summary>Modo de conexión al móvil.</summary>
    public ConnectionMode Mode { get; set; } = ConnectionMode.Usb;

    /// <summary>
    /// En modo Hotspot/Wireless: dirección ADB del móvil (ip:puerto), p. ej.
    /// "192.168.43.1:5555". En modo hotspot suele ser la IP de la pasarela del
    /// hotspot del propio móvil.
    /// </summary>
    public string? DeviceAddress { get; set; }

    /// <summary>
    /// SSID esperado del hotspot del móvil. Si está definido, el PC SOLO conecta
    /// cuando está unido a esa red. "Solo funciona desde esa red".
    /// </summary>
    public string? RequiredSsid { get; set; }

    // --- Disparador para mostrar/ocultar ---
    // Por defecto: Enter del numpad con NumLock activado.
    public ToggleTrigger Trigger { get; set; } = ToggleTrigger.NumpadEnterNumLock;
    public HotkeyConfig ToggleHotkey { get; set; } = HotkeyConfig.Default;

    // --- Ventana del móvil en el PC ---
    public int WindowWidth { get; set; } = 360;
    public int WindowHeight { get; set; } = 780;
    public int MarginRight { get; set; } = 24;
    public int MarginBottom { get; set; } = 24;
    public int AnimationMs { get; set; } = 220;
    public bool StartHidden { get; set; } = true;

    // --- Calidad / latencia de scrcpy ---
    public int MaxSize { get; set; } = 0;          // 0 = nativo; baja p. ej. 1024 si va justo
    public int BitrateMbps { get; set; } = 8;
    public int MaxFps { get; set; } = 60;
    public bool TurnScreenOff { get; set; } = true; // apaga el panel físico del móvil
    public bool StayAwake { get; set; } = true;
    // El flag de orientación cambió entre versiones de scrcpy. Si tu versión lo
    // rechaza, cámbialo aquí: "--lock-video-orientation={0}" o "--capture-orientation={0}".
    public string OrientationFlagFormat { get; set; } = "--orientation={0}";

    // --- Verificador de contraseña (Argon2id), no la contraseña ---
    public string? PasswordVerifierB64 { get; set; }
    public string? PasswordSaltB64 { get; set; }

    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mirror");
    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { /* config corrupta -> valores por defecto */ }
        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public bool IsFirstRun => PasswordVerifierB64 is null;
}

public enum ToggleTrigger
{
    /// <summary>Enter del teclado numérico, solo cuando NumLock está activado.</summary>
    NumpadEnterNumLock,
    /// <summary>Combinación de teclas clásica (Ctrl/Alt/Shift + tecla).</summary>
    Hotkey
}

public enum ConnectionMode
{
    /// <summary>Cable USB: cero exposición de red. Máxima privacidad y latencia mínima.</summary>
    Usb,
    /// <summary>Hotspot del móvil: la red vigilada del edificio queda fuera del circuito.</summary>
    Hotspot,
    /// <summary>Wi-Fi local normal (menos privado; el tráfico va cifrado pero el metadato se ve).</summary>
    Wireless
}

public sealed class HotkeyConfig
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    /// <summary>Virtual-key code. Por defecto Scroll Lock (0x91), como un "NumLock" de toggle.</summary>
    public uint VirtualKey { get; set; } = 0x91;

    public static HotkeyConfig Default => new() { VirtualKey = 0x91 }; // Scroll Lock
}
