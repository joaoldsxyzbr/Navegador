using Navegador.Core;

namespace Navegador.Tests;

public static class VisualCppRuntimeTests
{
    public static IEnumerable<TestCase> Cases =>
    [
        new("runtime CefSharp: detecta quando não está instalado", () =>
        {
            Assert.False(VisualCppRuntimePolicy.IsSupported(0, 14, 42));
        }),

        new("runtime CefSharp: recusa uma versão anterior ao Visual C++ 2022", () =>
        {
            Assert.False(VisualCppRuntimePolicy.IsSupported(1, 14, 29));
        }),

        new("runtime CefSharp: aceita a versão mínima do Visual C++ 2022", () =>
        {
            Assert.True(VisualCppRuntimePolicy.IsSupported(1, 14, 30));
        }),

        new("runtime CefSharp: aceita atualizações compatíveis", () =>
        {
            Assert.True(VisualCppRuntimePolicy.IsSupported(1, 14, 42));
            Assert.True(VisualCppRuntimePolicy.IsSupported(1, 15, 0));
        })
    ];
}
