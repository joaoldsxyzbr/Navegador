# Requisitos do Navegador

## Primeira etapa: Windows

- App desktop próprio em C# / WinForms.
- WebView2 como motor pronto; sem compilar Chromium ou Firefox.
- Visual escuro inspirado no Chrome.
- Abas, voltar, avançar, recarregar e barra de endereço com busca.
- Perfil persistente local em `Data/WebView2`.
- Distribuição portátil como objetivo; explicar/instalar o WebView2 Runtime se estiver ausente.
- Preservar a release v0.1.0 sem tentar migrar automaticamente perfis Chromium.

## Ainda fora do protótipo

- Favoritos e histórico visíveis.
- Gerenciador de downloads.
- Janela privada e controles de privacidade.
- Página inicial própria e configurações.
- Botão de atualização integrado e publicação de uma nova release.
- Android/iOS e sincronização.

## Critérios do MVP Windows

1. A aplicação compila em runner Windows via .NET 10.
2. Abre páginas HTTPS e pesquisas digitadas na barra.
3. Permite criar, alternar e fechar abas.
4. Voltar, avançar e recarregar funcionam conforme o histórico da aba.
5. O perfil persiste na pasta local do Navegador.
6. Tema escuro aparece na moldura, abas e controles.
7. Ausência do WebView2 Runtime gera uma mensagem clara com link oficial.
