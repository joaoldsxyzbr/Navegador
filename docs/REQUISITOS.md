# Requisitos do Navegador

## Windows

- C# / WinForms / .NET 10.
- WebView2, sem compilar Chromium/Firefox.
- Visual próximo do Chrome, mantendo identidade Navegador.
- Tema escuro.
- Abas, navegação, omnibox e nova guia própria.
- Botão visível para atualizar o Navegador.
- Modo portátil com fallback seguro quando a pasta não for gravável.
- Instância única.

## Dados do usuário

- Sessão restaurável.
- Favoritos com barra e gerenciamento.
- Histórico pesquisável.
- Downloads com destino configurável, progresso, cancelar e abrir.
- Configurações persistentes.
- Arquivo corrompido não pode impedir a inicialização.

## Atualização

- Consultar GitHub Releases por HTTPS.
- Comparar versão instalada/disponível.
- Validar SHA-256 antes e durante a aplicação.
- Helper fora da pasta de instalação e dentro da raiz segura de atualizações.
- Confirmar processo pai e diretório de destino.
- Preservar Data\.
- Rollback de arquivos antigos e remoção de arquivos novos em falha.
- Reiniciar ao concluir.

## Publicação

- Versão em um único lugar: VERSION.
- Release apenas por tag vX.Y.Z compatível.
- Pacote x64 single-file com Navegador.exe na raiz.

## Ainda fora

- Favicons.
- Agrupamento/fixação/reordenação de abas.
- Janela privada.
- Sincronização.
- Android/iOS.
