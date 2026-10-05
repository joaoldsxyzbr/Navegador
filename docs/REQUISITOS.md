# Requisitos do Navegador

## Primeira etapa: Windows

- App desktop próprio em C# / WinForms.
- WebView2 como motor pronto; sem compilar Chromium ou Firefox.
- Visual próximo do Chrome: abas, barra de endereço, toolbar, menus e controles em posições equivalentes.
- Identidade do app permanece Navegador.
- Tema escuro.
- Abas, voltar, avançar, recarregar e barra de endereço com busca.
- Perfil persistente local em `Data/WebView2`.
- Distribuição portátil como objetivo; explicar/instalar WebView2 Runtime se estiver ausente.
- Preservar a release v0.1.0 sem tentar migrar automaticamente perfis.

## Extensões

- Habilitar a API de extensões do WebView2.
- Instalar extensão Chromium a partir de pasta local descompactada com `manifest.json`.
- Listar extensões instaladas e permitir ativar, desativar e remover.
- Verificar a compatibilidade real com extensões Chrome comuns.
- Loja Chrome Web Store, atualização automática, ícones e popups precisam de protótipo específico; não presumir suporte completo do WebView2.

## Fora do protótipo

- Favoritos e histórico visíveis.
- Gerenciador de downloads.
- Janela privada e controles de privacidade.
- Página inicial própria e configurações.
- Botão de atualização integrado e publicação de uma nova release.
- Android/iOS e sincronização.

## Critérios do MVP Windows

1. Aplicação compila em runner Windows via .NET 10.
2. Abre páginas HTTPS e pesquisas digitadas na barra.
3. Permite criar, alternar e fechar abas.
4. Voltar, avançar e recarregar funcionam conforme histórico da aba.
5. Perfil persiste em pasta local do Navegador.
6. Tema escuro e composição visual são comparados ao Chrome.
7. Ausência do WebView2 Runtime gera mensagem clara com link oficial.
8. Uma extensão compatível pode ser instalada de pasta, ativada, desativada e removida.
9. Limitações da Web Store e de popups/ícones ficam visíveis ao usuário.
