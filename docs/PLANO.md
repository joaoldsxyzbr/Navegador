# Plano de implementação

## Estado — 05/10/2026

A direção foi simplificada para um app Windows próprio em C# / WinForms com WebView2. O repositório foi reiniciado numa nova árvore; o histórico Git e a release v0.1.0 continuam preservados.

## Fase 1 — Protótipo Windows

- [x] Definir WebView2 para PC e começar pelo Windows.
- [x] Criar app WinForms com motor WebView2.
- [x] Implementar abas, endereço/busca, voltar, avançar, recarregar e tema escuro.
- [x] Guardar perfil em `Data/WebView2`.
- [ ] Compilar e validar no CI Windows.
- [ ] Testar no computador do João com WebView2 Runtime.
- [ ] Ajustar visual e tratar navegação popup/links externos.

## Fase 2 — Completar o MVP

- [ ] Favoritos e histórico.
- [ ] Downloads com escolha de destino e tela de downloads.
- [ ] Janela privada e exclusão de dados.
- [ ] Tela inicial, configurações e acessibilidade.
- [ ] Fluxo de atualização integrado e seguro.

## Fase 3 — Distribuição

- [ ] Decidir como atender PCs sem WebView2 Runtime.
- [ ] Gerar pacote portátil x64 e validar em pasta extraída.
- [ ] Atualização com HTTPS, SHA-256, preservação do perfil e rollback.
- [ ] Publicar uma nova release sem alterar a v0.1.0.

## Fase 4 — Celular

- [ ] Reavaliar Android e iOS depois que a versão Windows estiver estável.
- [ ] Definir motor e interface mobile separadamente.
- [ ] Só então avaliar sincronização entre dispositivos.

## Limites do primeiro protótipo

O protótipo inicial não tem ainda favoritos, histórico visual, downloads, modo privado, configurações ou atualizador. O CI comprovará compilação; testar navegação real e o comportamento portátil ainda exige executar o app em Windows com o Runtime disponível.
