# Plano de implementação

## Estado — 05/10/2026

A direção é um app Windows próprio em C# / WinForms com WebView2. O repositório foi reiniciado numa nova árvore; o histórico Git e a release v0.1.0 continuam preservados.

## Fase 1 — Protótipo Windows

- [x] Definir WebView2 para PC e começar pelo Windows.
- [x] Criar app WinForms com motor WebView2.
- [x] Implementar abas, endereço/busca, voltar, avançar, recarregar e tema escuro.
- [x] Criar perfil persistente em `Data/WebView2`.
- [x] Habilitar API de extensões e criar tela para instalar pasta local, ativar, desativar e remover.
- [ ] Compilar e validar no CI Windows.
- [ ] Testar no PC com WebView2 Runtime.
- [ ] Comparar screenshots e aproximar a interface do Chrome.
- [ ] Testar extensões populares, ícones, popups e limitações.

## Fase 2 — Completar o MVP

- [ ] Polir a interface Chrome-like: formas de abas, barra, menus, ícones, estados e atalhos.
- [ ] Favoritos e histórico.
- [ ] Downloads com escolha de destino e tela de downloads.
- [ ] Janela privada e exclusão de dados.
- [ ] Tela inicial, configurações e acessibilidade.
- [ ] Avaliar instalação da Chrome Web Store e popups/ações de extensão; WebView2 só aceita diretamente a pasta descompactada pela API.

## Fase 3 — Distribuição

- [ ] Decidir como atender PCs sem WebView2 Runtime.
- [ ] Gerar pacote portátil x64 e validar em pasta extraída.
- [ ] Atualização com HTTPS, SHA-256, preservação do perfil e rollback.
- [ ] Publicar uma nova release sem alterar a v0.1.0.

## Fase 4 — Celular

- [ ] Reavaliar Android e iOS depois que a versão Windows estiver estável.
- [ ] Definir motor e interface mobile separadamente.
- [ ] Só então avaliar sincronização.

