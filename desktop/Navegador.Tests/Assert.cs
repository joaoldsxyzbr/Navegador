namespace Navegador.Tests;

/// <summary>Asserções mínimas, equivalentes às do xUnit.</summary>
internal static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition) throw new AssertionException(message ?? "Esperava verdadeiro, veio falso.");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition) throw new AssertionException(message ?? "Esperava falso, veio verdadeiro.");
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException(
                message ?? $"Esperava <{Format(expected)}>, veio <{Format(actual)}>.");
        }
    }

    public static void NotEqual<T>(T notExpected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
        {
            throw new AssertionException(message ?? $"Não esperava <{Format(actual)}>.");
        }
    }

    public static void Null(object? value, string? message = null)
    {
        if (value is not null) throw new AssertionException(message ?? $"Esperava nulo, veio <{value}>.");
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value is null) throw new AssertionException(message ?? "Esperava um valor, veio nulo.");
    }

    public static void Empty<T>(IEnumerable<T> values, string? message = null)
    {
        if (values.Any()) throw new AssertionException(message ?? "Esperava uma coleção vazia.");
    }

    public static void Single<T>(IEnumerable<T> values, string? message = null)
    {
        var count = values.Count();
        if (count != 1) throw new AssertionException(message ?? $"Esperava um item, veio {count}.");
    }

    public static void StartsWith(string expectedPrefix, string? actual, string? message = null)
    {
        if (actual is null || !actual.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Esperava começar com <{expectedPrefix}>, veio <{Format(actual)}>.");
        }
    }

    public static void EndsWith(string expectedSuffix, string? actual, string? message = null)
    {
        if (actual is null || !actual.EndsWith(expectedSuffix, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Esperava terminar com <{expectedSuffix}>, veio <{Format(actual)}>.");
        }
    }

    public static TException Throws<TException>(Action action, string? message = null)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException expected)
        {
            return expected;
        }
        catch (Exception other)
        {
            throw new AssertionException(
                message ?? $"Esperava {typeof(TException).Name}, veio {other.GetType().Name}: {other.Message}");
        }

        throw new AssertionException(message ?? $"Esperava {typeof(TException).Name}, mas nada foi lançado.");
    }

    private static string Format<T>(T value) => value switch
    {
        null => "nulo",
        string text => $"\"{text}\"",
        _ => value.ToString() ?? "?"
    };
}

/// <summary>Falha de asserção. Separada para o runner distinguir de erro inesperado.</summary>
public sealed class AssertionException(string message) : Exception(message);
