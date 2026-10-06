# Navegador

Um navegador próprio para Windows, com visual do Chrome e motor WebView2 (Chromium/Edge).

## Baixar e usar

A distribuição Windows é portátil e foi simplificada para deixar o executável evidente.

1. Abra a página de **Releases** do repositório.
2. Baixe o ZIP Windows x64 da versão mais recente.
3. Extraia o ZIP inteiro.
4. Abra **`Navegador.exe`**, que fica diretamente na raiz da pasta extraída.

O aplicativo é publicado como **single-file self-contained**, então o .NET 10 não precisa ser instalado separadamente. O Microsoft Edge WebView2 Runtime continua sendo necessário para renderizar as páginas.

O perfil é criado em `Data/WebView2` ao lado do executável para manter o uso portátil.

## Direção

Começamos pelo PC. O app Windows usa C# e WinForms para a interface e WebView2 para carregar sites. O motor é fornecido pelo Microsoft Edge WebView2 Runtime; não compilamos Chromium nem Firefox.

O visual deve reproduzir de perto a organização do Chrome: faixa de abas, barra de endereço, controles, menus e acesso a extensões. O nome e a identidade do produto continuam sendo Navegador.

A linha Android fica para uma fase posterior, depois de validarmos o Windows.

## Protótipo atual

- abas;
- voltar, avançar e recarregar;
- barra de endereço com busca;
- tema escuro;
- perfil persistente em `Data/WebView2`;
- suporte experimental a extensões Chromium locais descompactadas, com lista, ativação, desativação e remoção.

A instalação pela Chrome Web Store e a interface de popup/ícones das extensões ainda precisam de trabalho específico. O WebView2 não fornece esses pontos de interface de navegador automaticamente.

Ainda faltam favoritos, histórico visível, gerenciador de downloads, janela privada, configurações e atualizador integrado.

## Requisitos para desenvolver

- Windows 10/11 x64;
- .NET 10 SDK;
- Microsoft Edge WebView2 Runtime.

O WebView2 Runtime é pré-instalado no Windows 11 e na maioria dos Windows 10 atualizados. O projeto mostra um erro com o endereço oficial se o Runtime não estiver disponível.

## Executar em desenvolvimento

No PowerShell, na raiz do repositório:

```powershell
dotnet run --project desktop/Navegador.Windows/Navegador.Windows.csproj
```

## Documentação

- [Arquitetura](docs/ARQUITETURA.md)
- [Requisitos](docs/REQUISITOS.md)
- [Plano](docs/PLANO.md)

## Histórico

- `v0.1.0`: bootstrap Chromium histórico.
- `v0.2.0`: primeira release WebView2, funcional mas com empacotamento self-contained espalhado em muitos arquivos.
- `v0.2.1`: reorganização da distribuição para single-file, deixando `Navegador.exe` evidente na raiz do pacote.
