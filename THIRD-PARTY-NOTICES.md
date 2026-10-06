# Avisos de terceiros

O pacote do Rumo redistribui CefSharp e Chromium Embedded Framework (CEF), com componentes do Chromium. Os avisos e textos de licença entregues pelos pacotes NuGet usados na compilação são incluídos no pacote Windows em `TERCEIROS/`, organizados por pacote.

O pacote também inclui `VC_redist.x64.exe`, o instalador oficial do Microsoft Visual C++ 2022 Redistributable x64, necessário ao CefSharp. A licença da Microsoft acompanha o próprio instalador. A release baixa o arquivo do endereço oficial `https://aka.ms/vs/17/release/vc_redist.x64.exe` e valida a assinatura Authenticode antes de empacotá-lo.

Fontes oficiais:

- [CefSharp](https://github.com/cefsharp/CefSharp)
- [Chromium Embedded Framework](https://github.com/chromiumembedded/cef)
- [Chromium](https://chromium.googlesource.com/chromium/src/)

O workflow de release verifica se os arquivos de licença foram encontrados antes de criar os artefatos distribuíveis.
