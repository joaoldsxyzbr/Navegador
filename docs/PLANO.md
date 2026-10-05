# Plano de implementação

## Objetivo

Entregar um navegador leve para Windows e Android, com uma base compartilhada e comportamento nativo por plataforma.

## Checkpoints

- [x] Definir stack e arquitetura inicial.
- [x] Criar repositório e documentação base.
- [x] Criar o esqueleto funcional com navegação web.
- [x] Validar build Windows no GitHub Actions.
- [x] Validar build Android no GitHub Actions.
- [x] Implementar abas.
- [x] Implementar favoritos.
- [x] Implementar histórico local.
- [ ] Implementar downloads.
- [ ] Implementar modo privado.
- [ ] Implementar configurações e atualização do navegador.
- [ ] Preparar distribuição portátil no Windows.
- [ ] Preparar pacote Android.

## Escopo do primeiro MVP

1. Abrir URL ou pesquisar texto digitado.
2. Voltar e avançar.
3. Atualizar página.
4. Voltar à página inicial.
5. Sincronizar a barra de endereço com a página atual.
6. Rodar em Windows e Android a partir da mesma base.
7. Manter abas, favoritos e histórico local.
8. Baixar arquivos com integração nativa de cada plataforma.
9. Oferecer modo privado com isolamento real.
10. Permitir verificar e aplicar atualizações pelas configurações.

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
