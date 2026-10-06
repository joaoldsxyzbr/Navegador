# Requisitos do Navegador

## Primeira etapa: Windows

- App desktop próprio em C# / WinForms.
- WebView2 como motor pronto; sem compilar Chromium ou Firefox.
- Visual próximo do Chrome: abas, barra de endereço, toolbar, menus e controles em posições equivalentes.
- Identidade do app permanece Navegador.
- Tema escuro.
- Abas, voltar, avançar, recarregar e barra de endereço com busca.
- Perfil persistente local, com caminho único para todos os dados.
- Instância única.
- Distribuição portátil como objetivo; explicar/instalar WebView2 Runtime se estiver ausente.
- Preservar a release v0.1.0 sem tentar migrar automaticamente perfis.

## Dados do usuário

- **Sessão**: salvar abas, ordem e aba ativa ao fechar; restaurar na próxima abertura quando a opção estiver ligada.
- **Favoritos**: adicionar e remover pela barra, listar, renomear, buscar e abrir; barra visível abaixo da toolbar.
- **Histórico**: registrar visitas com título, endereço e data; buscar, remover item e limpar tudo.
- **Downloads**: pasta de destino configurável, opção de perguntar antes de salvar, progresso visível, cancelar, abrir arquivo e abrir pasta.
- **Configurações**: sessão, pasta de downloads, pergunta de destino e página inicial.
- Nenhum desses arquivos pode deixar o navegador sem abrir quando estiver corrompido.

## Extensões

- Habilitar a API de extensões do WebView2.
- Instalar extensão Chromium a partir de pasta local descompactada com `manifest.json`.
- Listar extensões instaladas e permitir ativar, desativar e remover.
- Informar na interface que a Chrome Web Store e as janelas de popup/ícones não são suportadas.

## Atualização

- Consultar a release mais recente no GitHub.
- Comparar a versão instalada com a disponível usando as três partes visíveis.
- Baixar apenas de HTTPS em `github.com`.
- Validar SHA-256 antes de instalar; sem hash confiável, não instalar.
- O processo que substitui arquivos não pode aceitar destino arbitrário nem pacote forjado.
- Preservar o perfil do usuário durante a atualização.
- Restaurar a versão anterior se a cópia de algum arquivo falhar.
- Reiniciar o navegador ao final.

## Publicação

- A versão do produto existe em um único lugar.
- Publicar somente a partir de uma etiqueta `vX.Y.Z` que combine com essa versão.
- O pacote precisa conter `Navegador.exe` na raiz, sem poluição de arquivos de runtime.

## Fora do protótipo

- Janela privada e controles de privacidade.
- Favicons, agrupamento e fixação de abas, reordenar arrastando, reabrir aba fechada.
- Sincronização entre máquinas.
- Android/iOS.

## Critérios do MVP Windows

1. A aplicação compila em runner Windows via .NET 10.
2. Abre páginas HTTPS e pesquisas digitadas na barra.
3. Permite criar, alternar e fechar abas.
4. Voltar, avançar e recarregar funcionam conforme histórico da aba.
5. Perfil persiste em pasta local do Navegador.
6. Tema escuro e composição visual são comparados ao Chrome.
7. Ausência do WebView2 Runtime gera mensagem clara com link oficial.
8. Uma extensão compatível pode ser instalada de pasta, ativada, desativada e removida.
9. Limitações da Web Store e de popups/ícones ficam visíveis ao usuário.
10. As abas voltam na abertura seguinte.
11. Favoritos, histórico e downloads sobrevivem ao fechamento e podem ser gerenciados.
12. Os testes do Core passam no CI e localmente sem acesso ao NuGet.
