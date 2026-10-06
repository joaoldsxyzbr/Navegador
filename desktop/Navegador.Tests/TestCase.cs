namespace Navegador.Tests;

/// <summary>
/// Um caso de teste. O projeto roda sem framework externo para funcionar em
/// qualquer máquina sem acesso ao NuGet; o mesmo arquivo compila no xUnit
/// quando o pacote está disponível.
/// </summary>
public sealed record TestCase(string Name, Action Body);
