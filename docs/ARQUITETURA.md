# Arquitetura do Navegador

## Decisão atual

Em 05/10/2026, a nova linha do projeto foi reiniciada como um navegador Windows próprio usando **WebView2**. Firefox/Gecko não será a base desta linha. A release v0.1.0 permanece histórica.

## Desktop Windows

- Interface nativa: C# e WinForms sobre .NET 10 LTS.
- Motor: Microsoft Edge WebView2 Runtime (Chromium).
- Perfil: diretório controlado pelo app em `Data/WebView2`.
- Distribuição: objetivo portátil; o protótipo usa o Runtime Evergreen instalado no Windows. A estratégia para PCs sem Runtime será definida antes da primeira release.
- Interface: reproduzir de perto a disposição do Chrome (abas, omnibox, toolbar, menus e acesso a extensões), com identidade própria Navegador.
- Extensões: API WebView2 habilitada. O protótipo pode instalar extensões Chromium a partir de uma pasta local descompactada e gerenciar ativação/remoção.

O WebView2 oferece APIs de instalação e gerenciamento de extensões no perfil, mas não fornece a loja nem todos os pontos de interface do navegador. Extensões com ícone/popup podem exigir UI própria do Navegador; compatibilidade não será presumida como idêntica à versão Chrome.

## Plataforma móvel

Fora da primeira etapa. Android e iOS serão avaliados depois que o MVP Windows funcionar.

## Release histórica

A v0.1.0 foi publicada como bootstrap Chromium. Ela permanece em Releases e no histórico Git. O novo app começa em `0.2.0` e não reaproveita o perfil anterior automaticamente.

## Fontes técnicas

- [WebView2 com WinForms](https://learn.microsoft.com/microsoft-edge/webview2/get-started/winforms)
- [API para instalar uma extensão](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2profile.addbrowserextensionasync)
- [Diferenças entre Edge e WebView2](https://learn.microsoft.com/microsoft-edge/webview2/concepts/browser-features)
- [Runtime Evergreen e versão fixa](https://learn.microsoft.com/microsoft-edge/webview2/concepts/evergreen-vs-fixed-version)
- [.NET: política oficial de suporte](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
