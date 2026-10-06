using System.Diagnostics;

namespace Navegador.Tests;

/// <summary>
/// Executor de testes sem dependências externas.
///
/// O projeto roda com <c>dotnet run</c> em qualquer máquina, mesmo sem acesso ao
/// NuGet (o runner do xUnit precisa baixar pacotes). Sai com código 1 quando
/// algum caso falha, o que é o suficiente para o CI.
/// </summary>
public static class TestRunner
{
    public static int Run(IEnumerable<TestCase> cases)
    {
        var all = cases.ToList();
        var failures = new List<(TestCase Case, Exception Error)>();
        var stopwatch = Stopwatch.StartNew();

        foreach (var testCase in all)
        {
            try
            {
                testCase.Body();
                Console.WriteLine($"  [ok]    {testCase.Name}");
            }
            catch (Exception exception)
            {
                failures.Add((testCase, exception));
                Console.WriteLine($"  [FALHA] {testCase.Name}");
                Console.WriteLine($"          {exception.Message}");
            }
        }

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"{all.Count - failures.Count}/{all.Count} testes passaram em {stopwatch.ElapsedMilliseconds} ms.");

        if (failures.Count == 0) return 0;

        Console.WriteLine();
        Console.WriteLine("Falhas:");
        foreach (var (testCase, error) in failures)
        {
            Console.WriteLine($"- {testCase.Name}");
            Console.WriteLine($"  {error.GetType().Name}: {error.Message}");
            if (error is not AssertionException) Console.WriteLine(error.StackTrace);
        }

        return 1;
    }

    public static IEnumerable<TestCase> AllCases =>
        AddressResolverTests.Cases
            .Concat(AddressSuggestionTests.Cases)
            .Concat(StoreTests.Cases)
            .Concat(UpdateTests.Cases);
}
