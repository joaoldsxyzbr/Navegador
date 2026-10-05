# Arquitetura do Navegador

## Decisão atual

Em 05/10/2026, a nova linha do projeto passa a ser um navegador Windows próprio usando **WebView2**. Firefox/Gecko deixa de ser a base desta linha. A release v0.1.0 permanece histórica e não será apagada.

## Desktop Windows

- Interface nativa: C# e WinForms sobre .NET 10 LTS.
- Motor: Microsoft Edge WebView2 Runtime (Chromium).
- Perfil: diretório controlado pelo app em `Data/WebView2`.
- Distribuição: objetivo portátil; o protótipo usa o Runtime Evergreen já instalado no Windows. A estratégia para PCs sem Runtime será definida antes da primeira release.
- Interface: tema escuro e organização inspirada no Chrome, sem usar marcas ou recursos proprietários do Chrome.

O Runtime Evergreen é mantido e atualizado pela Microsoft. O app continua responsável pela interface, abas, navegação, dados locais, permissões, downloads e demais recursos de navegador.

## Plataforma móvel

Fora da primeira etapa. Android e iOS serão avaliados depois que o MVP Windows funcionar; nenhuma implementação mobile faz parte deste protótipo.

## Release histórica

A v0.1.0 foi publicada como bootstrap Chromium. Ela permanece em Releases e no histórico Git. O novo app começa em `0.2.0` e não reaproveita o perfil anterior automaticamente.

## Fontes técnicas

- [WebView2 com WinForms](https://learn.microsoft.com/microsoft-edge/webview2/get-started/winforms)
- [Runtime Evergreen e versão fixa](https://learn.microsoft.com/microsoft-edge/webview2/concepts/evergreen-vs-fixed-version)
- [.NET: política oficial de suporte](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
