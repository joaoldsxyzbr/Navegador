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

## Regras

- Código específico de plataforma só entra em Platforms/.
- Regras reutilizáveis devem ficar em Navegador.Core.
- Recursos novos devem evitar serviços em segundo plano sem necessidade.
- Armazenamento de histórico, favoritos e configurações é local por padrão.
- Não adicionar telemetria sem decisão explícita.
