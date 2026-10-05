# Navegador

Navegador é um projeto de navegador baseado no Firefox, com a interface de desktop adaptada para manter a familiaridade visual do Chrome.

## Direção atual

- Motor: Gecko, da família Firefox.
- Desktop: personalizar a interface upstream do Firefox com HTML, CSS e JavaScript para aproximá-la da organização visual do Chrome.
- Android: usar GeckoView como motor; a interface do app será um shell próprio e separado da interface desktop.
- Distribuição Windows: portátil, com perfil local e atualizador próprio.
- Sincronização Windows ↔ Android: fora do escopo atual.

A interface Chrome-like é um requisito visual. O primeiro passo é criar um protótipo e validar abas, barra de endereço, toolbar e menus antes de assumir que a aproximação está pronta. Não pretendemos alterar o motor Gecko para reproduzir o visual.

## Build mais leve

A documentação oficial do Firefox informa 40 GB livres e 4 GB de RAM como mínimo para build no Windows, com 8 GB ou mais recomendado. O Artifact Mode baixa componentes nativos já compilados e permite trabalhar na interface web do Firefox sem recompilar C++/Rust; mudanças no motor continuam fora desse modo.

- [Build do Firefox no Windows](https://firefox-source-docs.mozilla.org/setup/windows_build.html)
- [Firefox Artifact Builds e limitações](https://firefox-source-docs.mozilla.org/contributing/build/artifact_builds.html)
- [Arquitetura da interface Firefox](https://firefox-source-docs.mozilla.org/browser/overview.html)
- [GeckoView para Android](https://firefox-source-docs.mozilla.org/mobile/android/geckoview/index.html)

O runner padrão do GitHub não tem espaço suficiente para essa preparação. A etapa de protótipo deve rodar em máquina Windows que atenda ao espaço livre ou em runner maior disponível e autorizado.

## Versão histórica

A release **v0.1.0** foi publicada com um snapshot Chromium e continuará sendo identificada como bootstrap histórico. Ela não é baseada em Firefox. A migração para Gecko terá pacote/canal próprio; o atualizador não deve substituir um perfil Chromium por Firefox nem prometer migração de dados entre motores.

## Plano

Veja [docs/PLANO.md](docs/PLANO.md), [docs/ARQUITETURA.md](docs/ARQUITETURA.md) e [docs/FIREFOX.md](docs/FIREFOX.md). O antigo build Chromium está documentado em [docs/CHROMIUM.md](docs/CHROMIUM.md) como histórico da v0.1.0.
