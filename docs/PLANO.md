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

- [x] Favicons nas abas.
- [x] Arrastar/reordenar abas.
- [x] Fixar abas.
- [x] Autocomplete da omnibox usando favoritos e histórico.
- [ ] Menu de contexto de página.
- [x] Janela privada e limpeza de dados.
- [x] Modo de tela cheia com F11/Esc.
- [ ] Refinar visual com screenshots reais em Windows.

## Fase 4 — Distribuição

- [x] Oferecer a página oficial do WebView2 Runtime quando ele não está instalado.
- [x] Instalador opcional por usuário mantendo o ZIP portátil.
- [ ] Assinatura de código.

## Fase 5 — Mobile

- [ ] Reavaliar Android/iOS após estabilidade no Windows.
- [ ] Só depois avaliar sincronização.
