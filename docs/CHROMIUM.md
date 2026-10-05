# Ambiente Chromium

## Visão geral

O repositório guarda o overlay do Navegador. O checkout completo do Chromium fica em um diretório externo.

A versão atualmente fixada está em `chromium/VERSION`.

## Pré-requisitos no Windows

Use a documentação oficial do Chromium como referência para Visual Studio, Windows SDK e `depot_tools`.

O `depot_tools` deve estar no início do `PATH`.

## Preparar a árvore

No PowerShell, a partir do repositório do Navegador:

```powershell
./chromium/scripts/prepare-windows.ps1 -Workspace C:\src\navegador-chromium
```

O script:

1. cria/usa o workspace externo;
2. executa `fetch chromium --no-history` quando necessário;
3. posiciona `src` na versão de `chromium/VERSION`;
4. sincroniza dependências com `gclient sync`;
5. aplica os patches listados em `chromium/patches/series`.

O script recusa uma árvore upstream com alterações locais antes de trocar a versão.

## Compilar

Depois da preparação:

```powershell
./chromium/scripts/build-windows.ps1 -Workspace C:\src\navegador-chromium
```

O build usa `chromium/args/windows-release.gn`, gera `out/Navegador` e compila o target `chrome`.

No primeiro estágio o binário ainda é uma base Chromium. Branding e diferenças funcionais entram progressivamente por patches.

## Patches

Adicione o arquivo em `chromium/patches/` e inclua o nome em `chromium/patches/series`.

Exemplo:

```text
0001-branding-name.patch
0002-default-search.patch
```

A ordem em `series` é a ordem de aplicação.

## Atualizar upstream

Troque somente `chromium/VERSION` primeiro e rode a preparação novamente em uma árvore limpa. Se um patch falhar, corrija aquele patch antes de avançar.

Nunca resolva conflito editando silenciosamente a árvore externa e deixando o repositório desatualizado: toda diferença permanente precisa voltar para o overlay no GitHub.
