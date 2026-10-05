# Arquitetura

## Decisão principal

O Navegador é uma distribuição própria baseada em **Chromium upstream + overlay versionado**.

A antiga aplicação .NET MAUI/WebView foi removida da `main`. O projeto não usa mais WebView2 como fundação do produto.

## Objetivo arquitetural

Ter um navegador Chromium próprio sem manter um fork profundo do motor e **sem criar uma interface paralela ao Chromium**.

```text
Chromium upstream limpo
        +
configuração
        +
patches funcionais pequenos
        +
branding mínimo
        ↓
Navegador
```

## Regra de interface

A UI do Chromium é a base visual oficial do Navegador.

Por padrão, não serão redesenhados:

- barra de abas;
- omnibox/barra de endereço;
- barra de ferramentas;
- menus;
- página de downloads;
- histórico;
- favoritos;
- configurações;
- DevTools;
- demais superfícies já fornecidas pelo Chromium.

Novas funções devem, sempre que possível, ser integradas usando componentes, comandos, menus, páginas e padrões já existentes no Chromium.

Uma alteração visual só é aceitável quando:

1. for necessária para expor uma função nova;
2. reutilizar o design system e componentes do Chromium;
3. permanecer pequena e isolada;
4. não exigir manter um fork visual independente.

Trocar nome, ícone, identificadores do produto e diretórios próprios é considerado **branding mínimo**, não redesign.

## Fonte de verdade

O repositório `joaoldsxyzbr/Navegador` contém somente o que diferencia o Navegador do Chromium e tudo que torna o build reproduzível.

O código completo do Chromium é obtido externamente durante a preparação do ambiente e nunca deve ser commitado aqui.

## Componentes

### chromium/VERSION

Fixa exatamente a versão upstream usada pelo projeto. Atualizar Chromium é uma mudança explícita e revisável.

### chromium/args

Argumentos GN próprios por plataforma e perfil de build.

### chromium/patches

Patches ordenados e reaplicáveis. `series` define a ordem oficial.

Regras:

- um objetivo por patch;
- nomes numerados;
- priorizar recursos e comportamento, não customização visual;
- evitar refatorações upstream desnecessárias;
- patch que não reaplica bloqueia a atualização até ser corrigido ou removido.

### chromium/branding

Somente identidade essencial do Navegador, como nome, ícones e identificadores. Não é uma camada de interface própria.

### chromium/scripts

Automação de preparação, validação e build. Os scripts devem operar sobre um checkout externo e nunca depender de uma cópia vendorizada do Chromium no Git.

## Plataforma

### Windows

É a primeira plataforma da nova arquitetura. O objetivo inicial é produzir um executável Chromium funcional e depois adicionar branding mínimo, defaults e recursos próprios sem alterar a experiência visual base.

### Android

Continua no escopo do produto, mas só entra depois que a receita Chromium para Windows estiver reproduzível. Isso reduz duas frentes pesadas de build ao mesmo tempo.

## Estratégia de customização

Ordem de preferência:

1. recurso já existente no Chromium;
2. preferência/default suportado;
3. argumento GN ou configuração;
4. extensão funcional pequena usando componentes nativos;
5. patch funcional isolado;
6. alteração maior somente quando houver benefício claro.

A regra é manter o delta para o upstream pequeno e evitar um fork de UI.

## Atualização do Chromium

Uma atualização segue este ciclo:

1. escolher uma versão upstream;
2. atualizar `chromium/VERSION`;
3. preparar uma árvore limpa;
4. aplicar `chromium/patches/series`;
5. corrigir somente patches incompatíveis;
6. gerar o build;
7. executar testes;
8. registrar incompatibilidades e decisões.

## CI

O CI comum valida o overlay, a versão, a lista de patches e a ausência do antigo projeto MAUI.

O build integral do Chromium não roda no CI leve porque checkout e compilação são caros em disco e tempo. Ele terá workflow dedicado quando a infraestrutura de build estiver definida.

## Segurança

- Chromium deve permanecer próximo da versão estável suportada.
- Nenhuma chave ou token deve entrar em patches, scripts ou argumentos.
- Patches que alterem sandbox, isolamento de processos, TLS, permissões ou segurança exigem revisão específica.
- Recursos Google não devem ser habilitados por segredo embutido no binário.
