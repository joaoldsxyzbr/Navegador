# Firefox e interface do Navegador

## Base

- Desktop: Firefox upstream, engine Gecko.
- Android: GeckoView para engine, com interface própria do Navegador.
- Referência visual: Chrome desktop no Windows e, separadamente, Chrome no Android.
- A v0.1.0 continua como release histórica Chromium; não é substituída pelo Firefox no mesmo perfil.

## Por que Firefox

A documentação Mozilla para build no Windows informa 40 GB de espaço livre, 4 GB de RAM mínimos e 8 GB ou mais recomendados. Isso é menor que a exigência atual documentada pelo Chromium (100 GB livres), embora ainda exceda o armazenamento dos runners padrão do GitHub.

O Firefox usa tecnologias web para parte importante de sua interface desktop. O Artifact Mode permite buscar componentes nativos pré-compilados e testar mudanças em HTML/XHTML, CSS, JavaScript e strings sem compilar C++/Rust. O modo não permite modificar código C, C++ ou Rust nem atende a toda mudança de configuração do build.

Fontes oficiais:

- [Build Firefox no Windows](https://firefox-source-docs.mozilla.org/setup/windows_build.html)
- [Artifact Builds: suporte e limitações](https://firefox-source-docs.mozilla.org/contributing/build/artifact_builds.html)
- [Arquitetura do Firefox Desktop](https://firefox-source-docs.mozilla.org/browser/overview.html)
- [GeckoView](https://firefox-source-docs.mozilla.org/mobile/android/geckoview/index.html)

## Protótipo de interface desktop

O protótipo deve ser feito sobre o frontend Firefox e comparado com o Chrome nas seguintes áreas:

1. posição e forma da faixa de abas;
2. barra de endereço, voltar/avançar e recarregar;
3. toolbar e ações principais;
4. menu principal;
5. tema escuro;
6. navegação por teclado e acessibilidade;
7. comportamento após atualização upstream.

Evitar começar com mudanças no motor. Se uma mudança exigir C/C++/Rust, documentar o motivo e avaliar novamente o custo antes de entrar no escopo.

## Windows portátil

Antes de portar o workflow de release, identificar a forma de distribuir o build Firefox em modo portátil e apontar o perfil para `Data/`. Adaptar launcher e atualizador ao executável e layout Firefox somente depois de validar um pacote de teste. Proteger os dados já criados pela v0.1.0 Chromium; não reutilizar o mesmo canal de atualização entre motores.

## Android

GeckoView fornece o engine, mas não fornece o visual Chrome pronto. A barra de endereço, abas, menus, downloads e configurações do app serão UI Android separada. Avaliar Android Components da Mozilla e demonstrar navegação, abas, janela privada, downloads e modo escuro antes de estabilizar o MVP móvel.

## Recursos necessários

A máquina para Artifact Mode precisa cumprir os requisitos atuais do Mozilla Build no Windows, incluindo 40 GB livres. Os runners públicos padrão do GitHub documentam 14 GB de SSD, então não devem receber esse job. A documentação deve ser conferida de novo antes de configurar qualquer runner.

## Critério de aceite do protótipo

- build Artifact Firefox inicia no Windows;
- abas/toolbar/omnibox/menu lembram claramente a organização do Chrome;
- a interface permanece funcional e navegável por teclado;
- não houve recompilação local do engine;
- pacote de teste não altera nem migra o perfil Chromium da v0.1.0.
