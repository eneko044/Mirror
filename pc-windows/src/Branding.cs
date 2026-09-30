namespace Mirror;

/// <summary>
/// Nombre visible de la app en el PC. Neutro a propósito: es lo que aparece en
/// el Administrador de tareas, la carpeta de configuración y el mutex.
///
/// Cámbialo por lo que quieras (algo genérico y discreto). NO lo hagas pasar por
/// un proceso del sistema (svchost, RuntimeBroker, etc.): los antivirus marcan
/// justo eso y llama más la atención.
///
/// El nombre del PROCESO que se ve en los registros es el nombre del ARCHIVO
/// .exe: renombra el ejecutable a lo que prefieras y el proceso hereda ese nombre.
/// </summary>
public static class Branding
{
    public const string AppName = "LocalPanel";
}
