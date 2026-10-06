# Arquitetura do Rumo

## Decisão atual

Rumo é o navegador Windows do repositório Navegador: C# / WinForms / .NET 10, usando CefSharp e Chromium incorporado. A release Chromium v0.1.0 é histórica; as versões WebView2 até v0.5.0 também são históricas.

## Projetos

| Projeto | TFM | Responsabilidade |
| --- | --- | --- |
| Navegador.Core | net10.0 | caminhos, sessão, favoritos, histórico, configurações, resolução de endereço e protocolo do atualizador |
| Navegador.Windows | net10.0-windows | shell WinForms, CefSharp/Chromium, downloads e atualização |
| Navegador.Tests | net10.0 | testes do Core |

A dependência é Windows → Core.

## Shell e nova guia

O shell usa barra de título própria, abas arredondadas, omnibox com foco destacado, ícones vetoriais desenhados pelo app e botão visível de atualização. A nova guia é uma página interna escura e minimalista, com marca Rumo, pesquisa central e atalhos vindos dos favoritos. A bússola vetorial é `assets/rumo-mark.svg`; `assets/rumo.ico` é embutido no executável.

## Dados e modo portátil

O diretório de dados é único: Data\ ao lado do executável quando a pasta é gravável, senão %LOCALAPPDATA%\Navegador. AppPaths.IsPortable reflete o diretório efetivamente escolhido. O perfil `Chromium\Profile`, sessão, favoritos, histórico e configurações seguem essa decisão. O perfil legado `WebView2` fica preservado e não é importado.

## Distribuição e runtime

O CefSharp 152 requer o Microsoft Visual C++ 2022 Redistributable x64. A release inclui o instalador oficial da Microsoft: o setup do Rumo o executa se o runtime não estiver instalado, enquanto a distribuição ZIP deixa o instalador ao lado do executável para uso em uma máquina limpa. O aplicativo não depende do Edge WebView2 Runtime.

## Persistência

JSON é salvo por arquivo temporário + substituição. Arquivo ausente/corrompido volta para um estado seguro. A sessão remove URLs não persistíveis e preserva a aba ativa.

## Downloads

O Navegador define a pasta de destino, evita sobrescrever silenciosamente arquivos com o mesmo nome e acompanha progresso por meio do handler de downloads do CefSharp. Se a interface nativa de downloads do Chrome Runtime entrar em conflito com o handler, o CI não detecta esse comportamento; é necessária validação funcional no Windows.

## Atualização

1. O processo principal consulta releases/latest, baixa o pacote para %LOCALAPPDATA%\Navegador\Updates e valida SHA-256.
2. Uma cópia do executável é criada dentro da pasta segura da atualização e fora da instalação.
3. O auxiliar valida token, PID, pacote, destino e o executável do processo pai, sinaliza prontidão e só então o processo principal fecha.
4. O auxiliar valida o hash novamente, extrai o pacote e substitui os arquivos.
5. Em falha, arquivos antigos são restaurados e arquivos novos da tentativa são removidos.
6. Data\ nunca é substituído.

## Extensões

`CefSharpSettings.RuntimeStyle = CefRuntimeStyle.Chrome` habilita as páginas internas de Chromium e o suporte ao sistema de extensões. O shell abre `chrome://extensions/` em uma guia, e o modo de desenvolvedor permite testar extensões descompactadas. Janelas `chrome-extension://` podem ser popups nativos. O projeto não promete compatibilidade universal com APIs de extensão nem instalação pela Chrome Web Store; os fluxos e a barra de ações ainda precisam de teste manual com extensões reais.

## Processo de engenharia

Mudanças significativas de arquitetura, UX, segurança, extensões, atualização e manutenção devem partir do problema do Rumo e comparar soluções já testadas em projetos open source relevantes. O processo e as referências canônicas ficam em [`REFERENCIAS-OPEN-SOURCE.md`](REFERENCIAS-OPEN-SOURCE.md).

A decisão atual é C# / WinForms / .NET 10 + CefSharp/Chromium. Referências externas servem para extrair princípios e padrões; limites do runtime Chrome embutido devem ser tratados explicitamente.

## Publicação

VERSION é a fonte única de versão. Releases só são disparadas por tag vX.Y.Z que combine com esse arquivo. O workflow roda testes, publica um pacote x64 self-contained com os arquivos nativos do Chromium ao lado de `Navegador.exe`, valida esses arquivos e cria a release.

## Mobile

Android/iOS continuam posteriores à estabilização do Windows.
