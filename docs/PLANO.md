# Plano de implementação

## Objetivo

Entregar um navegador leve para Windows e Android, com uma base compartilhada e comportamento nativo por plataforma.

## Checkpoints

- [x] Definir stack e arquitetura inicial.
- [x] Criar repositório e documentação base.
- [x] Criar o esqueleto funcional com navegação web.
- [ ] Validar build Windows no GitHub Actions.
- [ ] Validar build Android no GitHub Actions.
- [ ] Implementar abas.
- [ ] Implementar favoritos.
- [ ] Implementar histórico local.
- [ ] Implementar downloads.
- [ ] Implementar modo privado.
- [ ] Preparar distribuição portátil no Windows.
- [ ] Preparar pacote Android.

## Escopo do primeiro MVP

1. Abrir URL ou pesquisar texto digitado.
2. Voltar e avançar.
3. Atualizar página.
4. Voltar à página inicial.
5. Sincronizar a barra de endereço com a página atual.
6. Rodar em Windows e Android a partir da mesma base.

## Fora do primeiro MVP

- Sincronização em nuvem.
- Extensões.
- Conta de usuário.
- Motor próprio.
- iOS.

## Critérios de aceite do primeiro MVP

- Compilar para Windows.
- Compilar para Android.
- Navegar em páginas HTTPS.
- Pesquisa funcionar quando a entrada não for uma URL.
- Controles básicos responderem sem duplicação de lógica entre plataformas.
