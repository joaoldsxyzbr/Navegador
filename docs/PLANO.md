# Plano de implementação

## Estado

A direção é um app Windows próprio em C# / WinForms com WebView2, com a lógica testável separada em `Navegador.Core`. O protótipo está na versão `0.5.0`.

## Fase 1 — Protótipo Windows (concluída)

- [x] Definir WebView2 para PC e começar pelo Windows.
- [x] Criar app WinForms com motor WebView2.
- [x] Implementar abas, endereço/busca, voltar, avançar, recarregar e tema escuro.
- [x] Criar perfil persistente.
- [x] Habilitar API de extensões e criar tela para instalar pasta local, ativar, desativar e remover.
- [x] Compilar e validar no CI Windows.
- [x] Comparar a composição visual com o Chrome (barra de título, abas, omnibox, menus).

## Fase 2 — MVP completo (concluída em 0.5.0)

- [x] Favoritos: estrela, barra de favoritos, janela com busca, renomear e remover.
- [x] Histórico: registro, busca, remoção e limpeza total.
- [x] Downloads: pasta de destino, pergunta opcional, faixa de progresso e gerenciador.
- [x] Sessão: reabrir as abas anteriores.
- [x] Configurações, com página inicial e caminho de dados visível.
- [x] Instância única.
- [x] Separar `Navegador.Core` e cobrir a lógica com testes que rodam sem NuGet.
- [x] Endurecer o atualizador: token de uso único, destino fixo, hash conferido no auxiliar e rollback.
- [x] Centralizar a versão em `VERSION` e publicar por etiqueta.

## Fase 3 — Polimento do shell

- [ ] Favicons nas abas e nos favoritos.
- [ ] Reordenar abas arrastando, fixar aba e reabrir aba fechada (`Ctrl+Shift+T`).
- [ ] Autocompletar na barra de endereço usando o histórico.
- [ ] Menu de contexto na página (abrir link em nova aba, copiar endereço, salvar imagem).
- [ ] Janela privada e limpeza de dados pela interface.
- [ ] Página inicial própria, com atalhos e busca.

## Fase 4 — Distribuição

- [ ] Decidir como atender PCs sem WebView2 Runtime (instalador do Runtime ou pacote fixo).
- [ ] Empacotar instalador, preservando o pacote portátil.
- [ ] Assinar o executável e o pacote de atualização.

## Fase 5 — Celular

- [ ] Reavaliar Android e iOS depois que a versão Windows estiver estável.
- [ ] Definir motor e interface mobile separadamente.
- [ ] Só então avaliar sincronização.
