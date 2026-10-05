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
- navegação;
- integração com Windows e Android.

## Regras

- Código específico de plataforma só entra em Platforms/.
- Regras reutilizáveis devem ficar em Navegador.Core.
- Recursos novos devem evitar serviços em segundo plano sem necessidade.
- Armazenamento futuro de histórico, favoritos e configurações deve ser local por padrão.
- Não adicionar telemetria sem decisão explícita.
