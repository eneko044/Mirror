using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Mirror;

/// <summary>
/// Contraseña de la app. NO se guarda la contraseña: se guarda un verificador
/// derivado con Argon2id (memoria alta, resistente a fuerza bruta por GPU).
/// La primera vez se define; después se comprueba.
///
/// Nota: esta contraseña bloquea el arranque del front-end en el PC. El cifrado
/// del enlace móvil↔PC en modo hotspot/wireless lo aporta el propio transporte
/// (ver docs/SECURITY.md); en USB el enlace es físico y no toca la red.
/// </summary>
public static class AuthGate
{
    // Parámetros Argon2id (deben coincidir si algún día se comparte con el móvil).
    private const int Iterations = 3;
    private const int MemoryKiB = 64 * 1024; // 64 MiB
    private const int Parallelism = 1;
    private const int HashLen = 32;
    private const int SaltLen = 16;

    public static byte[] Derive(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            Iterations = Iterations,
            MemorySize = MemoryKiB, // en KiB
        };
        return argon2.GetBytes(HashLen);
    }

    public static void SetPassword(AppConfig cfg, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLen);
        var verifier = Derive(password, salt);
        cfg.PasswordSaltB64 = Convert.ToBase64String(salt);
        cfg.PasswordVerifierB64 = Convert.ToBase64String(verifier);
        cfg.Save();
    }

    public static bool Verify(AppConfig cfg, string password)
    {
        if (cfg.PasswordSaltB64 is null || cfg.PasswordVerifierB64 is null) return false;
        var salt = Convert.FromBase64String(cfg.PasswordSaltB64);
        var expected = Convert.FromBase64String(cfg.PasswordVerifierB64);
        var actual = Derive(password, salt);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
