using System.Diagnostics;

namespace Mirror;

/// <summary>
/// Comprueba a qué Wi-Fi está unido el PC. Sirve para el modo hotspot:
/// "solo funciona cuando estás en la red del móvil".
/// </summary>
public static class NetworkCheck
{
    /// <summary>True si el PC está conectado a una Wi-Fi cuyo SSID coincide.</summary>
    public static bool IsConnectedToSsid(string ssid)
    {
        var current = CurrentSsid();
        return current is not null &&
               string.Equals(current, ssid, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>SSID actual (o null si no hay Wi-Fi o no se pudo leer).</summary>
    public static string? CurrentSsid()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };
            using var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3000);

            foreach (var raw in output.Split('\n'))
            {
                var line = raw.Trim();
                // Coincide con "SSID" pero no con "BSSID". Idioma-agnóstico por el prefijo.
                int idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var key = line[..idx].Trim();
                if (key.Equals("SSID", StringComparison.OrdinalIgnoreCase))
                    return line[(idx + 1)..].Trim();
            }
        }
        catch { /* sin Wi-Fi o netsh no disponible */ }
        return null;
    }
}
