namespace Navegador.Core;

public static class VisualCppRuntimePolicy
{
    public const int MinimumMajorVersion = 14;
    public const int MinimumMinorVersion = 30;

    public static bool IsSupported(int installed, int major, int minor) =>
        installed == 1 &&
        (major > MinimumMajorVersion ||
         major == MinimumMajorVersion && minor >= MinimumMinorVersion);
}
