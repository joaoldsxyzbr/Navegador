# Arquitetura do Rumo

## Decisão atual

Rumo é o navegador Windows do repositório Navegador: C# / WinForms / .NET 10, usando WebView2 como motor. A release Chromium v0.1.0 é histórica.

## Projetos

| Projeto | TFM | Responsabilidade |
| --- | --- | --- |
| Navegador.Core | net10.0 | caminhos, sessão, favoritos, histórico, configurações, resolução de endereço e protocolo do atualizador |
| Navegador.Windows | net10.0-windows | shell WinForms, WebView2, downloads e atualização |
| Navegador.Tests | net10.0 | testes do Core |

A dependência é Windows → Core.

## Shell e nova guia

O shell usa barra de título própria, abas arredondadas, omnibox com foco destacado, ícones vetoriais desenhados pelo app e botão visível de atualização. A nova guia é uma página interna escura e minimalista, com marca Rumo, pesquisa central e atalhos vindos dos favoritos. A bússola vetorial é `assets/rumo-mark.svg`; `assets/rumo.ico` é embutido no executável.

## Dados e modo portátil

O diretório de dados é único: Data\ ao lado do executável quando a pasta é gravável, senão %LOCALAPPDATA%\Navegador. AppPaths.IsPortable reflete o diretório efetivamente escolhido. Perfil WebView2, sessão, favoritos, histórico e configurações seguem a mesma decisão.

## Persistência

JSON é salvo por arquivo temporário + substituição. Arquivo ausente/corrompido volta para um estado seguro. A sessão remove URLs não persistíveis e preserva a aba ativa.

## Downloads

O Navegador define a pasta de destino, evita sobrescrever silenciosamente arquivos com o mesmo nome, acompanha progresso e traduz os motivos oficiais de interrupção do WebView2.

## Atualização

1. O processo principal consulta releases/latest, baixa o pacote para %LOCALAPPDATA%\Navegador\Updates e valida SHA-256.
2. Uma cópia do executável é criada dentro da pasta segura da atualização e fora da instalação.
3. O auxiliar valida token, PID, pacote, destino e o executável do processo pai, sinaliza prontidão e só então o processo principal fecha.
4. O auxiliar valida o hash novamente, extrai o pacote e substitui os arquivos.
5. Em falha, arquivos antigos são restaurados e arquivos novos da tentativa são removidos.
6. Data\ nunca é substituído.

## Extensões

Extensões Chromium locais descompactadas podem ser instaladas, ativadas e removidas. Chrome Web Store, ícones e popups não são presumidos porque o WebView2 não fornece a interface completa de um navegador Chromium.

## Processo de engenharia

Mudanças significativas de arquitetura, UX, segurança, extensões, atualização e manutenção devem partir do problema do Rumo e comparar soluções já testadas em projetos open source relevantes. O processo e as referências canônicas ficam em [`REFERENCIAS-OPEN-SOURCE.md`](REFERENCIAS-OPEN-SOURCE.md).

A comparação não altera a decisão de base atual: o Rumo continua em C# / WinForms / .NET 10 + WebView2. Referências externas servem para extrair princípios e padrões; limitações do WebView2 devem ser tratadas explicitamente, sem simular suporte inexistente.

## Publicação

VERSION é a fonte única de versão. Releases só são disparadas por tag vX.Y.Z que combine com esse arquivo. O workflow roda testes, publica single-file x64, valida o pacote e cria a release.

## Mobile

Android/iOS continuam posteriores à estabilização do Windows.
