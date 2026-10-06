# Referências open source e processo de aprendizado

O Rumo deve aprender com navegadores e projetos open source maduros antes de tomar decisões importantes de arquitetura, experiência, segurança e manutenção. A intenção não é copiar produtos inteiros, e sim aproveitar soluções já testadas, entender seus compromissos e adaptar apenas o que fizer sentido ao nosso escopo: Windows, C# / WinForms / .NET 10 e WebView2.

## Princípios

1. **Começar pelo problema.** A referência é escolhida depois de definir o problema do Rumo, não o contrário.
2. **Separar motor e produto.** Limitações do WebView2 são tratadas como restrições do motor; decisões de shell, UX e persistência continuam sob nosso controle.
3. **Comparar mais de uma abordagem.** Para decisões relevantes, consultar pelo menos duas referências quando houver alternativas úteis.
4. **Preferir fontes primárias.** Código, documentação, issues, ADRs e materiais oficiais do próprio projeto valem mais que reproduções de terceiros.
5. **Adaptar, não clonar.** Uma solução só entra no Rumo se reduzir um problema real sem criar custo desproporcional de manutenção.
6. **Preservar portabilidade.** Toda mudança deve considerar o ZIP portátil, o instalador por usuário e o diretório de dados único.
7. **Checar licença antes de reutilizar código.** Ideias e padrões podem inspirar a solução; trechos de código só podem ser incorporados quando a licença e as obrigações forem compatíveis e registradas.
8. **Registrar decisões duráveis.** Quando uma pesquisa mudar arquitetura, comportamento ou manutenção, a conclusão deve aparecer na documentação relevante do repositório.

## Projetos de referência

