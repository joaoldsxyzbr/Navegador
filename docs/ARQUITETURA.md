# Arquitetura

## Decisão principal

O Navegador passa a ser uma distribuição própria baseada em **Chromium upstream + overlay versionado**.

A antiga aplicação .NET MAUI/WebView foi removida da `main`. O projeto não usa mais WebView2 como fundação do produto.

## Objetivo arquitetural

Ter um navegador Chromium próprio sem manter um fork profundo do motor.

```text
Chromium upstream limpo
        +
configuração
        +
patches pequenos
        +
branding
        ↓
Navegador
```

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
- evitar refatorações upstream desnecessárias;
- patch que não reaplica bloqueia a atualização até ser corrigido ou removido.

### chromium/branding

Fontes da identidade do Navegador. A integração desses recursos ao Chromium deve acontecer por patches pequenos.

### chromium/scripts

Automação de preparação, validação e build. Os scripts devem operar sobre um checkout externo e nunca depender de uma cópia vendorizada do Chromium no Git.

## Plataforma

### Windows

É a primeira plataforma da nova arquitetura. O objetivo inicial é produzir um executável Chromium funcional, depois substituir identidade, defaults e recursos gradualmente.

### Android

Continua no escopo do produto, mas só entra depois que a receita Chromium para Windows estiver reproduzível. Isso reduz duas frentes pesadas de build ao mesmo tempo.

## Estratégia de customização

Ordem de preferência:

1. argumento GN ou configuração suportada;
2. preferência/default do Chromium;
3. recurso/branding;
4. patch pequeno;
5. alteração maior somente quando houver benefício claro.

A regra é manter o delta para o upstream pequeno.

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
