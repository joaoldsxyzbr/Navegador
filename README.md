# Navegador

Um navegador próprio para Windows, com visual do Chrome e motor WebView2 (Chromium/Edge).

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

- Windows 10/11;
- .NET 10 SDK;
- Microsoft Edge WebView2 Runtime.

O WebView2 Runtime é pré-instalado no Windows 11 e na maioria dos Windows 10 atualizados. O projeto mostra um erro com o endereço oficial se o Runtime não estiver disponível.

## Executar

No PowerShell, na raiz do repositório:

```powershell
dotnet run --project desktop/Navegador.Windows/Navegador.Windows.csproj
```

O perfil será criado na pasta `Data/WebView2`, ao lado do executável.

## Documentação

- [Arquitetura](docs/ARQUITETURA.md)
- [Requisitos](docs/REQUISITOS.md)
- [Plano](docs/PLANO.md)

## Histórico

A release `v0.1.0` é um bootstrap Chromium anterior, mantido no histórico e na página de Releases do GitHub. A nova base WebView2 é uma linha de desenvolvimento separada.
