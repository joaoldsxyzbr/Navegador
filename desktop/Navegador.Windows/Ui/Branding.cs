namespace Navegador.Windows.Ui;

internal static class Branding
{
    public const string Name = "Rumo";

    private const string MarkResourceName = "Rumo.Mark.svg";
    private static readonly Lazy<string> Mark = new(ReadMark);

    public static string MarkSvg => Mark.Value;

    private static string ReadMark()
    {
        using var stream = typeof(Branding).Assembly.GetManifestResourceStream(MarkResourceName)
            ?? throw new InvalidOperationException("O logo Rumo não foi incluído no executável.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
