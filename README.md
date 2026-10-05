# Navegador

Navegador simples e leve construído como uma distribuição própria do Chromium.

## Estratégia

O projeto segue um modelo de overlay inspirado em distribuições que mantêm o motor upstream quase intacto:

- Chromium upstream não é copiado para este repositório;
- a versão base fica fixada em `chromium/VERSION`;
- configurações de build ficam em `chromium/args/`;
- alterações próprias entram como patches pequenos e numerados em `chromium/patches/`;
- identidade visual e recursos próprios ficam em `chromium/branding/`;
- scripts em `chromium/scripts/` preparam e compilam uma árvore Chromium externa.

A fonte de verdade do Navegador é este repositório. O checkout completo do Chromium é apenas material de build e não deve ser commitado.

## Base atual

- Chromium: **154.0.8037.92**
- Plataforma inicial da nova arquitetura: **Windows x64**
- Android: fase seguinte, depois que o fluxo desktop estiver reproduzível

## Estrutura

```text
chromium/
  VERSION
  args/
  branding/
  patches/
  scripts/
docs/
  ARQUITETURA.md
  CHROMIUM.md
  PLANO.md
.github/
  workflows/
```

## Fluxo

```text
Chromium upstream
      +
args de build
      +
patches do Navegador
      +
branding
      ↓
Navegador
```

## Validação rápida

No PowerShell:

```powershell
./chromium/scripts/verify.ps1
```

Para preparar uma árvore externa do Chromium, instale `depot_tools` e siga `docs/CHROMIUM.md`.

## Princípios

1. Alterar o mínimo possível no Chromium.
2. Preferir configuração antes de patch.
3. Preferir patch isolado antes de alteração ampla.
4. Manter cada patch documentado e reaplicável.
5. Atualizações de segurança do upstream têm prioridade.
6. Não versionar o checkout nem artefatos de build do Chromium.
7. GitHub é a fonte de verdade do projeto.