| Projeto | O que observar | Uso principal no Rumo |
| --- | --- | --- |
| [Chromium](https://chromium.googlesource.com/chromium/src/) | abas, omnibox, perfis, permissões, downloads, DevTools, arquitetura de extensões e isolamento | referência de comportamento familiar e de padrões Chromium |
| [Brave](https://github.com/brave/brave-browser) | como adicionar produto e recursos próprios sobre Chromium sem perder compatibilidade | shell, configurações, extensões e recursos próprios |
| [Microsoft Edge WebView2 Samples](https://github.com/MicrosoftEdge/WebView2Samples) | APIs realmente expostas pelo WebView2, perfis, downloads, permissões, janelas e integração nativa | fonte primária para saber o que é tecnicamente possível no motor atual |
| [Firefox / Gecko](https://github.com/mozilla/gecko-dev) | sessão, perfis, privacidade, multiprocessamento, recuperação e organização do chrome do navegador | alternativas de arquitetura e UX fora do ecossistema Chromium |
| [LibreWolf](https://codeberg.org/librewolf/source) | manutenção de um downstream, defaults próprios e atualização sem reescrever o upstream | disciplina de manutenção e separação entre base e personalização |
| [ungoogled-chromium](https://github.com/ungoogled-software/ungoogled-chromium) | séries de patches, automação e controle de divergência em relação ao upstream | aprender a reduzir custo de manutenção de alterações próprias |
| [Ladybird](https://github.com/LadybirdBrowser/ladybird) | isolamento de processos, sandbox, fronteiras entre componentes, testes e evolução de arquitetura | referência de engenharia limpa e segurança, mesmo usando um motor diferente |
| [Zen Browser](https://github.com/zen-browser/desktop) | organização de abas, barra lateral, densidade visual e personalização | inspiração de UX moderna sem abandonar padrões familiares |
| [Floorp](https://github.com/Floorp-Projects/Floorp) | personalização sobre Firefox e organização de recursos adicionais | comparação com outro navegador derivado e altamente personalizável |

A lista não é fechada. Novos projetos podem ser incluídos quando resolverem um problema concreto melhor que as referências atuais.

## Processo para decisões importantes

Antes de uma mudança arquitetural ou funcional significativa:

1. **Definir o problema no Rumo.** Exemplo: “extensões carregam, mas popups não podem ser operados”.
2. **Inspecionar o estado atual.** Confirmar comportamento do código e, quando envolver o motor, a documentação/API atual do WebView2.
3. **Escolher referências adequadas.** Normalmente uma referência do ecossistema Chromium/WebView2 e uma segunda abordagem independente.
4. **Extrair princípios, não aparência.** Identificar fluxo, estados, limites, segurança, manutenção e comportamento de erro.
5. **Comparar com nossas restrições.** Windows, WebView2, modo portátil, tamanho do projeto, acessibilidade e custo de atualização.
6. **Escolher a menor solução suficiente.** Evitar criar infraestrutura que o WebView2 ou o Windows já fornecem.
7. **Implementar e validar.** Testes automatizados quando aplicáveis e validação visual/funcional real no Windows para mudanças de UI.
8. **Atualizar a documentação.** Registrar a decisão em Arquitetura, Requisitos, Plano ou neste documento quando ela for durável.

## Perguntas obrigatórias antes de adotar uma ideia

- Qual problema real do Rumo isso resolve?
- O comportamento pertence ao motor ou ao nosso shell?
- O WebView2 permite implementar isso de forma suportada?
- Como Chrome/Brave resolvem e como Firefox ou outro projeto resolve?
- Qual é o custo de manter a solução nas próximas versões?
- O modo portátil continua correto?
- A mudança afeta segurança, privacidade, permissões ou dados?
- A experiência continua acessível e familiar no Windows?
- Existe alternativa menor com praticamente o mesmo benefício?
- Há alguma obrigação de licença ou atribuição?

## Prioridades atuais de estudo

### Extensões

**Referências:** Chromium, Brave, Edge/WebView2 e ungoogled-chromium.

Investigar separadamente instalação, descoberta, ativação/desativação, permissões, toolbar, popups e atualização. Não prometer compatibilidade com Chrome Web Store ou APIs que o WebView2 não exponha.

### Configurações

**Referências:** Chrome/Chromium, Brave, Firefox, Zen e Floorp.

Buscar navegação simples, categorias pequenas, pesquisa quando houver escala suficiente, estados claros e explicações para limitações do motor.

### Abas e janela

**Referências:** Chromium, Firefox e Zen.

Estudar grupos, abas fixadas, restauração, fechamento em massa, atalhos, overflow e comportamento ao maximizar/tela cheia.

### Atualização, recuperação e distribuição

**Referências:** LibreWolf, ungoogled-chromium, Firefox e Ladybird.

Estudar atualização confiável, rollback, separação de dados, crash recovery, builds reproduzíveis quando viável e redução de divergência.

### Privacidade e permissões

**Referências:** Chromium, Brave e Firefox.

Separar permissões do site, dados persistentes, janela privada e limpeza de dados. Preferir controles compreensíveis a um painel cheio de opções sem contexto.

## O que não vamos copiar

- Marca, ícones, textos ou identidade visual de outros navegadores.
- Código incompatível com a licença do Rumo ou cuja origem não esteja clara.
- Funcionalidades só porque são populares.
- Workarounds frágeis para fingir suporte que o WebView2 não oferece.
- Complexidade de um navegador completo quando uma solução menor atende ao nosso produto.
- Um motor próprio. O Rumo continua usando WebView2 enquanto essa decisão for adequada.

## Modelo curto para registrar uma decisão

```md
### <decisão>

**Problema:**  
O que precisamos resolver no Rumo.

**Referências consultadas:**  
Projetos/documentação relevantes.

**Aprendizado:**  
Princípios úteis e diferenças entre as abordagens.

**Restrições do Rumo:**  
WebView2, Windows, portabilidade, segurança, manutenção etc.

**Decisão:**  
Solução escolhida e por quê.

**Não escolhido:**  
Alternativas relevantes e motivo da rejeição.
```

Esse processo existe para tornar o Rumo melhor sem transformá-lo numa colcha de retalhos de outros navegadores. A referência externa ajuda a reduzir erros; a decisão final continua sendo nossa.
