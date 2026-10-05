# Plano de implementação

## Objetivo

Entregar um navegador leve para Windows e Android, com uma base compartilhada e comportamento nativo por plataforma.

## Checkpoints

- [x] Definir stack e arquitetura inicial.
- [x] Criar repositório e documentação base.
- [x] Criar o esqueleto funcional com navegação web.
- [x] Validar builds Windows e Android no GitHub Actions.
- [x] Implementar abas, favoritos e histórico local.
- [x] Implementar downloads nativos.
- [x] Implementar modo privado isolado.
- [x] Criar configurações e consulta de atualização por release do projeto.
- [x] Preparar fluxo de publicação de ZIP portátil e APK assinado.
- [x] Corrigir as falhas de build e obter CI final verde para Windows e Android.
- [ ] Cadastrar a chave Android nos segredos do GitHub Actions.
- [ ] Publicar a primeira release pública assinada.
- [ ] Validar instalação e atualização nos dispositivos Windows e Android.

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
10. Consultar, validar e aplicar atualizações pelas configurações.

## Fora do primeiro MVP

- Sincronização em nuvem.
- Extensões.
- Conta de usuário.
- Motor próprio.
- iOS.

## Critérios de aceite

- Compilar para Windows e Android.
- Navegar em páginas HTTPS e pesquisar texto que não seja URL.
- Navegação privada não compartilhar cookies, cache ou armazenamento web com a sessão normal.
- Atualizador consultar apenas releases do Navegador.
- Pacote ser confirmado pelo usuário e validado por SHA-256 antes da instalação.
- Android manter a mesma chave de assinatura entre versões.

## Política de validação

O CI completo roda no pull request para main ou por execução manual. Ajustes intermediários na preview devem ser agrupados antes desse CI.
