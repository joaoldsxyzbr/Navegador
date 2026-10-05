# Atualizações do Navegador

## Objetivo

O usuário não deve precisar baixar manualmente um novo ZIP a cada versão.

A distribuição Windows contém:

~~~text
Navegador/
├── Navegador.exe
├── Atualizar Navegador.exe
├── App/
├── Data/
├── Updater/
└── version.txt
~~~

## Fluxo da v0.1.0

1. Atualizar Navegador.exe consulta o manifesto update.json da release mais recente.
2. Compara a versão publicada com version.txt.
3. Se houver versão nova, oferece o download.
4. Baixa o pacote completo para a pasta temporária do Windows.
5. Calcula SHA-256 localmente.
6. Só continua se o hash for idêntico ao publicado.
7. O aplicador fecha somente processos Chromium executados de App/.
8. Substitui arquivos gerenciados do programa.
9. Não substitui nem apaga Data/.
10. Se a troca falhar, restaura o backup.
11. Reinicia o Navegador.

## Canal

A v0.1.0 usa um único canal público, apontado por:

https://github.com/joaoldsxyzbr/Navegador/releases/latest/download/update.json

O manifesto contém:

- versão;
- URL exata do ZIP;
- SHA-256.

## Interface

Na primeira versão testável, o atualizador é um executável separado, Atualizar Navegador.exe, com o botão Verificar atualizações.

Quando o projeto estiver compilando o Chromium próprio, o mesmo fluxo poderá ser ligado à página nativa Sobre o Navegador por um patch pequeno, sem mudar o protocolo nem o formato do pacote.

## Por que não usar um serviço residente

O Navegador é portátil. Portanto, a primeira implementação evita:

- serviço Windows permanente;
- tarefa agendada obrigatória;
- instalador;
- dependência obrigatória de Registro.

Isso mantém a pasta autocontida.

## Pacote completo antes de diferencial

A atualização inicial baixa o ZIP completo. Atualização diferencial fica para uma otimização posterior.

Essa escolha reduz complexidade e risco de corrupção na primeira versão. A experiência do usuário continua sendo de um clique; ele não precisa procurar ou substituir manualmente o pacote.

## Segurança

- HTTPS obrigatório;
- SHA-256 obrigatório;
- rollback do conjunto gerenciado;
- Data/ fora do conjunto substituído;
- pacote só é aplicado se contiver App/chrome.exe e version.txt compatível com o manifesto.
