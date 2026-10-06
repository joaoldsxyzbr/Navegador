# Decisão de arquitetura: motor do Rumo

**Data:** 6 de outubro de 2026  
**Estado:** decisão para a arquitetura Windows atual

## Decisão

Manter o Microsoft Edge WebView2 como motor do Rumo enquanto o produto usa o shell WinForms existente. Não migrar as abas para CefSharp/CEF sem uma integração de sandbox do Windows suportada e validada.

A PoC de CefSharp continua útil para avaliar APIs e comportamento de extensões, mas não prova que o motor está pronto para uso de produção.

## Motivo

O Rumo abre conteúdo arbitrário da web. O isolamento dos processos de renderização é, portanto, um requisito do motor, junto com compatibilidade, atualização e distribuição.

A documentação do CefSharp declara que o sandbox do Chromium não está implementado e que adicioná-lo diretamente ao CefSharp é tecnicamente inviável no modelo .NET atual. O sandbox do CEF no Windows requer integração nativa com a biblioteca de sandbox e a informação de inicialização correspondente. A PoC existente não inclui essa integração.

O Chrome for Testing também não é uma distribuição para navegação regular: o Google o destina a testes e informa que ele não recebe atualização automática. Usá-lo como runtime portátil não resolveria o requisito de manutenção do Rumo.

O WebView2 preserva o isolamento de navegador fornecido pelo runtime do Edge e seu canal Evergreen de atualização. Sua API pública de extensões instala pastas locais já descompactadas. Isso não equivale à instalação pela Chrome Web Store, nem oferece a interface completa de ícones e pop-ups de extensão.

## Alternativas avaliadas

| Opção | Segurança e manutenção | Extensões | Decisão |
| --- | --- | --- | --- |
| WebView2 no shell atual | Integração suportada; runtime Evergreen; isolamento do Chromium | Extensões locais descompactadas; sem fluxo completo da Chrome Web Store e sem pop-ups na barra do Rumo | Manter |
| CefSharp com CEF Chrome Runtime | A versão examinada do CefSharp não implementa sandbox do Chromium; empacotamento exige vários arquivos nativos e subprocessos | CEF expõe um subconjunto da API de extensões; não é o Chrome completo | Não usar em produção |
| Chrome for Testing | Build versionado sem atualização automática, declarado para automação e testes | Browser completo para testes, mas sem ciclo de atualização para navegação regular | Não usar como runtime de produção |

## Consequências

- O Rumo continua em C# / WinForms / .NET 10 + WebView2. O perfil, o pacote portátil e o atualizador não mudam de formato por causa da PoC.
- A tela de extensões deve explicar claramente a instalação de pastas locais e os limites atuais, sem sugerir que a loja instala extensões no Rumo.
- Melhorias de navegação, configurações e gerenciamento das extensões locais podem continuar no shell atual.
- Se a Chrome Web Store e os pop-ups completos se tornarem requisitos obrigatórios, será necessária uma decisão de replatforming para um navegador Chromium completo. Essa proposta deve provar sandbox no Windows, atualização segura, distribuição portátil, instalação de extensões e migração dos dados antes de substituir o motor.

## Critério para reavaliar CefSharp

Uma nova tentativa com CefSharp só deve ser considerada após existir um protótipo que demonstre, em uma instalação limpa do Windows:

1. sandbox de renderer e subprocessos habilitado por integração nativa suportada;
2. atualização do Chromium sem substituir ou perder dados do perfil;
3. publicação e atualização do pacote portátil pelo CI;
4. instalação, execução, ícone, pop-up e permissões de uma extensão representativa da Chrome Web Store;
5. recuperação do perfil WebView2, sem apagar os dados legados.

## Referências primárias

- [CefSharp — General Usage](https://github.com/cefsharp/CefSharp/wiki/General-Usage) — limitações de extensões e ausência de sandbox.
- [CEF — Windows Sandbox Setup](https://bitbucket.org/chromiumembedded/cef/wiki/SandboxSetup) e [CEF settings](https://github.com/chromiumembedded/cef/blob/master/include/internal/cef_types.h) — requisitos da integração de sandbox no Windows.
- [Chrome for Testing](https://developer.chrome.com/docs/automation-and-testing/chrome-for-testing/) — finalidade de testes e ausência de atualização automática.
- [WebView2 — segurança](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security).
- [WebView2 — adicionar uma extensão local](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2profile.addbrowserextensionasync).
- [Chromium — Site Isolation](https://www.chromium.org/developers/design-documents/site-isolation/) — papel dos renderers isolados e do sandbox.
