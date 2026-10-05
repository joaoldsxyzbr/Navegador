# Requisitos do produto

Este documento registra requisitos funcionais e de distribuição já aprovados para o Navegador.

## Interface

A interface visual deve permanecer próxima do Chromium upstream. O projeto não terá um redesign próprio de abas, omnibox, menus ou configurações.

### Tema escuro

- O Navegador inicia em **tema escuro por padrão**.
- O tema deve usar o suporte visual nativo do Chromium.
- O usuário poderá escolher entre:
  - Escuro;
  - Claro;
  - Sistema.
- A implementação não deve criar um tema paralelo nem substituir componentes nativos sem necessidade.

## Portabilidade no Windows

A distribuição Windows deve ser portátil.

Critérios:

- fornecida como ZIP/pasta executável;
- não exigir instalador;
- não exigir privilégios administrativos para uso normal;
- não depender de serviço permanente do Windows;
- não depender de entradas obrigatórias no Registro para funcionar;
- armazenar o perfil local em uma pasta controlada pelo Navegador, preferencialmente `Data/` ao lado do executável;
- poder ser copiada ou movida como uma unidade.

O Chromium suporta sobrescrever o diretório de dados do usuário por `--user-data-dir`. O launcher/empacotamento do Navegador deve usar esse mecanismo ou integração equivalente.

### Limite de portabilidade de credenciais

Dados protegidos pelas APIs criptográficas do sistema operacional podem ficar vinculados ao usuário ou computador Windows. Portanto, mover a pasta não garante que sessões, cookies, senhas ou outros segredos protegidos pelo SO funcionem em outro computador.

O Navegador não tentará contornar essas proteções do sistema operacional.

## Atualizações

A distribuição Windows deve permitir atualização sem baixar e substituir manualmente a pasta.

Requisitos atuais:

- um executável próprio deve oferecer a ação Verificar atualizações;
- o pacote deve ser baixado por HTTPS;
- o SHA-256 publicado deve ser validado antes da instalação;
- Data/ nunca deve ser substituída pelo atualizador;
- falhas durante a troca devem restaurar a versão anterior;
- a primeira implementação usa pacote completo; atualização diferencial poderá ser adicionada depois;
- não deve existir serviço residente obrigatório para manter o caráter portátil.

A integração desse fluxo à página Sobre o Navegador fica para a fase em que o Chromium próprio estiver sendo compilado.

## Sincronização

Sincronização entre Windows e Android fica **fora do escopo atual**.

Nesta fase:

- não haverá conta do Navegador;
- não haverá backend de sincronização;
- não haverá sincronização de favoritos, abas, histórico ou configurações;
- não haverá sincronização própria de senhas, passkeys ou autofill;
- não haverá dependência de Chrome Sync/Google Sync.

Esse recurso poderá ser reavaliado no futuro como uma iniciativa separada.

## Android

No Android, “portátil” não significa executar sem instalação. O app será instalado normalmente pelo sistema.

## Prioridades

1. Primeiro build Chromium reproduzível no Windows.
2. Tema escuro padrão.
3. Empacotamento portátil Windows.
4. Identidade mínima do Navegador.
5. Funções próprias mantendo a interface Chromium.
6. Atualização do Chromium.
7. Android.
