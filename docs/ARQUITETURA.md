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
- integração com Windows e Android.

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

O modo privado é separado da pilha normal de abas para garantir isolamento real, e não apenas ocultar o histórico da interface.

### Windows

- abre uma janela nativa separada;
- inicializa WebView2 com `CoreWebView2ControllerOptions.IsInPrivateModeEnabled = true`;
- usa perfil próprio da sessão privada;
- não passa pela persistência de histórico ou favoritos do aplicativo.

### Android

- disponível a partir do Android 9 (API 28);
- abre uma Activity dedicada no processo `:private`;
- chama `WebView.SetDataDirectorySuffix("private")` antes de criar o WebView;
- o processo privado usa diretório de dados diferente da sessão normal;
- cookies, armazenamento web, cache, histórico e dados de formulário são limpos ao abrir e ao encerrar;
- a sessão privada não acessa o `BrowserDataStore` do aplicativo.

No Android 7 e 8, o recurso fica indisponível porque a API necessária para separar diretórios do WebView não existe. Isso evita apresentar uma falsa sensação de privacidade.

## CI

O workflow completo roda no pull request para `main` ou por execução manual. Ele não roda em cada push da `preview`, reduzindo builds pesados durante ajustes intermediários.

Cada alvo instala apenas o workload necessário e restaura somente o TargetFramework correspondente antes da compilação.

## Regras

- Código específico de plataforma só entra em Platforms/.
- Regras reutilizáveis devem ficar em Navegador.Core.
- Recursos novos devem evitar serviços em segundo plano sem necessidade.
- Armazenamento de histórico, favoritos e configurações é local por padrão.
- Não adicionar telemetria sem decisão explícita.
