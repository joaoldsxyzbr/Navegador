# Arquitetura

## Decisão

A nova base do Navegador será **Firefox upstream (Gecko) + personalização da interface**. O usuário decidiu em 05/10/2026 trocar Chromium por Firefox e manter como referência visual a organização do Chrome.

A release v0.1.0 continua sendo o bootstrap Chromium já publicado. A troca de motor exige tratar essa versão como uma linha histórica separada.

## Objetivo

Ter uma distribuição Firefox própria com interface desktop familiar ao Chrome, tema escuro, pacote portátil Windows e atualizador. Não é necessário alterar o motor Gecko para trabalhar no visual e em recursos de interface.

## Desktop

A interface do Firefox usa HTML, CSS e JavaScript, então a adaptação visual pode começar na camada de frontend. O primeiro protótipo deve cobrir:

- faixa de abas horizontal no topo;
- barra de endereço e navegação;
- botões/toolbar e menu principal;
- tema escuro;
- páginas nativas mais usadas, conforme a viabilidade.

O objetivo é preservar o comportamento e a acessibilidade do Firefox enquanto aproximamos sua composição visual do Chrome. “Manter o visual” é critério de comparação do protótipo; não presumimos equivalência pixel a pixel antes de validar.

O Artifact Mode do Firefox baixa componentes C++ pré-compilados e suporta mudanças de frontend como JavaScript, XHTML/HTML, CSS e strings/FTL. Ele não suporta modificar código C, C++ ou Rust. Assim, é um caminho para prototipar a interface sem recompilar o motor; não é uma forma de alterar o engine. Veja [Firefox Artifact Builds](https://firefox-source-docs.mozilla.org/contributing/build/artifact_builds.html).

## Android

Android será baseado em GeckoView, a biblioteca de engine da Mozilla indicada para apps e navegadores. A UI do Firefox desktop não é reaproveitada: o Navegador Android terá shell nativo separado, usando a linguagem visual Chrome Android como referência. O início dessa plataforma depende do protótipo desktop e da estratégia de distribuição GeckoView.

## Distribuição e perfil

- Windows deve continuar portátil, sem exigir instalação para o uso normal.
- O perfil deve ficar em diretório controlado pelo Navegador, como `Data/`.
- O atualizador deve verificar HTTPS e SHA-256, preservar os dados e permitir rollback.
- O pacote Firefox deverá ser definido e validado antes de adaptar launcher/atualizador; não assumir que o layout do Chromium (`App/chrome.exe`) continuará válido.
- Perfis Chromium e Firefox não são intercambiáveis. A migração entre motores não deve substituir a v0.1.0 nem apagar seus dados; publicar uma linha Firefox separada no primeiro ciclo.

## Compilação

O modo Artifact reduz a compilação local quando as mudanças ficam no frontend, baixando os componentes nativos prontos. A documentação Firefox para Windows indica 40 GB de espaço livre, 4 GB de RAM mínimos e 8 GB ou mais recomendados. Runner padrão do GitHub com 14 GB de SSD é insuficiente; usar máquina/runner compatível e autorizado para a preparação do checkout.

Build completo do Gecko só será necessário se uma função exigir mudança de código nativo do motor. O escopo inicial do Navegador não exige esse tipo de mudança.

## Histórico Chromium

O código e o pacote Chromium da v0.1.0 ficam preservados enquanto a migração Firefox é prototipada. Os detalhes anteriores estão em [CHROMIUM.md](CHROMIUM.md); não tratar seus scripts como arquitetura atual.
