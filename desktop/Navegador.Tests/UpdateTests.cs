using System.Security.Cryptography;
using Navegador.Core.Updates;

namespace Navegador.Tests;

public static class UpdateTests
{
    private const string ValidReleaseJson = """
    {
      "tag_name": "v0.5.0",
      "name": "Navegador v0.5.0",
      "assets": [
        {
          "name": "Navegador-v0.5.0-windows-x64.zip",
          "size": 52428800,
          "browser_download_url": "https://github.com/joaoldsxyzbr/Navegador/releases/download/v0.5.0/Navegador-v0.5.0-windows-x64.zip",
          "digest": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
        },
        {
          "name": "Navegador-v0.5.0-windows-x64.zip.sha256",
          "size": 100,
          "browser_download_url": "https://github.com/joaoldsxyzbr/Navegador/releases/download/v0.5.0/Navegador-v0.5.0-windows-x64.zip.sha256"
        }
      ]
    }
    """;

    public static IEnumerable<TestCase> Cases =>
    [
        new("versão: comparação ignora a quarta parte", () =>
        {
            Assert.Equal(0, VersionFormatter.Compare(new Version(0, 4, 0, 0), new Version(0, 4, 0)));
            Assert.True(VersionFormatter.Compare(new Version(0, 4, 0, 0), new Version(0, 5, 0)) < 0);
            Assert.True(VersionFormatter.Compare(new Version(0, 4, 1, 0), new Version(0, 4, 0)) > 0);
            Assert.True(VersionFormatter.Compare(new Version(1, 0, 0), new Version(0, 9, 9)) > 0);
        }),

        new("versão: formatação de exibição", () =>
        {
            Assert.Equal("0.4.0", VersionFormatter.ToDisplay(new Version(0, 4, 0, 0)));
            Assert.Equal("1.2.3", VersionFormatter.ToDisplay(new Version(1, 2, 3, 77)));
        }),

        new("versão: tag aceita com e sem prefixo v", () =>
        {
            Assert.True(VersionFormatter.TryParseTag("v0.4.0", out var withPrefix));
            Assert.Equal("0.4.0", VersionFormatter.ToDisplay(withPrefix));

            Assert.True(VersionFormatter.TryParseTag("1.10.2", out var withoutPrefix));
            Assert.Equal("1.10.2", VersionFormatter.ToDisplay(withoutPrefix));
        }),

        new("versão: tag inválida é recusada", () =>
        {
            Assert.False(VersionFormatter.TryParseTag("release-final", out _));
            Assert.False(VersionFormatter.TryParseTag("", out _));
            Assert.False(VersionFormatter.TryParseTag(null, out _));
        }),

        new("release: lê tag, pacote, hash e checksum", () =>
        {
            var release = ReleaseParser.Parse(ValidReleaseJson);

            Assert.Equal("v0.5.0", release.TagName);
            Assert.Equal("0.5.0", release.DisplayVersion);
            Assert.Equal("Navegador-v0.5.0-windows-x64.zip", release.PackageName);
            Assert.Equal(52428800, release.SizeBytes);
            Assert.True(release.DownloadUrl.EndsWith("Navegador-v0.5.0-windows-x64.zip", StringComparison.Ordinal));
            Assert.Equal("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", release.ExpectedSha256);
            Assert.NotNull(release.ChecksumUrl);
        }),

        new("release: sem pacote Windows x64 é recusada", () =>
        {
            const string json = """
            { "tag_name": "v0.5.0", "assets": [ { "name": "fonte.tar.gz", "browser_download_url": "https://github.com/x/y" } ] }
            """;

            Assert.Throws<InvalidOperationException>(() => ReleaseParser.Parse(json));
        }),

        new("release: sem tag válida é recusada", () =>
        {
            const string json = """
            { "tag_name": "sem-versao", "assets": [] }
            """;

            Assert.Throws<InvalidOperationException>(() => ReleaseParser.Parse(json));
        }),

        new("release: download fora do github é recusado", () =>
        {
            const string json = """
            {
              "tag_name": "v9.9.9",
              "assets": [
                {
                  "name": "Navegador-v9.9.9-windows-x64.zip",
                  "browser_download_url": "https://exemplo-malicioso.com/Navegador-v9.9.9-windows-x64.zip"
                }
              ]
            }
            """;

            Assert.Throws<InvalidOperationException>(() => ReleaseParser.Parse(json));
        }),

        new("release: download por http é recusado", () =>
        {
            const string json = """
            {
              "tag_name": "v9.9.9",
              "assets": [
                {
                  "name": "Navegador-v9.9.9-windows-x64.zip",
                  "browser_download_url": "http://github.com/x/Navegador-v9.9.9-windows-x64.zip"
                }
              ]
            }
            """;

            Assert.Throws<InvalidOperationException>(() => ReleaseParser.Parse(json));
        }),

        new("release: sem digest ainda aceita o arquivo .sha256", () =>
        {
            const string json = """
            {
              "tag_name": "v0.6.0",
              "assets": [
                {
                  "name": "Navegador-v0.6.0-windows-x64.zip",
                  "browser_download_url": "https://github.com/a/b/Navegador-v0.6.0-windows-x64.zip"
                },
                {
                  "name": "Navegador-v0.6.0-windows-x64.zip.sha256",
                  "browser_download_url": "https://github.com/a/b/Navegador-v0.6.0-windows-x64.zip.sha256"
                }
              ]
            }
            """;

            var release = ReleaseParser.Parse(json);
            Assert.Null(release.ExpectedSha256);
            Assert.NotNull(release.ChecksumUrl);
        }),

        new("checksum: primeiro campo do arquivo é lido", () =>
        {
            var hash = new string('a', 64);
            var content = $"{hash}  Navegador-v0.5.0-windows-x64.zip\n";
            Assert.Equal(hash, ReleaseParser.ParseChecksumFile(content));
        }),

        new("checksum: entrada inválida devolve nulo", () =>
        {
            Assert.Null(ReleaseParser.ParseChecksumFile("   "));
            Assert.Null(ReleaseParser.ParseChecksumFile("não-é-hash"));
        }),

        new("pacote: hash confere", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "package.zip");
                File.WriteAllText(path, "conteúdo do pacote");
                var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

                // Não deve lançar.
                UpdateApplyPolicy.EnsurePackageHashAsync(path, hash).GetAwaiter().GetResult();
                UpdateApplyPolicy.EnsurePackageHashAsync(path, hash.ToLowerInvariant()).GetAwaiter().GetResult();
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("pacote: hash divergente é recusado", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "package.zip");
                File.WriteAllText(path, "conteúdo do pacote");

                Assert.Throws<InvalidOperationException>(() =>
                    UpdateApplyPolicy.EnsurePackageHashAsync(path, new string('0', 64)).GetAwaiter().GetResult());
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("pacote: sem hash informado é recusado", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var path = Path.Combine(directory, "package.zip");
                File.WriteAllText(path, "conteúdo do pacote");

                Assert.Throws<InvalidOperationException>(() =>
                    UpdateApplyPolicy.EnsurePackageHashAsync(path, null).GetAwaiter().GetResult());
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("instalação: só a própria pasta é aceita", () =>
        {
            var own = Navegador.Core.AppPaths.BaseDirectory;

            // Não deve lançar.
            UpdateApplyPolicy.EnsureInstallDirectoryMatches(own, own);
            UpdateApplyPolicy.EnsureInstallDirectoryMatches(own.TrimEnd('\\') + "\\", own);

            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsureInstallDirectoryMatches(@"C:\Windows\System32", own));
            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsureInstallDirectoryMatches(@"C:\PastaInventada\Navegador", own));
        }),

