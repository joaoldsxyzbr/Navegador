# Navegador

Navegador simples e leve construído como uma distribuição própria do Chromium.

## Estratégia

O projeto segue um modelo de overlay inspirado em distribuições que mantêm o motor upstream quase intacto:

- Chromium upstream não é copiado para este repositório;
- a versão base fica fixada em `chromium/VERSION`;
- configurações de build ficam em `chromium/args/`;
- alterações próprias entram como patches pequenos e numerados em `chromium/patches/`;
- branding mínimo e recursos próprios ficam em `chromium/branding/`;
- scripts em `chromium/scripts/` preparam e compilam uma árvore Chromium externa.

A fonte de verdade do Navegador é este repositório. O checkout completo do Chromium é apenas material de build e não deve ser commitado.

## Direção do produto

O Navegador **não pretende redesenhar a interface do Chromium**.

A interface upstream deve ser preservada sempre que possível:

- abas, barra de endereço, menus e páginas de configurações seguem o Chromium;
- novos recursos devem usar padrões e componentes nativos do Chromium;
- mudanças visuais só entram quando forem indispensáveis para uma função nova;
- nome, ícone e identificadores próprios são branding, não um fork visual da interface.

O diferencial do Navegador será principalmente **funcional**, por meio de novos recursos, configurações, integrações e defaults.

## Requisitos principais

- tema escuro nativo do Chromium por padrão, com opção Claro/Escuro/Sistema;
- distribuição Windows portátil, sem instalador e com perfil local junto do aplicativo;
- atualizador próprio com verificação, download, SHA-256, preservação de Data/ e rollback;
- interface visual próxima do Chromium upstream;
- Android continua no roadmap depois da base Windows estar reproduzível;
- sincronização entre dispositivos fica **fora do escopo atual**.

A primeira versão Chromium do projeto é a **v0.1.0**. Até existir um ambiente capaz de compilar o Chromium completo, o pacote testável usa um snapshot oficial fixado como bootstrap. Essa limitação está documentada em `bootstrap/README.md`.

Os detalhes ficam em `docs/REQUISITOS.md`.

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
  REQUISITOS.md
  ATUALIZACOES.md
.github/
  workflows/
```

## Fluxo

```text
Chromium upstream
      +
args de build
      +
patches funcionais do Navegador
      +
branding mínimo
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

1. Preservar a interface nativa do Chromium.
2. Alterar o mínimo possível no upstream.
3. Preferir configuração antes de patch.
4. Preferir patch funcional isolado antes de alteração ampla.
5. Manter cada patch documentado e reaplicável.
6. Atualizações de segurança do upstream têm prioridade.
7. Não versionar o checkout nem artefatos de build do Chromium.
8. GitHub é a fonte de verdade do projeto.
