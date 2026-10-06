# Arquitetura do Navegador

## Decisão atual

A linha do projeto é um navegador Windows próprio usando **WebView2**. Firefox/Gecko não é a base desta linha. A release v0.1.0 permanece histórica.

## Projetos

O repositório tem três projetos, separados pelo que cada um pode testar:

| Projeto | TFM | Responsabilidade |
| --- | --- | --- |
| `Navegador.Core` | `net10.0` | lógica pura: caminhos de dados, sessão, favoritos, histórico, configurações, resolução de endereço, leitura do releases do GitHub e o protocolo do atualizador |
| `Navegador.Windows` | `net10.0-windows` | interface WinForms, WebView2, downloads e o auxiliar de atualização |
| `Navegador.Tests` | `net10.0` | testes do Core |

O `Core` não referencia WinForms nem WebView2. Essa é a razão da separação: tudo que dá para testar sem abrir uma janela mora lá, e o app depende dele. A direção da dependência é sempre `Windows → Core`; nada no Core conhece a interface.

## Desktop Windows

- Interface nativa: C# e WinForms sobre .NET 10.
- Motor: Microsoft Edge WebView2 Runtime (Chromium).
- Shell: barra de título própria (`FormBorderStyle.None`), com redimensionamento e cursor de borda implementados em `WM_NCHITTEST`/`WM_SETCURSOR`, abas e omnibox.
- Instância única via mutex nomeado: duas cópias não podem escrever a mesma sessão nem o mesmo perfil.
- Distribuição: pacote portátil x64; o Runtime Evergreen é o que o Windows já oferece.

## Dados e persistência

Todos os dados ficam em um único diretório, decidido em `AppPaths`:

1. `Data\` ao lado do executável, se der para escrever ali (modo portátil, o padrão);
2. `%LOCALAPPDATA%\Navegador`, quando a primeira opção não é gravável.

A gravação de cada arquivo JSON é **atômica**: escreve em `.tmp` e move por cima. Se o navegador morrer no meio, o arquivo anterior continua íntegro em vez de virar JSON truncado. Leitura de arquivo corrompido nunca derruba o app: devolve o estado vazio.

## Sessão

Ao fechar, as abas vivas viram `session.json`. Ao abrir, o app restaura abas, ordem e a aba ativa — se o usuário não tiver desligado a opção nas configurações. URLs não navegáveis (`javascript:`, `data:`) são descartadas na gravação e na leitura, e o índice da aba ativa é recalculado para continuar apontando para a mesma aba depois do descarte.

## Extensões

O WebView2 oferece APIs de instalação e gerenciamento de extensões no perfil, mas não fornece a loja nem os pontos de interface do navegador (ícone na barra, popup, páginas de opções). O protótipo instala extensões Chromium de uma pasta local descompactada e gerencia ativação e remoção. Compatibilidade com a Chrome Web Store não é presumida, e a própria tela de extensões informa o limite.

## Atualização

O fluxo tem duas fases e duas identidades:

1. **Processo principal**: consulta `releases/latest`, compara versões (apenas as três partes visíveis — a versão do assembly tem quatro), baixa o pacote para `%LOCALAPPDATA%\Navegador\Updates\<versão>-<guid>`, confere o SHA-256 publicado e copia a si mesmo para `Navegador.Atualizador.exe` **fora da pasta de instalação**.
2. **Auxiliar**: roda como `Navegador.exe --apply-update`, espera o processo pai encerrar, extrai o pacote, confere o hash **de novo**, valida a assinatura PE do executável e substitui os arquivos.

O auxiliar não confia nos argumentos que recebe. Ele só age quando encontra o combinado (`update.json`) gravado pelo processo pai, com o mesmo token e o mesmo PID, quando o diretório de destino é exatamente a pasta da própria instalação e quando o pacote está dentro da pasta de atualizações do Navegador. Sem isso, um processo local de baixo privilégio poderia pedir para o Navegador descompactar um ZIP forjado por cima de qualquer pasta com o token dele.

Antes de cada substituição, o arquivo antigo é copiado para uma pasta de backup. Qualquer falha de cópia dispara a restauração completa, então uma atualização malsucedida deixa a instalação anterior funcionando. O perfil em `Data\` nunca é substituído pelo pacote.

## Publicação

A versão existe uma única vez, no arquivo `VERSION`. O `Directory.Build.props` da raiz lê esse arquivo e alimenta `Version`, `AssemblyVersion`, `FileVersion` e `InformationalVersion` de todos os projetos. O workflow de release lê o mesmo arquivo e publica apenas quando uma etiqueta `vX.Y.Z` é enviada, conferindo que a etiqueta combina com o `VERSION`.

## Plataforma móvel

Fora da primeira etapa. Android e iOS serão avaliados depois que o MVP Windows estiver estável.

## Release histórica

A v0.1.0 foi publicada como bootstrap Chromium. Ela permanece em Releases e no histórico Git. O app começou a nova linha em `0.2.0` e não reaproveita o perfil anterior automaticamente.

## Fontes técnicas

- [WebView2 com WinForms](https://learn.microsoft.com/microsoft-edge/webview2/get-started/winforms)
- [API para instalar uma extensão](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2profile.addbrowserextensionasync)
- [Diferenças entre Edge e WebView2](https://learn.microsoft.com/microsoft-edge/webview2/concepts/browser-features)
- [Runtime Evergreen e versão fixa](https://learn.microsoft.com/microsoft-edge/webview2/concepts/evergreen-vs-fixed-version)
- [.NET: política oficial de suporte](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
