# Navegador

Navegador simples, leve e multiplataforma para **Windows e Android**, com uma base de código compartilhada em **.NET MAUI**.

## Objetivo

Criar um navegador enxuto, rápido e fácil de manter, evitando recursos que não tragam valor ao uso diário.

## Stack inicial

- .NET 10
- .NET MAUI
- C# + XAML
- Windows: WebView2 (Edge/Chromium)
- Android: Android WebView (Chromium)

## MVP

- Barra de endereço e pesquisa
- Voltar, avançar e atualizar
- Abas
- Página inicial
- Navegação HTTPS
- Favoritos e histórico local
- Downloads nativos
- Modo privado realmente isolado
- Base para configurações e atualização do navegador

## Modo privado

- Windows: janela separada usando perfil WebView2 em modo InPrivate.
- Android 9+: atividade em processo separado, com diretório WebView exclusivo e limpeza de cookies, cache e armazenamento ao encerrar.
- Android 7 e 8: o modo privado fica indisponível para não oferecer um isolamento falso.

## Plataformas

- Windows 10 1809+ / Windows 11
- Android

## Estrutura

```text
src/
  Navegador/
docs/
  PLANO.md
  ARQUITETURA.md
.github/
  workflows/
```

## Entrega e CI

O desenvolvimento ocorre em `preview`. O CI pesado não roda a cada push: ele é executado no pull request final para `main` ou manualmente por `workflow_dispatch`.

## Princípios

1. Leveza antes de excesso de recursos.
2. Uma base compartilhada sempre que possível.
3. Código específico de plataforma apenas quando necessário.
4. Segurança e privacidade por padrão.
5. GitHub como fonte de verdade do projeto.
