# Plano de implementação

## Estado

Linha Windows em C# / WinForms / .NET 10 + CefSharp/Chromium. A migração é a versão 0.6.0; o Core continua separado e testável.

## Fase 1 — Protótipo Windows

- [x] WinForms, abas, omnibox, navegação e tema escuro.
- [x] CefSharp com Chrome Runtime e perfil Chromium portátil.
- [x] Perfil persistente com modo portátil.
- [x] Extensões locais descompactadas.
- [x] CI Windows.

## Fase 2 — MVP de uso diário

- [x] Favoritos.
- [x] Histórico.
- [x] Downloads.
- [x] Sessão restaurável.
- [x] Configurações.
- [x] Instância única.
- [x] Atualização integrada com SHA-256 e rollback.
- [x] Core testável.
- [x] Nova guia própria com pesquisa e atalhos.
- [x] Reabrir guia fechada com Ctrl+Shift+T.

## Fase 3 — Polimento

- [x] Favicons nas abas.
- [x] Arrastar/reordenar abas.
- [x] Fixar abas.
- [x] Autocomplete da omnibox usando favoritos e histórico.
- [ ] Menu de contexto de página.
- [x] Janela privada e limpeza de dados.
- [x] Modo de tela cheia com F11/Esc.
- [ ] Refinar visual com screenshots reais em Windows.

## Fase 4 — Distribuição

- [x] Distribuir Chromium e CefSharp com o app, sem exigir Edge WebView2 Runtime.
- [x] Instalador opcional por usuário mantendo o ZIP portátil.
- [ ] Validar no Windows uma instalação/remoção de extensão e o popup de uma extensão real.
- [ ] Confirmar downloads e limpeza de cache em execução com Chrome Runtime.
- [ ] Assinatura de código.

## Fase 5 — Mobile

- [ ] Reavaliar Android/iOS após estabilidade no Windows.
- [ ] Só depois avaliar sincronização.
