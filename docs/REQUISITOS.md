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

A sincronização entre dispositivos é o mecanismo oficial para transportar dados suportados entre Windows e Android.

## Sincronização Windows ↔ Android

O Navegador terá sincronização própria e não dependerá de Chrome Sync/Google Sync.

### Primeira versão

Sincronizar:

- favoritos;
- abas abertas;
- histórico;
- configurações selecionadas.

### Dados sensíveis

Senhas, passkeys, autofill e outros segredos entram somente depois de existir um desenho de criptografia ponta a ponta revisado e testado.

### Requisitos de arquitetura

- backend controlado pelo projeto;
- cliente Windows e Android usando o mesmo protocolo;
- identificação de dispositivo;
- resolução explícita de conflitos;
- transporte via HTTPS;
- dados sensíveis nunca devem ser armazenados em texto puro no servidor;
- preferir criptografia ponta a ponta para o conteúdo sincronizado;
- o backend e o protocolo também devem ser versionados e documentados neste repositório.

A escolha da infraestrutura do backend será feita separadamente antes da implementação.

## Android

No Android, “portátil” não significa executar sem instalação. O app será instalado normalmente pelo sistema e participará da mesma sincronização do Windows.

## Prioridades

1. Primeiro build Chromium reproduzível no Windows.
2. Tema escuro padrão.
3. Empacotamento portátil Windows.
4. Identidade mínima do Navegador.
5. Base da sincronização.
6. Cliente Android e sincronização entre plataformas.
