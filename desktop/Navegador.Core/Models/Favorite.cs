namespace Navegador.Core.Models;

/// <summary>Uma página salva nos favoritos.</summary>
public sealed class Favorite
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.Now;

    public static Favorite Create(string url, string? title) => new()
    {
        Url = url,
        Title = string.IsNullOrWhiteSpace(title) ? url : title,
        AddedAt = DateTimeOffset.Now
    };
}
