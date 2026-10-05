# Bootstrap da primeira versão

A primeira versão testável do Navegador usa temporariamente um snapshot oficial do Chromium para Windows x64 como base binária.

- Chromium snapshot: 154.0.8037.0
- revisão: 1689481
- origem: infraestrutura oficial chromium-browser-snapshots

## Por que existe

O runner Windows padrão do GitHub não possui espaço suficiente para o checkout e build completo do Chromium. A documentação oficial do Chromium exige pelo menos 100 GB livres para o build Windows, enquanto o runner padrão do GitHub possui 14 GB.

O bootstrap permite validar agora:

- modo portátil;
- launcher;
- tema escuro padrão;
- atualização automática do pacote;
- preservação da pasta Data;
- processo de release.

## Limite

Este snapshot não é o build final do overlay do Navegador. Ele é usado somente na primeira etapa testável. Assim que houver um ambiente de build Chromium adequado, o conteúdo de App será substituído pelo build próprio gerado a partir de chromium/VERSION e dos patches do projeto, sem mudar o formato portátil nem o protocolo do atualizador.
