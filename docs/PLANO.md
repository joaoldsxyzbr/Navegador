# Plano de implementação

## Objetivo

Transformar o Navegador em uma distribuição Chromium própria, leve de manter e com delta mínimo em relação ao upstream.

## Fase 1 — Fundação do overlay

- [x] Escolher Chromium + overlay como arquitetura principal.
- [x] Remover a aplicação MAUI/WebView da `main`.
- [x] Fixar uma versão Chromium.
- [x] Criar estrutura de args, patches, branding e scripts.
- [x] Criar validação leve do overlay no CI.
- [ ] Executar o primeiro checkout completo do Chromium em ambiente de build.
- [ ] Compilar o target `chrome` sem patches.
- [ ] Registrar tamanho, duração e requisitos reais do primeiro build.

## Fase 2 — Identidade do Navegador

- [ ] Criar patch de nome do produto.
- [ ] Integrar ícones do Navegador.
- [ ] Ajustar identificadores e diretórios de perfil.
- [ ] Definir página inicial e mecanismo de pesquisa padrão.
- [ ] Garantir que o binário não use identidade do Google Chrome.

## Fase 3 — Produto mínimo

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
- [ ] Portar branding e configurações compartilháveis.
- [ ] Criar empacotamento APK/AAB.
- [ ] Validar recursos móveis e atualização.

## Critérios de aceite da fundação

- `main` não contém a antiga implementação MAUI/WebView.
- Chromium completo não está vendorizado no Git.
- uma versão upstream exata está fixada;
- patches possuem ordem explícita;
- scripts conseguem preparar e construir uma árvore externa;
- CI detecta estrutura quebrada antes do build pesado.
