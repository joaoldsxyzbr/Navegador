# Rumo

O Rumo é um navegador para Windows feito em C# / WinForms, com Chromium incorporado por meio do CefSharp.

<img src="assets/rumo-mark.svg" width="112" alt="Logo Rumo — uma bússola azul e ciano">

[Identidade visual](docs/IDENTIDADE-VISUAL.md)

## Baixar e usar

Cada release oferece duas opções: instalador por usuário e ZIP portátil.

- **Instalador**: execute o arquivo `Rumo-v…-setup.exe` e siga as etapas. Ele cria atalhos no menu Iniciar e, se você escolher, na área de trabalho. Se o runtime C++ necessário não estiver presente, o Windows pedirá autorização para instalá-lo.
- **Portátil**: baixe `Rumo-v…-windows-x64-portable.zip` e extraia. Abra `Abrir Rumo.cmd`; os arquivos do Chromium ficam juntos dentro da pasta `Rumo`.

Os pacotes incluem o .NET, CefSharp, Chromium e o instalador oficial do runtime Visual C++ 2022 x64. Não exigem o Microsoft Edge WebView2 Runtime. O ZIP contém todos esses arquivos; extraia a pasta inteira e mantenha-os juntos.

O arquivo `Rumo-v…-windows-x64.zip` é o pacote plano usado pelo atualizador interno. Para uso portátil manual, escolha o ZIP terminado em `-portable.zip`, que mantém os arquivos dentro de uma pasta `Rumo` e inclui um iniciador visível.

## Onde ficam os dados

O Rumo tenta manter tudo em `Data\` ao lado do executável, para continuar portátil:

| Arquivo | Conteúdo |
| --- | --- |
| `Data\Chromium\Profile\` | perfil do motor (cookies, cache e extensões) |
| `Data\session.json` | abas abertas quando o navegador foi fechado |
| `Data\favorites.json` | favoritos |
| `Data\history.json` | histórico de navegação |
| `Data\settings.json` | preferências |

No ZIP portátil, os dados ficam junto do executável em `Data\`. No instalador por usuário, ficam em `%LOCALAPPDATA%\Navegador`, fora da pasta instalada. Se não puder gravar na pasta portátil, o Rumo usa o mesmo perfil local. A tela **Configurações** mostra qual modo está em uso. O perfil antigo `Data\WebView2` não é convertido automaticamente; ele permanece no disco sem ser alterado. Desinstalar o aplicativo preserva os dados do usuário.

## Direção

O app Windows usa C# e WinForms para a interface e CefSharp para incorporar uma versão pré-compilada do Chromium. O projeto não compila o Chromium no CI.

O visual mantém a organização familiar do Chrome: faixa de abas, barra de endereço, controles, menus e acesso a extensões. A marca do produto é Rumo; o repositório e o executável continuam com o nome técnico Navegador nesta versão.

A linha Android fica para uma fase posterior, depois de validarmos o Windows.

Decisões relevantes de arquitetura, UX, segurança e manutenção seguem um processo de comparação com projetos open source maduros, documentado em [Referências open source](docs/REFERENCIAS-OPEN-SOURCE.md). A ideia é aprender com soluções já testadas sem copiar funcionalidades ou complexidade que não façam sentido para o Rumo.

## O que o protótipo já faz

- abas com criação, troca, fechamento, favicons, fixação pelo menu de contexto e reordenação por arraste;
- modo de tela cheia com `F11` e saída por `Esc`;
- sugestões de favoritos e histórico na barra de endereço, navegáveis pelas setas do teclado;
- voltar, avançar, recarregar e parar;
- barra de endereço com busca (endereço sem esquema vira `https://`, texto livre vira busca);
- tema escuro e shell próprio, com barra de título desenhada pelo app;
- perfil persistente em `Data\Chromium\Profile`;
- **sessão**: as abas voltam na próxima abertura (desligável em Configurações);
- **favoritos**: estrela na barra, barra de favoritos e uma janela para renomear e remover;
- **histórico**: registro das visitas, com busca, remoção e limpeza total;
- **privacidade**: janela InPrivate que não salva histórico nem sessão e não compartilha cookies com o perfil normal; limpeza dos dados do perfil com confirmação;
- **downloads**: pasta de destino configurável, faixa de downloads na parte de baixo e janela com abrir, abrir pasta e cancelar;
- **configurações**: sessão, pasta de downloads, pergunta de destino e página inicial;
- instância única, para duas cópias não escreverem a mesma sessão;
- gerenciador interno de extensões Chromium em `chrome://extensions`, com modo de desenvolvedor disponível para extensões descompactadas;
- atualização integrada por **botão visível na barra superior** e pelo menu, com consulta ao GitHub Releases, validação SHA-256, **rollback** automático e reinício;
- nova guia própria, limpa e escura, com pesquisa central e atalhos dos favoritos;
- `Ctrl+Shift+T` para reabrir a última guia fechada.

