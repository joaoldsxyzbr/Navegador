namespace Navegador.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("Os testes deste repositório só rodam no Windows.");
            return 0;
        }

        var cases = TestRunner.AllCases;

        // Permite rodar um subconjunto: dotnet run -- sessão
        if (args.Length > 0)
        {
            cases = cases.Where(testCase =>
                args.Any(filter => testCase.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)));

            Console.WriteLine($"Filtro: {string.Join(", ", args)}");
        }

        Console.WriteLine("Testes do Navegador");
        Console.WriteLine();
        return TestRunner.Run(cases);
    }
}
