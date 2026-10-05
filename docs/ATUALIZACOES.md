# Atualizações e distribuição

## Origem das versões

O aplicativo consulta a API pública de releases de joaoldsxyzbr/Navegador. O repositório e os releases precisam estar públicos para que o navegador consulte versões sem token pessoal.

Cada release usa uma tag semântica, como v0.1.1, e contém sempre o pacote Windows:

- Navegador-windows-x64.zip
- Navegador-android.apk (opcional; só é gerado quando solicitado manualmente com os segredos Android configurados)

O aplicativo escolhe o pacote da plataforma, exibe as notas e só inicia o download após confirmação.

## Integridade e instalação

O atualizador exige o campo SHA-256 do asset fornecido pela API do GitHub e compara o hash do arquivo baixado. Se o pacote ou o digest não estiver disponível, a instalação é bloqueada.

- Windows: extrai o ZIP sobre a pasta portátil após fechar o navegador e inicia a nova versão.
- Android: abre o instalador do sistema para confirmação. A instalação manual pode pedir permissão para instalar apps desconhecidos para o Navegador.

## Assinatura Android

A chave precisa ser a mesma em todas as versões distribuídas por APK. Nunca gere uma chave diferente a cada execução e nunca envie o keystore para o Git.

Cadastre os segredos em Settings → Secrets and variables → Actions:

- ANDROID_KEYSTORE_BASE64: keystore codificado em Base64.
- ANDROID_KEYSTORE_PASSWORD: senha do keystore.
- ANDROID_KEY_ALIAS: alias da chave.
- ANDROID_KEY_PASSWORD: senha da chave.

Esses segredos só são exigidos ao solicitar a geração do APK. A publicação do ZIP Windows não depende da assinatura Android.

## Criar uma release

1. Atualize ApplicationDisplayVersion e ApplicationVersion em src/Navegador/Navegador.csproj.
2. Envie uma tag vMAJOR.MINOR.PATCH para um commit aprovado na main; o GitHub Actions publica o ZIP portátil Windows.
3. Para o APK, execute manualmente o workflow Release na mesma tag e marque include_android; isso exige os quatro segredos Android.
4. Instale e teste cada pacote antes de distribuir a versão.

A visibilidade pública do repositório e a configuração inicial dos segredos são etapas operacionais do proprietário.
