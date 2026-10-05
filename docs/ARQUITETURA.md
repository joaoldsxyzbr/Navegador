# Arquitetura

## Decisão principal

O projeto usa .NET MAUI para compartilhar a maior parte da interface e da lógica entre Windows e Android.

### Motores web

- Windows: WebView2, baseado em Edge/Chromium.
- Android: android.webkit.WebView, baseado em Chromium.

O projeto não embarca um Chromium completo próprio no MVP. Isso reduz tamanho, atualização e manutenção.

## Camadas

### Navegador.Core

Código independente de interface e plataforma.

Responsabilidades iniciais:

- interpretar texto da barra de endereço;
- diferenciar URL de pesquisa;
- manter regras que possam ser testadas sem carregar MAUI.

### Navegador

Aplicativo MAUI.

Responsabilidades:

- interface;
- WebView;
- abas e estado visual;
- navegação;
- favoritos e histórico local;
- integração com Windows e Android;
- consulta, validação e início de atualização.

## Abas

Cada aba possui sua própria instância de WebView. Apenas a aba ativa fica visível. Isso preserva a pilha de navegação nativa de cada aba e evita recriar manualmente o histórico de voltar/avançar.

## Persistência local

Favoritos e histórico são arquivos JSON armazenados em FileSystem.Current.AppDataDirectory.

- favoritos.json
- historico.json

O histórico mantém no máximo 500 entradas.
Os favoritos mantêm no máximo 200 entradas.

No Windows, os dados do WebView2 ficam em uma subpasta WebView2 dentro do AppDataDirectory para evitar escrita no diretório de instalação.

## Downloads

Downloads usam os recursos nativos do motor e da plataforma.

### Windows

O WebView2 mantém o diálogo padrão de download. O navegador ajusta o caminho apenas quando necessário para impedir sobrescrita silenciosa de um arquivo existente.

### Android

O WebView usa DownloadListener e delega o download ao DownloadManager do Android.

- cookies da sessão são encaminhados ao download;
- User-Agent é preservado;
- Android 10 ou mais recente salva em Downloads público;
- versões anteriores usam o diretório externo do aplicativo para evitar solicitar permissão ampla de armazenamento.

## Modo privado

O modo privado é separado da pilha normal de abas para garantir isolamento real.

### Windows

- abre uma janela nativa separada;
- inicializa WebView2 com perfil InPrivate;
- não passa pela persistência de histórico ou favoritos.

### Android

- disponível a partir do Android 9 (API 28);
- abre uma Activity dedicada no processo :private;
- chama WebView.SetDataDirectorySuffix("private") antes de criar o WebView;
- usa diretório de dados independente e limpa cookies, armazenamento web, cache e histórico ao encerrar;
- não acessa o BrowserDataStore do aplicativo.

No Android 7 e 8, o recurso fica indisponível porque a API de isolamento necessária não existe.

## Atualizações

- O aplicativo consulta apenas a última release pública do próprio repositório.
- Nenhum token pessoal fica embutido no navegador.
- O pacote da plataforma é selecionado por nome fixo, URL HTTPS e host github.com.
- O hash SHA-256 informado pela API do GitHub é obrigatório e conferido após o download.
- O usuário confirma o download antes de instalar.
- Windows troca os arquivos da pasta portátil depois de encerrar o navegador.
- Android abre o instalador de sistema para a confirmação final.
- APKs de todas as versões precisam manter a mesma chave de assinatura.

## Distribuição

O workflow .github/workflows/release.yml gera um ZIP portátil para Windows e um APK assinado para Android em cada tag v*. A chave Android fica apenas nos segredos do GitHub Actions. A consulta do app depende de releases públicas.

## CI

O workflow completo roda no pull request para main ou por execução manual. Ele não roda em cada push da preview.

Cada alvo instala apenas o workload necessário. O projeto Navegador.Core é restaurado separadamente em net10.0, e o app é restaurado sem dependências para o TargetFramework da matriz. No Windows, restore e build usam o RID win-x64 para obter o runtime pack correspondente.

## Regras

- Código específico de plataforma só entra em Platforms/.
- Regras reutilizáveis devem ficar em Navegador.Core.
- Recursos novos devem evitar serviços em segundo plano sem necessidade.
- Armazenamento de histórico, favoritos e configurações é local por padrão.
- Não adicionar telemetria sem decisão explícita.
