# Plano de implementação

## Estado — 05/10/2026

- A release v0.1.0 está publicada e inicia no Windows; ela usa Chromium e continua sendo a versão histórica/bootstrap.
- Decisão atual: migrar a nova linha do Navegador para Firefox/Gecko, mantendo o Chrome como referência visual da interface.
- O objetivo inicial é personalizar a interface web do Firefox usando Artifact Mode, que baixa componentes nativos prontos e evita recompilar o motor para mudanças de frontend.
- Build Firefox no Windows pede 40 GB livres; runners padrão do GitHub têm 14 GB, então o protótipo precisa de máquina/runner compatível e autorizado.
- Android usará GeckoView, mas terá interface separada; o Chrome visual precisa ser recriado no app Android.

## Fase 1 — Provar o caminho Firefox desktop

- [x] Decidir Firefox/Gecko como nova base.
- [x] Definir Chrome como referência visual desktop.
- [x] Escolher Artifact Mode como primeira opção para mudanças de frontend.
- [ ] Preparar o ambiente compatível e obter fonte/artifacts Firefox.
- [ ] Criar protótipo de abas, omnibox, toolbar, menu e tema escuro.
- [ ] Confirmar que o protótipo compila em Artifact Mode sem build do motor.
- [ ] Validar o resultado visual e funções nativas.
- [ ] Registrar espaço, RAM, duração e limites reais.

## Fase 2 — Distribuição Windows

- [ ] Escolher/validar empacotamento portátil Firefox.
- [ ] Adaptar launcher, perfil `Data/` e atualizador ao executável/layout Firefox.
- [ ] Manter o pacote e perfil Chromium v0.1.0 separados; não atualizar cruzado.
- [ ] Testar atualização, hash inválido, rollback e preservação dos dados.
- [ ] Publicar uma primeira release Firefox somente após validar o pacote no Windows.

## Fase 3 — Recursos desktop

- [ ] Deixar modo escuro como padrão e manter Claro/Escuro/Sistema se viável.
- [ ] Aplicar identidade Navegador sem usar marcas/recursos do Firefox indevidamente.
- [ ] Validar downloads, janela privada, atalhos e recursos nativos.
- [ ] Integrar o atualizador à UI após provar a integração segura com frontend Firefox.

## Fase 4 — Android

- [ ] Avaliar GeckoView e Android Components.
- [ ] Criar shell Chrome-like independente para Android.
- [ ] Validar navegação, abas, privado, downloads e atualização.
- [ ] Criar APK/AAB quando o protótipo estiver funcional.

## Fora do escopo atual

- sync Windows ↔ Android;
- backend/conta própria;
- compilar mudanças em C/C++/Rust no motor Gecko sem necessidade comprovada.

## Histórico

- O overlay Chromium e a release v0.1.0 ficam registrados em [CHROMIUM.md](CHROMIUM.md).
- A issue antiga de infraestrutura Chromium foi encerrada como não planejada para a nova direção; o protótipo Firefox é acompanhado na [issue #9](https://github.com/joaoldsxyzbr/Navegador/issues/9).
