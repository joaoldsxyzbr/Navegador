# Rumo · PoC CefSharp

Esta pasta contém um aplicativo WinForms descartável para testar CEF no Chrome Runtime sem alterar o executável principal nem compartilhar o perfil WebView2.

## O que testar

1. Baixe o artefato CefSharp-PoC-win-x64 na execução do GitHub Actions vinculada ao PR.
2. Extraia o ZIP e execute Rumo.CefSharpPoc.exe no Windows x64. A máquina precisa do Microsoft Visual C++ Redistributable 2022 x64.
3. A página inicial abre a Chrome Web Store. Escolha uma extensão Manifest V3 com popup, como Dark Reader, e inicie a instalação pela própria página.
4. Registre se a confirmação de instalação aparece dentro da janela da PoC ou se uma janela/sessão Chromium independente é aberta.
5. Use Extensões para abrir chrome://extensions. Confirme se a extensão aparece no mesmo perfil.
6. Abra o popup/ação da extensão, confirme o fluxo de permissões e reinicie a PoC. Verifique se a extensão continua instalada.
7. Registre o resultado, a versão do Windows e o tamanho do ZIP publicado.

O perfil isolado fica em %LOCALAPPDATA%\\Rumo\\CefSharpPoc. Apague somente essa pasta para repetir o teste com um perfil limpo. A PoC não acessa os dados do perfil real do Rumo.

## Build local

Com .NET 10 SDK em Windows:

    dotnet restore experiments/CefSharpExtensionsPoc/CefSharpExtensionsPoc.csproj -r win-x64
    dotnet publish experiments/CefSharpExtensionsPoc/CefSharpExtensionsPoc.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/CefSharpPoc/win-x64

O build usa CefSharp.WinForms.NETCore 152.0.100 e publica os arquivos nativos em uma pasta, para que o teste também mostre o custo de distribuição. Não é uma release do Rumo.

## Limite deste experimento

O CI confirma que a aplicação e o pacote Windows compilam. A instalação real pela Chrome Web Store, os popups e a persistência precisam ser observados em uma sessão Windows com interface gráfica. Os resultados ainda não foram validados manualmente.
