namespace Mirror;

/// <summary>Diálogos mínimos creados por código (sin diseñador).</summary>
internal static class Dialogs
{
    /// <summary>Pide la contraseña. Devuelve null si se cancela.</summary>
    public static string? AskPassword(string title, string prompt)
    {
        using var f = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false, MinimizeBox = false,
            ClientSize = new Size(360, 130),
            ShowInTaskbar = false,
        };
        var lbl = new Label { Text = prompt, Left = 12, Top = 12, Width = 336, Height = 30 };
        var box = new TextBox { Left = 12, Top = 48, Width = 336, UseSystemPasswordChar = true };
        var ok = new Button { Text = "Aceptar", Left = 192, Top = 88, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancelar", Left = 273, Top = 88, Width = 75, DialogResult = DialogResult.Cancel };
        f.Controls.AddRange(new Control[] { lbl, box, ok, cancel });
        f.AcceptButton = ok; f.CancelButton = cancel;
        return f.ShowDialog() == DialogResult.OK ? box.Text : null;
    }

    /// <summary>Primer arranque: crea contraseña y elige modo de conexión.</summary>
    public static bool FirstRunSetup(AppConfig cfg)
    {
        using var f = new Form
        {
            Text = "Mirror — configuración inicial",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false, MinimizeBox = false,
            ClientSize = new Size(420, 320),
            ShowInTaskbar = false,
        };

        var l1 = new Label { Text = "Crea una contraseña (larga, tipo frase):", Left = 12, Top = 12, Width = 396 };
        var pass1 = new TextBox { Left = 12, Top = 34, Width = 396, UseSystemPasswordChar = true };
        var l2 = new Label { Text = "Repite la contraseña:", Left = 12, Top = 66, Width = 396 };
        var pass2 = new TextBox { Left = 12, Top = 88, Width = 396, UseSystemPasswordChar = true };

        var l3 = new Label { Text = "Modo de conexión al móvil:", Left = 12, Top = 124, Width = 396 };
        var modeUsb = new RadioButton { Text = "USB (máxima privacidad, recomendado)", Left = 24, Top = 146, Width = 384, Checked = true };
        var modeHot = new RadioButton { Text = "Hotspot del móvil (red del edificio queda fuera)", Left = 24, Top = 168, Width = 384 };
        var modeWifi = new RadioButton { Text = "Wi-Fi local (menos privado)", Left = 24, Top = 190, Width = 384 };

        var l4 = new Label { Text = "Dirección ADB del móvil (ip:puerto) — solo hotspot/Wi-Fi:", Left = 12, Top = 218, Width = 396 };
        var addr = new TextBox { Left = 12, Top = 240, Width = 396, PlaceholderText = "192.168.43.1:5555" };

        var ok = new Button { Text = "Guardar", Left = 252, Top = 278, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancelar", Left = 333, Top = 278, Width = 75, DialogResult = DialogResult.Cancel };

        f.Controls.AddRange(new Control[] { l1, pass1, l2, pass2, l3, modeUsb, modeHot, modeWifi, l4, addr, ok, cancel });
        f.AcceptButton = ok; f.CancelButton = cancel;

        while (true)
        {
            if (f.ShowDialog() != DialogResult.OK) return false;
            if (pass1.Text.Length < 8)
            {
                MessageBox.Show("La contraseña debe tener al menos 8 caracteres.", "Mirror");
                continue;
            }
            if (pass1.Text != pass2.Text)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Mirror");
                continue;
            }
            cfg.Mode = modeHot.Checked ? ConnectionMode.Hotspot
                     : modeWifi.Checked ? ConnectionMode.Wireless
                     : ConnectionMode.Usb;
            cfg.DeviceAddress = string.IsNullOrWhiteSpace(addr.Text) ? null : addr.Text.Trim();
            AuthGate.SetPassword(cfg, pass1.Text); // guarda verificador + salt, hace Save()
            return true;
        }
    }
}
