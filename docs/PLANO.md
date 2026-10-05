# Plano de implementação

## Objetivo

Transformar o Navegador em uma distribuição Chromium própria, leve de manter, com delta mínimo em relação ao upstream e **interface visual preservada do Chromium**.

## Fase 1 — Fundação do overlay

- [x] Escolher Chromium + overlay como arquitetura principal.
- [x] Remover a aplicação MAUI/WebView da `main`.
- [x] Fixar uma versão Chromium.
- [x] Criar estrutura de args, patches, branding e scripts.
- [x] Criar validação leve do overlay no CI.
- [x] Definir que a UI upstream do Chromium será preservada.
- [ ] Executar o primeiro checkout completo do Chromium em ambiente de build.
- [ ] Compilar o target `chrome` sem patches.
- [ ] Registrar tamanho, duração e requisitos reais do primeiro build.

## Fase 2 — Identidade mínima

- [ ] Ajustar nome do produto para Navegador.
- [ ] Integrar ícones do Navegador.
- [ ] Ajustar identificadores e diretórios de perfil.
- [ ] Garantir que o binário use identidade própria sem redesenhar a UI.
- [ ] Definir página inicial e mecanismo de pesquisa padrão.

## Fase 3 — Funções do Navegador

- [ ] Definir a primeira função adicional.
- [ ] Integrar novos recursos usando componentes nativos do Chromium.
- [ ] Evitar alterações na disposição visual de abas, omnibox, menus e configurações.
- [ ] Revisar recursos Chromium que ficam habilitados.
- [ ] Definir defaults de privacidade e telemetria.
- [ ] Definir política de atualizações.
- [ ] Validar abas, downloads, histórico, favoritos, perfis e modo privado.
- [ ] Criar empacotamento Windows x64.

## Fase 4 — Atualização upstream

- [ ] Automatizar teste de reaplicação dos patches.
- [ ] Criar rotina de atualização da versão Chromium.
- [ ] Adicionar relatório de patches quebrados.
- [ ] Definir cadência para atualizações de segurança.

## Fase 5 — Android

- [ ] Definir estratégia de build Chromium Android.
- [ ] Portar branding, defaults e funções compartilháveis.
- [ ] Preservar a interface Chromium/Android sempre que aplicável.
- [ ] Criar empacotamento APK/AAB.
- [ ] Validar recursos móveis e atualização.

## Critérios de aceite da fundação

- `main` não contém a antiga implementação MAUI/WebView.
- Chromium completo não está vendorizado no Git.
- uma versão upstream exata está fixada;
- patches possuem ordem explícita;
- scripts conseguem preparar e construir uma árvore externa;
- UI upstream é a referência visual do produto;
- CI detecta estrutura quebrada antes do build pesado.
