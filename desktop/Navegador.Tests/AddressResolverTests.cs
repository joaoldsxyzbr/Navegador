using Navegador.Core;

namespace Navegador.Tests;

public static class AddressResolverTests
{
    public static IEnumerable<TestCase> Cases =>
    [
        new("URL https é mantida", () =>
        {
            Assert.Equal("https://exemplo.com/pagina", AddressResolver.Resolve("https://exemplo.com/pagina"));
        }),

        new("URL http é mantida", () =>
        {
            Assert.Equal("http://exemplo.com/", AddressResolver.Resolve("http://exemplo.com"));
        }),

        new("espaços nas pontas são ignorados", () =>
        {
            Assert.Equal("https://exemplo.com/", AddressResolver.Resolve("   https://exemplo.com   "));
        }),

        new("domínio sem esquema vira https", () =>
        {
            Assert.Equal("https://exemplo.com", AddressResolver.Resolve("exemplo.com"));
        }),

        new("subdomínio sem esquema vira https", () =>
        {
            Assert.Equal("https://docs.exemplo.com/guia", AddressResolver.Resolve("docs.exemplo.com/guia"));
        }),

        new("localhost vira https", () =>
        {
            Assert.Equal("https://localhost", AddressResolver.Resolve("localhost"));
        }),

        new("localhost com porta vira https", () =>
        {
            Assert.Equal("https://localhost:5000", AddressResolver.Resolve("localhost:5000"));
        }),

        new("texto livre vira busca", () =>
        {
            Assert.Equal(AddressResolver.SearchPrefix + "navegador%20webview2", AddressResolver.Resolve("navegador webview2"));
        }),

        new("palavra única sem ponto vira busca", () =>
        {
            Assert.Equal(AddressResolver.SearchPrefix + "noticias", AddressResolver.Resolve("noticias"));
        }),

        new("acentos são escapados na busca", () =>
        {
            Assert.Equal(AddressResolver.SearchPrefix + "caf%C3%A9", AddressResolver.Resolve("café"));
        }),

        new("entrada vazia não navega", () =>
        {
            Assert.Equal(string.Empty, AddressResolver.Resolve("   "));
        }),

        new("esquema javascript é tratado como busca", () =>
        {
            Assert.StartsWith(AddressResolver.SearchPrefix, AddressResolver.Resolve("javascript:alert(1)"));
        }),

        new("apenas http, https, file e edge são persistáveis", () =>
        {
            Assert.True(AddressResolver.IsPersistable("https://exemplo.com"));
            Assert.True(AddressResolver.IsPersistable("file:///C:/tmp/pagina.html"));
            Assert.False(AddressResolver.IsPersistable("javascript:alert(1)"));
            Assert.False(AddressResolver.IsPersistable("data:text/html,<h1>oi</h1>"));
            Assert.False(AddressResolver.IsPersistable(null));
            Assert.False(AddressResolver.IsPersistable("não é url"));
        }),

        new("host é extraído para exibição", () =>
        {
            Assert.Equal("exemplo.com", AddressResolver.HostOf("https://exemplo.com/a/b"));
            Assert.Equal(string.Empty, AddressResolver.HostOf("nada"));
        })
    ];
}
