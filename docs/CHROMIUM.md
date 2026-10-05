# Histórico Chromium — v0.1.0

Este documento descreve a arquitetura anterior e permanece para reproduzir a release histórica v0.1.0. A nova linha de desenvolvimento migrou para Firefox/Gecko conforme [ARQUITETURA.md](ARQUITETURA.md).

## Estado histórico

- A v0.1.0 usa um snapshot oficial Chromium para Windows x64.
- O launcher, perfil portátil, atualizador separado e publicação foram validados; o usuário confirmou que o navegador inicia.
- O build completo de Chromium não foi realizado.
- A estrutura `chromium/` e workflows associados só devem ser removidos depois que o protótipo Firefox e um pacote Firefox tiverem sido validados.
- Não usar esses scripts como caminho de build da nova linha Firefox.

Veja [FIREFOX.md](FIREFOX.md) para arquitetura, Artifact Mode, interface Chrome-like e GeckoView.
