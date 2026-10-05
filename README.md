# Navegador

Um navegador próprio para Windows, com visual inspirado no Chrome e motor WebView2 (Chromium/Edge).

## Direção

Vamos começar pelo PC. O app Windows usa C# e WinForms para a interface e WebView2 para carregar sites. O motor é fornecido pelo Microsoft Edge WebView2 Runtime; não compilamos Chromium nem Firefox.

A linha Android fica para uma fase posterior, depois de validarmos o navegador Windows.

## Protótipo atual

O primeiro protótipo inclui:

- abas;
- voltar, avançar e recarregar;
- barra de endereço com busca;
- tema escuro na interface;
- perfil persistente dentro de `Data/WebView2`.

Ainda faltam favoritos, histórico visível, gerenciador de downloads, janela privada, configurações e atualizador integrado.

## Requisitos para desenvolver

- Windows 10/11;
- .NET 10 SDK;
- Microsoft Edge WebView2 Runtime.

O WebView2 Runtime é pré-instalado no Windows 11 e na maioria dos Windows 10 atualizados. O projeto detecta falha ao iniciar o motor e mostra o endereço oficial para instalar o Runtime.

## Executar

No PowerShell, na raiz do repositório:

```powershell
dotnet run --project desktop/Navegador.Windows/Navegador.Windows.csproj
```

O perfil será criado na pasta `Data/WebView2`, ao lado do executável. Para desenvolvimento, a pasta equivalente fica no diretório de saída do build.

## Documentação

- [Arquitetura](docs/ARQUITETURA.md)
- [Requisitos](docs/REQUISITOS.md)
- [Plano](docs/PLANO.md)

## Histórico

A release `v0.1.0` é um bootstrap Chromium anterior, mantido no histórico e na página de Releases do GitHub. A nova base WebView2 é uma linha de desenvolvimento separada.
