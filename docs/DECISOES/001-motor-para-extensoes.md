# Avaliação do motor para extensões

**Status:** proposta para uma prova de conceito; o Rumo continua usando WebView2.
**Data:** 2026-10-06

## Objetivo

Dar ao Rumo uma experiência de extensões mais próxima à do Brave: instalação pela Chrome Web Store, ícones e popups operáveis, permissões claras e gerenciamento no perfil do navegador.

## Recomendação provisória

Manter WebView2 na versão atual e testar **CEF com CefSharp no Chrome Runtime** em uma prova de conceito isolada. Não migrar o aplicativo antes de demonstrar que a instalação da Chrome Web Store e os popups funcionam dentro da janela e do perfil do Rumo, e de medir os custos de distribuição, atualização e migração de perfil.

Essa é uma recomendação para investigar o candidato, não uma decisão de substituir o motor.

## Comparação

| Alternativa | O que atende | Limites para este projeto |
| --- | --- | --- |
| **WebView2 (atual)** | Continua integrado ao WinForms e ao runtime do Edge; já está instalado e validado no Rumo. | A API pública instala uma pasta local descompactada. O WebView2 não fornece a tela de instalação da Chrome Web Store nem os pontos de interface do navegador para abrir a ação/popup da extensão. |
| **Electron** | Chromium embutido e APIs de extensão úteis para algumas extensões e ferramentas de desenvolvimento. | A documentação oficial descreve um subconjunto das APIs de Chrome Extensions e carregamento por diretório. Não oferece a experiência de loja completa desejada; também exigiria reescrever a casca WinForms em outro stack. |
| **CEF + CefSharp (Chrome Runtime)** | É a alternativa mais próxima mantendo C# e WinForms. A documentação do CEF descreve o Chrome Runtime como a camada de UI completa do Chrome, com APIs de extensões e diálogos próprios; CefSharp fornece o controle WinForms. | A integração precisa ser provada na versão atual. Há um relato de instalação pela Chrome Web Store que abriu outro processo Chromium; o mantenedor indicou que a interface de extensões depende do modo Chrome Styled. A compatibilidade de cada extensão, a instalação no perfil incorporado e o comportamento dos popups não estão garantidos por essa evidência. |

## Evidências e limites

- A API do WebView2 recebe o caminho de uma extensão local descompactada. A documentação da Microsoft também lista diferenças em relação ao Edge completo. Isso confirma o limite do motor atual para entregar a loja e a interface de extensões.
- A arquitetura do CEF diz que o Chrome Runtime inclui APIs avançadas como extensões e componentes de UI do Chrome. CefSharp permite usar o runtime Chrome Styled em WinForms, mas depende de uma combinação específica de controle, versão e configuração.
- Em março de 2025, um relato no repositório do CefSharp descreveu o botão “Add to Chrome” abrindo um processo Chromium independente. O mantenedor respondeu que as extensões devem funcionar no Chrome Styled; o relato não comprova instalação integrada no Rumo nem o comportamento da versão atual.
- Electron declara suporte a apenas parte das APIs de Chrome Extensions. Por isso, não é o primeiro candidato a testar para este requisito.

## Custos que a prova de conceito precisa medir

- **Distribuição:** CefSharp traz binários nativos, subprocessos e dependência do runtime Visual C++ redistribuível. Medir tamanho do instalador, publicação single-file, inicialização e consumo de memória.
- **Atualizações de segurança:** com CEF, o projeto passa a distribuir e atualizar sua própria versão de Chromium. Registrar como a versão do motor será atualizada e publicada com correções de segurança.
- **Perfil existente:** o perfil `Data\WebView2` não deve ser tratado como compatível com um perfil CEF. Favoritos, sessão e preferências do Rumo são dados próprios; cookies, cache, senhas e estado de extensões exigem uma política de migração explícita. Não presumir cópia direta nem conversão automática.
- **Recursos atuais:** validar downloads, múltiplos perfis, InPrivate, sessão, PDF/impressão, permissões, aceleração gráfica, links externos e fechamento limpo dos processos.

## Critérios de aprovação da prova de conceito

1. Compila e roda em Windows x64 com .NET 10 usando a estrutura WinForms existente.
2. Abre uma página da Chrome Web Store e conclui “Adicionar ao Chrome” dentro da janela e do perfil do Rumo, sem abrir uma sessão Chromium independente.
3. Após reiniciar, a extensão continua instalada e pode ser ativada, desativada e removida pelo Rumo.
4. A ação e o popup de pelo menos duas extensões Manifest V3 aparecem e funcionam; testar também permissões e acesso a sites.
5. O processo de instalação mostra a origem e as permissões pedidas antes de habilitar a extensão.
6. A publicação x64 e o fluxo de atualização do Rumo continuam reproduzíveis, com tamanho e dependências documentados.
7. A política para os dados do perfil antigo está escrita antes de qualquer migração de usuários.

Se qualquer um dos itens 2 a 5 falhar, não declarar suporte à Chrome Web Store. Nesse caso, manter o motor atual enquanto se decide se vale desenvolver e manter um fluxo próprio de catálogo e instalação.

## Fontes primárias

- [Microsoft: AddBrowserExtension no WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/win32/icorewebview2profile#addbrowserextension)
- [Microsoft: diferenças entre WebView2 e o Edge completo](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/edge-webview2-differences)
- [CEF: arquitetura e Chrome Runtime](https://github.com/chromiumembedded/cef/blob/master/docs/architecture.md)
- [CefSharp: repositório, versões e suporte a WinForms](https://github.com/cefsharp/CefSharp)
- [CefSharp: discussão sobre Chrome Runtime](https://github.com/cefsharp/CefSharp/discussions/4123)
- [CefSharp: relato sobre instalação pela Chrome Web Store](https://github.com/cefsharp/CefSharp/issues/5067)
- [Electron: suporte a extensões](https://www.electronjs.org/docs/latest/api/extensions)
- [Chrome for Developers: métodos alternativos de instalação](https://developer.chrome.com/docs/extensions/how-to/distribute/install-extensions)
