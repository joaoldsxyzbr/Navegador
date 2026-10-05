# Plano de implementação

## Estado atual — 05/10/2026

- A release **v0.1.0** está publicada e o usuário confirmou que o navegador inicia no Windows.
- O CI de overlay e o workflow da release passaram no commit `51e89c9524634497e3b6150d4d6c5199a02a3432`.
- A v0.1.0 ainda empacota um snapshot oficial do Chromium. Ela valida o launcher, o perfil portátil, o atualizador separado e a publicação; **não** valida o build completo do overlay.
- O build integral está bloqueado porque não há um ambiente de build Windows disponível. Até existir um ambiente autorizado e compatível, o trabalho continua sobre o bootstrap v0.1.0 e tarefas que não exigem compilar o Chromium inteiro.

## Fluxo viável enquanto não há runner de build

1. Fixar uma revisão oficial do Chromium em `bootstrap/SNAPSHOT_REVISION`.
2. No GitHub Actions, baixar esse snapshot, compilar o launcher e o atualizador, montar o pacote portátil e executar smoke test e validações do pacote.
3. Publicar a release somente se todos os passos passarem; gerar o manifesto de atualização e o SHA-256 no mesmo fluxo.
4. Fazer melhorias que cabem no overlay leve (launcher, atualizador, empacotamento, testes e documentação). Mudanças internas de UI, branding ou comportamento do Chromium aguardam o build próprio.
5. Atualizar intencionalmente a revisão upstream e a versão Navegador; não acompanhar snapshots automaticamente sem validação.

Esse é o fluxo que conseguimos sustentar agora e mantém um download oficial do Chromium verificável. Ele não transforma o snapshot em um build próprio nem permite alterar o executável interno do Chromium.

## Quando houver ambiente de build

Retomar o modelo completo upstream + overlay: obter a fonte Chromium, aplicar patches/configurações versionados, compilar e empacotar. Fazer isso apenas em um Windows autorizado com espaço livre compatível ou runner maior já disponível e aprovado. O workflow pesado deve ser separado do CI leve e só publicar depois dos mesmos testes de pacote e atualização.

## Objetivo

Transformar o Navegador em uma distribuição Chromium própria, leve de manter, com delta mínimo em relação ao upstream e **interface visual preservada do Chromium**.

## Fase 1 — Fundação do overlay

- [x] Escolher Chromium + overlay como arquitetura principal.
- [x] Remover a aplicação MAUI/WebView da `main`.
- [x] Fixar uma versão Chromium.
- [x] Criar estrutura de args, patches, branding e scripts.
- [x] Criar validação leve do overlay no CI.
- [x] Validar compilação do launcher e do atualizador no CI.
- [x] Definir que a UI upstream do Chromium será preservada.
- [x] Adiar sincronização entre dispositivos para fora do escopo atual.
- [ ] Identificar e preparar ambiente Windows autorizado para o build integral (acompanhar em [#8](https://github.com/joaoldsxyzbr/Navegador/issues/8)).
- [ ] Executar o primeiro checkout completo do Chromium nesse ambiente.
- [ ] Compilar o target `chrome` sem patches.
- [ ] Registrar espaço, RAM, duração e eventuais limites observados.

## Fase 2 — Identidade mínima

- [ ] Ajustar nome do produto para Navegador no binário próprio.
- [ ] Integrar ícones do Navegador.
- [ ] Ajustar identificadores e diretórios de perfil.
- [ ] Garantir identidade própria sem redesenhar a UI.
- [ ] Definir página inicial e mecanismo de pesquisa padrão.

## Fase 3 — Base do produto

- [ ] Tornar o tema escuro o padrão, preservando Claro/Escuro/Sistema.
- [x] Criar launcher/empacotamento portátil Windows.
- [x] Manter o perfil em `Data/` junto do pacote portátil.
- [ ] Validar execução sem instalador e sem privilégios administrativos.
- [ ] Definir a primeira função adicional própria do Navegador.
- [ ] Integrar novos recursos usando componentes nativos do Chromium.
- [ ] Evitar alterações na disposição visual de abas, omnibox, menus e configurações.
- [ ] Revisar recursos Chromium que ficam habilitados.
- [ ] Definir defaults de privacidade e telemetria.
- [x] Definir política de atualizações.
- [x] Implementar atualizador Windows separado, com manifesto, SHA-256, preservação de `Data/` e rollback; falta validação ponta a ponta de atualização.
- [ ] Integrar o atualizador à página nativa Sobre o Navegador quando o Chromium próprio estiver compilando.
- [ ] Validar downloads e navegação privada com os recursos nativos do Chromium.
- [x] Publicar empacotamento Windows x64 bootstrap v0.1.0.
- [ ] Substituir o snapshot bootstrap pelo build próprio do overlay.

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

## Fora do escopo atual

- sincronização Windows ↔ Android;
- backend de conta/sync;
- sincronização de favoritos, abas, histórico, configurações, senhas ou passkeys entre dispositivos.

## Critérios de aceite da fundação

- `main` não contém a antiga implementação MAUI/WebView.
- Chromium completo não está vendorizado no Git.
- uma versão upstream exata está fixada;
- patches possuem ordem explícita;
- scripts conseguem preparar e construir uma árvore externa;
- UI upstream é a referência visual do produto;
- CI detecta estrutura quebrada antes do build pesado.
