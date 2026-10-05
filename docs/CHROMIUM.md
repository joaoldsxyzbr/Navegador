# Ambiente Chromium

## Visão geral

O repositório guarda o overlay do Navegador. O checkout completo do Chromium fica em um diretório externo e nunca deve ser commitado.

A versão do overlay está em `chromium/VERSION`. A v0.1.0 publicada usa um snapshot oficial de bootstrap; ela não substitui o build próprio descrito neste documento.

## Requisitos para o primeiro build Windows

A documentação oficial do Chromium para Windows, consultada em 05/10/2026, indica:

- Windows 10 ou superior, máquina x64 e volume NTFS;
- pelo menos **100 GB livres**;
- 8 GB de RAM como mínimo e mais de 16 GB recomendado;
- Visual Studio 2026 com os componentes Desktop development with C++ e MFC/ATL;
- Windows 11 SDK e `depot_tools`.

Para desempenho, a documentação recomenda SSD rápido, muitos núcleos (20 ou mais não é excessivo) e bastante memória (64 GB não é excessivo). O primeiro build deve registrar os recursos e o tempo observados, sem assumir que o perfil recomendado seja obrigatório.

Referência oficial: [Checking out and Building Chromium for Windows](https://chromium.googlesource.com/chromium/src/+/HEAD/docs/windows_build_instructions.md).

## Escolha do ambiente

O CI comum usa runners padrão e valida apenas o overlay e as ferramentas pequenas; ele não faz checkout nem build completo.

Opções a avaliar antes de iniciar o primeiro build:

1. **Runner próprio Windows**, somente em equipamento autorizado, com os requisitos acima. Se for equipamento gerenciado pelo trabalho, a instalação do runner precisa estar permitida pela organização responsável.
2. **GitHub-hosted larger runner** Windows, se disponível para a conta e repositório. A documentação atual limita esses runners a organizações/empresas no GitHub Team ou Enterprise Cloud; eles são cobrados por minuto. Confirmar plano, runner disponível, espaço livre real e custo antes de alterar o workflow.

A menor configuração Windows publicada pelo GitHub para larger runners informa 150 GB de armazenamento. Isso não comprova que haverá 100 GB livres após a imagem do sistema; verificar o espaço disponível no início do job. Não configurar um runner faturável sem aprovação explícita do custo.

A escolha e a preparação do ambiente estão acompanhadas na [issue #8](https://github.com/joaoldsxyzbr/Navegador/issues/3).

## Pré-requisitos no Windows

Use a documentação oficial do Chromium como referência para Visual Studio, Windows SDK, Git e `depot_tools`. Mantenha `depot_tools` no início do `PATH`, conforme as instruções atuais do Chromium. O caminho do checkout deve ser curto e sem espaços; mantenha a árvore em disco rápido e evite que a máquina durma durante checkout/build.

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
