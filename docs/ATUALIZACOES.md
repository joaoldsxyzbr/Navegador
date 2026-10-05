# Atualizações do Navegador

## Requisitos

O Navegador deve oferecer um fluxo simples de atualização, sem exigir que a pessoa baixe e substitua manualmente a pasta.

- download por HTTPS;
- validação SHA-256 do pacote;
- preservação de dados do perfil;
- rollback quando a substituição falhar;
- sem serviço Windows residente obrigatório;
- distribuição portátil no Windows.

## Estado Chromium v0.1.0

A v0.1.0 contém um launcher e atualizador separados no pacote Chromium. Eles continuam vinculados ao layout e ao executável Chromium e não devem ser tratados como atualizador da futura linha Firefox.

## Linha Firefox

Antes de publicar Firefox, definir e testar:

1. executável e layout final do pacote;
2. localização do perfil Firefox em Data/;
3. fechamento seguro do processo Firefox;
4. quais arquivos o updater pode substituir;
5. preservação/backup do perfil Firefox;
6. hash, rollback e reinício;
7. distribuição/canal Firefox separado da v0.1.0 Chromium.

Não misturar nem migrar automaticamente o perfil Chromium existente. A integração visual do atualizador no frontend do Firefox fica depois de demonstrar que o pacote, as atualizações e o perfil funcionam com segurança.
