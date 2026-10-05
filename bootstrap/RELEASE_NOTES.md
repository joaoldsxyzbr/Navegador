## Primeira versão Chromium do Navegador

Esta é a primeira versão testável após a migração para Chromium + overlay.

### Incluído

- Chromium com interface upstream;
- tema escuro forçado por padrão no launcher;
- execução portátil, sem instalador;
- perfil local preservado em Data;
- Navegador.exe para iniciar o browser;
- Atualizar Navegador.exe para verificar, baixar e aplicar atualizações;
- validação SHA-256 antes de instalar uma atualização;
- rollback básico se a troca do pacote falhar.

### Importante

Nesta primeira versão, App usa um snapshot oficial do Chromium como bootstrap. O build integral do overlay próprio virá quando houver ambiente com capacidade suficiente para compilar Chromium. O formato portátil e o atualizador já foram desenhados para continuar iguais quando essa troca acontecer.
