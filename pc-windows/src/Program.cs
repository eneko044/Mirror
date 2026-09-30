using System.Diagnostics;

namespace Mirror;

internal static class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    private static void Main()
    {
        // Una sola instancia.
        _singleInstance = new Mutex(true, "Local_SingleInstance_9f3a", out bool isNew);
        if (!isNew) return;

        ApplicationConfiguration.Initialize();

        var cfg = AppConfig.Load();

        // --- Primer arranque: contraseña + modo de conexión ---
        if (cfg.IsFirstRun)
        {
            if (!Dialogs.FirstRunSetup(cfg)) return;
        }
        else
        {
            // Puerta de contraseña con bloqueo por intentos.
            int attempts = 0;
            while (true)
            {
                var pw = Dialogs.AskPassword(Branding.AppName, "Introduce la contraseña:");
                if (pw is null) return; // cancelado
                if (AuthGate.Verify(cfg, pw)) break;
                if (++attempts >= 5)
                {
                    MessageBox.Show("Demasiados intentos. Se cierra.", Branding.AppName);
                    return;
                }
                MessageBox.Show($"Contraseña incorrecta ({attempts}/5).", Branding.AppName);
            }
        }

        // --- Herramientas (adb.exe, scrcpy.exe) junto al ejecutable, en \tools ---
        string toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
        if (!File.Exists(Path.Combine(toolsDir, "scrcpy.exe")) ||
            !File.Exists(Path.Combine(toolsDir, "adb.exe")))
        {
            MessageBox.Show(
                "No encuentro scrcpy.exe / adb.exe en la carpeta 'tools'.\n" +
                "Descarga scrcpy (incluye adb) y copia su contenido en:\n" + toolsDir,
                Branding.AppName + " — faltan herramientas");
            return;
        }

        // --- Comprobación de red en modo hotspot: "solo desde esa red" ---
        if (cfg.Mode == ConnectionMode.Hotspot && !string.IsNullOrWhiteSpace(cfg.RequiredSsid))
        {
            if (!NetworkCheck.IsConnectedToSsid(cfg.RequiredSsid!))
            {
                MessageBox.Show(
                    $"No estás conectado al hotspot esperado ('{cfg.RequiredSsid}').\n" +
                    "Conéctate a esa red y vuelve a abrir.", Branding.AppName);
                return;
            }
        }

        var scrcpy = new ScrcpyController(cfg, toolsDir);

        // Preparar conexión ADB (USB / hotspot / wifi) antes de arrancar el espejo.
        string? err = scrcpy.PrepareConnectionAsync().GetAwaiter().GetResult();
        if (err is not null)
        {
            MessageBox.Show(err, Branding.AppName + " — conexión");
            return;
        }

        var host = new HostForm(cfg, scrcpy);
        using var tray = BuildTray(cfg, host);

        // Fuerza la creación del handle (arranca hook/atajo y scrcpy) sin mostrar la ventana.
        _ = host.Handle;

        Application.Run(); // bucle de mensajes; la ventana anfitriona es invisible
        tray.Visible = false;
    }

    private static NotifyIcon BuildTray(AppConfig cfg, HostForm host)
    {
        var menu = new ContextMenuStrip();
        string toggleText = cfg.Trigger == ToggleTrigger.NumpadEnterNumLock
            ? "Mostrar/Ocultar (Enter numpad + NumLock)"
            : "Mostrar/Ocultar";
        menu.Items.Add(toggleText, null, (_, _) => host.Invoke(new Action(host.TogglePanelPublic)));
        menu.Items.Add("Voltear / Rotar", null, (_, _) => host.Invoke(new Action(host.FlipPanel)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => { host.Close(); Application.Exit(); });

        return new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = Branding.AppName,
            ContextMenuStrip = menu,
        };
    }
}
