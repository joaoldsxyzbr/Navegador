# Atualizações e distribuição

## Origem das versões

O aplicativo consulta a API pública de releases de joaoldsxyzbr/Navegador. O repositório e os releases precisam estar públicos para que o navegador consulte versões sem token pessoal.

Cada release usa uma tag semântica, como v0.1.1, e contém os arquivos:

- Navegador-windows-x64.zip
- Navegador-android.apk

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

O fluxo de release exige esses segredos e falha sem publicar um APK não assinado.

## Criar uma release

1. Atualize ApplicationDisplayVersion e ApplicationVersion em src/Navegador/Navegador.csproj.
2. Envie uma tag vMAJOR.MINOR.PATCH para um commit aprovado na main.
3. O GitHub Actions publica um ZIP portátil do Windows e um APK Android assinado.
4. Instale os dois pacotes e confira a atualização antes de distribuir a versão.

A visibilidade pública do repositório e a configuração inicial dos segredos são etapas operacionais do proprietário.
