# Navegador

Um navegador próprio para Windows, com visual do Chrome e motor WebView2 (Chromium/Edge).

## Baixar e usar

A distribuição Windows é portátil e foi simplificada para deixar o executável evidente.

1. Abra a página de **Releases** do repositório.
2. Baixe o ZIP Windows x64 da versão mais recente.
3. Extraia o ZIP inteiro.
4. Abra **`Navegador.exe`**, que fica diretamente na raiz da pasta extraída.

O aplicativo é publicado como **single-file self-contained**, então o .NET 10 não precisa ser instalado separadamente. O Microsoft Edge WebView2 Runtime continua sendo necessário para renderizar as páginas.

## Onde ficam os dados

O Navegador tenta manter tudo em `Data\` ao lado do executável, para continuar portátil:

| Arquivo | Conteúdo |
| --- | --- |
| `Data\WebView2\` | perfil do motor (cookies, cache, extensões) |
| `Data\session.json` | abas abertas quando o navegador foi fechado |
| `Data\favorites.json` | favoritos |
| `Data\history.json` | histórico de navegação |
| `Data\settings.json` | preferências |

Se essa pasta não puder ser criada — por exemplo, quando o app é extraído dentro de `Program Files` sem permissão de escrita — os dados vão para `%LOCALAPPDATA%\Navegador` e a tela **Configurações** mostra qual modo está em uso. O perfil do WebView2 acompanha a mesma decisão, então nunca ficam dois perfis diferentes em uso.

## Direção

Começamos pelo PC. O app Windows usa C# e WinForms para a interface e WebView2 para carregar sites. O motor é fornecido pelo Microsoft Edge WebView2 Runtime; não compilamos Chromium nem Firefox.

O visual deve reproduzir de perto a organização do Chrome: faixa de abas, barra de endereço, controles, menus e acesso a extensões. O nome e a identidade do produto continuam sendo Navegador.

A linha Android fica para uma fase posterior, depois de validarmos o Windows.

## O que o protótipo já faz

- abas, com criação, troca, fechamento e `Ctrl+Tab`;
- voltar, avançar, recarregar e parar;
- barra de endereço com busca (endereço sem esquema vira `https://`, texto livre vira busca);
- tema escuro e shell próprio, com barra de título desenhada pelo app;
- perfil persistente em `Data\WebView2`;
- **sessão**: as abas voltam na próxima abertura (desligável em Configurações);
- **favoritos**: estrela na barra, barra de favoritos e uma janela para renomear e remover;
- **histórico**: registro das visitas, com busca, remoção e limpeza total;
- **downloads**: pasta de destino configurável, faixa de downloads na parte de baixo e janela com abrir, abrir pasta e cancelar;
- **configurações**: sessão, pasta de downloads, pergunta de destino e página inicial;
- instância única, para duas cópias não escreverem a mesma sessão;
- extensões Chromium locais descompactadas, com lista, ativação, desativação e remoção;
- atualização integrada pelo menu, com consulta ao GitHub Releases, validação SHA-256, **rollback** automático e reinício.

## Limites conhecidos

- **Extensões**: o WebView2 não oferece a Chrome Web Store nem a janela de popup do ícone na barra. Extensões que dependem de popup (gerenciadores de senha, bloqueadores com painel) carregam, mas não podem ser operadas pela interface. A tela de extensões diz isso ao usuário.
- **Abas**: não há favicon, agrupamento, fixação, arrastar para reordenar nem reabrir aba fechada (`Ctrl+Shift+T`).
- **Privacidade**: não há janela anônima nem limpeza de dados pela interface.
- **Sincronização** entre máquinas não existe.
- **Android/iOS**: fora do escopo desta etapa.

## Requisitos para desenvolver

- Windows 10/11 x64;
- .NET 10 SDK;
- Microsoft Edge WebView2 Runtime.

O WebView2 Runtime é pré-instalado no Windows 11 e na maioria dos Windows 10 atualizados. O projeto mostra um erro com o endereço oficial se o Runtime não estiver disponível.

## Estrutura do repositório

```
desktop/
  Navegador.Core/     lógica pura: sessão, favoritos, histórico, configurações,
                      resolução de endereço e o protocolo do atualizador
  Navegador.Windows/  app WinForms + WebView2 (depende do Core)
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

O script grava o `VERSION`, roda os testes, cria o commit e a etiqueta `v0.5.0`, e envia. O workflow **Release Windows** dispara com a etiqueta, confere que ela combina com o `VERSION`, publica o pacote e cria a release. Sem etiqueta, não há release.

## Documentação

- [Arquitetura](docs/ARQUITETURA.md)
- [Requisitos](docs/REQUISITOS.md)
- [Plano](docs/PLANO.md)

## Histórico

- `v0.1.0`: bootstrap Chromium histórico.
- `v0.2.0`: primeira release WebView2, funcional mas com empacotamento self-contained espalhado em muitos arquivos.
- `v0.2.1`: reorganização da distribuição para single-file, deixando `Navegador.exe` evidente na raiz do pacote.
- `v0.3.0`: primeira revisão visual grande do shell, com barra de título própria, abas e omnibox inspiradas no Chrome.
- `v0.4.0`: atualização integrada de um clique usando GitHub Releases.
- `v0.5.0`: separação do `Navegador.Core`, sessão, favoritos, histórico, downloads, configurações, instância única, atualizador com rollback e publicação por etiqueta.
