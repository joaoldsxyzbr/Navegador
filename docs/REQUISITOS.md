# Requisitos do produto

Este documento registra os requisitos aprovados para a nova linha Firefox do Navegador.

## Base e aparência

- Base desktop: Firefox/Gecko.
- Base Android: GeckoView.
- A interface desktop deve manter a familiaridade visual do Chrome: abas na parte superior, barra de endereço/ações e menu em posição familiar.
- A aproximação será validada por protótipo; não exigir equivalência pixel a pixel antes de medir o custo e a compatibilidade.
- Tema escuro como padrão, com Claro/Escuro/Sistema quando suportado pela implementação.
- Preservar comportamento, acessibilidade e recursos nativos Firefox sempre que possível.

## Windows portátil

- Distribuição ZIP/pasta executável, sem instalador obrigatório para uso comum.
- Não exigir privilégios administrativos nem serviço residente para navegar.
- Perfil local em pasta controlada pelo Navegador, preferencialmente `Data/`.
- Atualizador via HTTPS, validação SHA-256, preservação de Data/ e rollback.
- Definir e testar o empacotamento Firefox antes de migrar launcher e atualizador.
- A versão v0.1.0 Chromium permanece numa linha histórica. Não substituir automaticamente um perfil Chromium por Firefox nem declarar que os dados serão migrados.

## Android

- O app será instalado pelo Android; “portátil” refere-se ao Windows.
- GeckoView será o engine, com UI própria do app.
- Validar abas, navegação, janela privada, downloads, tema escuro e atualização antes de considerar a plataforma pronta.

## Sincronização

Sincronização entre Windows e Android continua fora do escopo atual: sem conta própria, backend ou sincronização de favoritos, abas, histórico, configurações, senhas ou passkeys.

## Fases

1. protótipo Firefox desktop com aparência Chrome-like via Artifact Mode;
2. validar interface e custo de build;
3. adaptar pacote portátil e atualizador para Firefox, mantendo v0.1.0 intacta;
4. estabilizar defaults, modo escuro, branding, downloads e janela privada;
5. protótipo Android GeckoView com shell separado;
6. reavaliar sincronização somente como iniciativa futura.
