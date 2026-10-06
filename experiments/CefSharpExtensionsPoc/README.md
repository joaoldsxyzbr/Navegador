# Rumo · PoC CefSharp

Esta pasta contém um aplicativo WinForms descartável para testar CEF no Chrome Runtime sem alterar o executável principal nem compartilhar o perfil WebView2.

## O que testar

1. Baixe o artefato CefSharp-PoC-win-x64 na execução do GitHub Actions vinculada ao PR.
2. Extraia o ZIP e execute Rumo.CefSharpPoc.exe no Windows x64. A máquina precisa do Microsoft Visual C++ Redistributable 2022 x64.
3. A página inicial abre a Chrome Web Store. Escolha uma extensão Manifest V3 com popup, como Dark Reader, e inicie a instalação pela própria página.
4. Registre se a confirmação de instalação aparece dentro da janela da PoC ou se uma janela/sessão Chromium independente é aberta.
5. Use Extensões para abrir chrome://extensions. Confirme se a extensão aparece no mesmo perfil.
6. Em chrome://extensions, habilite o modo do desenvolvedor e copie o ID da extensão. Cole-o no campo “ID da extensão” da PoC e clique em “Abrir popup”.
7. Confirme se o conteúdo do popup abre em uma janela da própria PoC e se o código e as APIs usados por ele respondem. Confirme também o fluxo de permissões e reinicie a PoC para verificar se a extensão continua instalada.
8. Registre o resultado, a versão do Windows e o tamanho do ZIP publicado.

O botão “Abrir popup” lê o manifesto instalado e hospeda a página `default_popup` declarada por ele em uma janela CefSharp da aplicação, usando o perfil da PoC. Isso exercita a página da extensão dentro do app, mas não reproduz o ícone nativo de ação nem um clique na barra de extensões do Chrome.

O perfil isolado fica em %LOCALAPPDATA%\\Rumo\\CefSharpPoc. Apague somente essa pasta para repetir o teste com um perfil limpo. A PoC não acessa os dados do perfil real do Rumo.

## Build local

Com .NET 10 SDK em Windows:

    dotnet restore experiments/CefSharpExtensionsPoc/CefSharpExtensionsPoc.csproj -r win-x64
    dotnet publish experiments/CefSharpExtensionsPoc/CefSharpExtensionsPoc.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/CefSharpPoc/win-x64

O build usa CefSharp.WinForms.NETCore 152.0.100 e publica os arquivos nativos em uma pasta, para que o teste também mostre o custo de distribuição. Não é uma release do Rumo.

## Limite deste experimento

O CI confirma que a aplicação e o pacote Windows compilam. A instalação real pela Chrome Web Store, a execução dos popups e a persistência precisam ser observadas em uma sessão Windows com interface gráfica. Os resultados ainda não foram validados manualmente. A barra nativa de ações também fica fora do escopo deste lançador de popup.

## Registro do teste manual

Preencha a tabela após executar a PoC. Até haver um resultado reproduzível, mantenha cada item como não validado.

| Verificação | Resultado | Observações |
| --- | --- | --- |
| CWS instalou sem abrir uma janela Chromium independente | Pendente | |
| Extensão aparece em chrome://extensions | Pendente | |
| Popup declarado no manifesto abre pela PoC e seu conteúdo responde | Pendente | |
| Ícone nativo da ação abre o popup na barra da PoC | Fora do escopo do lançador | |
| Permissões foram apresentadas antes de habilitar | Pendente | |
| Extensão persiste depois de fechar e reabrir a PoC | Pendente | |
| ZIP e dependências cabem no formato de distribuição desejado | Pendente | |
