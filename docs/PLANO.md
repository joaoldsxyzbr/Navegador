# Plano de implementação

## Estado

Linha Windows em C# / WinForms / .NET 10 + WebView2. O código está em 0.5.0 em preparação para release, com Core separado e testes.

## Fase 1 — Protótipo Windows

- [x] WebView2, WinForms, abas, omnibox, navegação e tema escuro.
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

- [ ] Favicons nas abas/favoritos.
- [ ] Arrastar/reordenar abas.
- [ ] Fixar abas.
- [ ] Autocomplete da omnibox usando histórico.
- [ ] Menu de contexto de página.
- [ ] Janela privada e limpeza de dados.
- [ ] Refinar visual com screenshots reais em Windows.

## Fase 4 — Distribuição

- [ ] Resolver experiência para PC sem WebView2 Runtime.
- [ ] Instalador opcional mantendo pacote portátil.
- [ ] Assinatura de código.

## Fase 5 — Mobile

- [ ] Reavaliar Android/iOS após estabilidade no Windows.
- [ ] Só depois avaliar sincronização.