## Limites conhecidos

- **Extensões**: o Rumo abre o gerenciador interno do Chromium e preserva janelas de popup de extensão. Compatibilidade e instalação pela Chrome Web Store dependem das APIs e políticas de cada extensão e ainda precisam de validação prática nesta distribuição.
- **Abas**: ainda não há grupos de abas; reabrir aba fechada funciona com `Ctrl+Shift+T`.
- **Instalador**: a instalação é por usuário e não tem assinatura de código nesta release.
- **Sincronização** entre máquinas não existe.
- **Android/iOS**: fora do escopo desta etapa.

## Requisitos para desenvolver

- Windows 10/11 x64;
- .NET 10 SDK;
- Nenhum runtime de navegador separado: o Chromium é distribuído junto com o app.

## Estrutura do repositório

```
desktop/
  Navegador.Core/     lógica pura: sessão, favoritos, histórico, configurações,
                      resolução de endereço e o protocolo do atualizador
  Navegador.Windows/  app WinForms + CefSharp/Chromium (depende do Core)
  Navegador.Tests/    testes do Core, sem dependências externas
docs/                 arquitetura, requisitos e plano
scripts/release.ps1   publica uma versão
VERSION               versão do produto (fonte única)
```

## Executar em desenvolvimento

No PowerShell, na raiz do repositório:

```powershell
dotnet run --project desktop/Navegador.Windows/Navegador.Windows.csproj
```

## Rodar os testes

```powershell
dotnet run --project desktop/Navegador.Tests/Navegador.Tests.csproj
```

O runner é um executável simples, sem `xUnit` nem `Microsoft.NET.Test.Sdk`: assim os testes rodam em qualquer máquina, mesmo sem acesso ao NuGet. Ele sai com código 1 quando algum caso falha, o que basta para o CI. Para rodar só um grupo:

```powershell
dotnet run --project desktop/Navegador.Tests/Navegador.Tests.csproj -- sessão
```

## Publicar uma versão

A versão do produto existe em um único lugar: o arquivo `VERSION`. O MSBuild lê esse arquivo (via `Directory.Build.props`), o executável recebe a versão e o workflow de release usa o mesmo número — não há versão duplicada no `csproj` nem no YAML.

```powershell
pwsh scripts/release.ps1 0.5.0 -Push
```

O script grava o `VERSION`, roda os testes, cria o commit e a etiqueta `v0.5.0`, e envia. O workflow **Release Windows** dispara com a etiqueta, confere que ela combina com o `VERSION`, publica o pacote e cria a release.

Como alternativa operacional pelo próprio GitHub, uma branch `release/vX.Y.Z` apontando para o commit final também dispara o mesmo workflow. Ele valida `VERSION`, cria a tag `vX.Y.Z` ao publicar a release e mantém o pacote gerado vinculado ao commit exato.

## Documentação

- [Arquitetura](docs/ARQUITETURA.md)
- [Requisitos](docs/REQUISITOS.md)
- [Plano](docs/PLANO.md)
- [Referências open source](docs/REFERENCIAS-OPEN-SOURCE.md)
- [Avisos de terceiros](THIRD-PARTY-NOTICES.md)

## Histórico

- `v0.1.0`: bootstrap Chromium histórico.
- `v0.2.0`: primeira release WebView2, funcional mas com empacotamento self-contained espalhado em muitos arquivos.
- `v0.2.1`: reorganização da distribuição para single-file, deixando `Navegador.exe` evidente na raiz do pacote.
- `v0.3.0`: primeira revisão visual grande do shell, com barra de título própria, abas e omnibox inspiradas no Chrome.
- `v0.4.0`: atualização integrada de um clique usando GitHub Releases.
- `v0.5.0`: marca Rumo, correção da maximização, modo InPrivate, limpeza de dados, favicons e controles de abas, sugestões na omnibox, tela cheia e instalador opcional; inclui também o Core separado, sessão, favoritos, histórico, downloads, nova guia e atualização integrada.
- `v0.6.0`: motor CefSharp/Chromium distribuído com o app, perfil independente, suporte à página interna de extensões e pacotes ZIP/instalador com os arquivos nativos do Chromium.
- `v0.6.1`: inclui o runtime Microsoft Visual C++ 2022 x64 exigido pelo CefSharp; o setup verifica e instala esse pré-requisito quando necessário.
- `v0.6.2`: organiza o ZIP portátil em uma pasta `Rumo` com iniciador visível, mantendo separado o pacote plano usado pelo atualizador interno.