        new("instalação: diretório vazio é recusado", () =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsureInstallDirectoryMatches("   ", Navegador.Core.AppPaths.BaseDirectory));
        }),

        new("pacote: só a pasta de atualizações é aceita", () =>
        {
            var inside = Path.Combine(UpdatePaths.Root, "0.5.0-teste", "package.zip");

            // Não deve lançar: o arquivo não precisa existir, só o caminho precisa ser coerente.
            UpdateApplyPolicy.EnsurePackageInsideUpdateRoot(inside);

            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsurePackageInsideUpdateRoot(@"C:\Windows\Temp\package.zip"));
            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsurePackageInsideUpdateRoot(@"C:\outra-pasta\package.zip"));
        }),

        new("pasta de atualizações reconhece seus filhos", () =>
        {
            Assert.True(UpdatePaths.IsInsideRoot(Path.Combine(UpdatePaths.Root, "a", "b.zip")));
            Assert.False(UpdatePaths.IsInsideRoot(UpdatePaths.Root));
            Assert.False(UpdatePaths.IsInsideRoot(@"C:\Windows"));
            Assert.False(UpdatePaths.IsInsideRoot(""));
        }),

        new("helper: precisa ficar na raiz segura de atualizações e fora da instalação", () =>
        {
            var helper = Path.Combine(UpdatePaths.Root, "0.5.0-teste", "Navegador.Atualizador.exe");
            UpdateApplyPolicy.EnsureHelperLocation(helper, Navegador.Core.AppPaths.BaseDirectory);
            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsureHelperLocation(Path.Combine(Navegador.Core.AppPaths.BaseDirectory, "Navegador.Atualizador.exe"), Navegador.Core.AppPaths.BaseDirectory));
            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsureHelperLocation(@"C:\Temp\Navegador.Atualizador.exe", Navegador.Core.AppPaths.BaseDirectory));
        }),

        new("executável principal: precisa pertencer à instalação informada", () =>
        {
            var install = @"C:\Apps\Navegador";
            UpdateApplyPolicy.EnsureExecutableBelongsToInstallDirectory(@"C:\Apps\Navegador\Navegador.exe", install);
            Assert.Throws<InvalidOperationException>(() =>
                UpdateApplyPolicy.EnsureExecutableBelongsToInstallDirectory(@"C:\OutraPasta\Navegador.exe", install));
        }),

        new("executável: assinatura PE é reconhecida", () =>
        {
            var directory = TestFiles.CreateDirectory();
            try
            {
                var fake = Path.Combine(directory, "fake.exe");
                File.WriteAllBytes(fake, [(byte)'M', (byte)'Z', 0x90, 0x00]);
                Assert.True(UpdateApplyPolicy.LooksLikeWindowsExecutable(fake));

                var notExecutable = Path.Combine(directory, "texto.txt");
                File.WriteAllText(notExecutable, "apenas texto");
                Assert.False(UpdateApplyPolicy.LooksLikeWindowsExecutable(notExecutable));

                Assert.False(UpdateApplyPolicy.LooksLikeWindowsExecutable(Path.Combine(directory, "inexistente.exe")));
            }
            finally
            {
                TestFiles.Delete(directory);
            }
        }),

        new("handshake: token só combina com ele mesmo", () =>
        {
            var handshake = UpdateHandshake.Create(123, @"C:\App\", "abc", "0.5.0");

            Assert.True(handshake.MatchesToken(handshake.Token));
            Assert.False(handshake.MatchesToken(Guid.NewGuid().ToString("N")));
            Assert.False(handshake.MatchesToken(null));
            Assert.False(handshake.MatchesToken(""));
            Assert.Equal(123, handshake.ParentProcessId);
        }),

        new("handshake: cada atualização gera um token novo", () =>
        {
            var first = UpdateHandshake.Create(1, @"C:\App\", "abc", "0.5.0");
            var second = UpdateHandshake.Create(1, @"C:\App\", "abc", "0.5.0");

            Assert.NotEqual(first.Token, second.Token);
        })
    ];
}
