using System.Net;
using System.Text;
using System.Text.Json;
using Navegador.Core.Models;

namespace Navegador.Windows.Ui;

internal static class NewTabPage
{
    public static string Build(IReadOnlyList<Favorite> favorites, bool isPrivate = false)
    {
        var shortcuts = new StringBuilder();

        foreach (var favorite in favorites.Take(8))
        {
            var url = WebUtility.HtmlEncode(favorite.Url);
            var title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(favorite.Title) ? favorite.Url : favorite.Title);
            var initial = WebUtility.HtmlEncode(Initial(favorite.Title, favorite.Url));

            shortcuts.Append(
                "<button class=\"shortcut\" type=\"button\" data-url=\"" + url + "\" title=\"" + title + "\">" +
                "<span class=\"shortcut-icon\">" + initial + "</span>" +
                "<span class=\"shortcut-title\">" + title + "</span>" +
                "</button>");
        }

        var shortcutsBlock = shortcuts.Length == 0
            ? """<div class="empty">Seus favoritos aparecerão aqui.</div>"""
            : shortcuts.ToString();
        var privateNotice = isPrivate
            ? """<div class="privacy-note">O histórico e as abas desta janela não serão salvos no perfil normal.</div>"""
            : string.Empty;

        const string template = """
<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Nova guia — Rumo</title>
<style>
:root{color-scheme:dark;--bg:#202124;--surface:#303134;--hover:#3c4043;--text:#e8eaed;--muted:#9aa0a6;--accent:#74d8ed}
*{box-sizing:border-box}html,body{width:100%;height:100%;margin:0}body{overflow:hidden;background:radial-gradient(circle at 50% 34%,rgba(70,196,225,.08),transparent 34%),var(--bg);color:var(--text);font-family:"Segoe UI",system-ui,sans-serif}
main{width:min(760px,calc(100% - 48px));margin:0 auto;padding-top:clamp(92px,17vh,180px);text-align:center}
.brand{display:inline-flex;align-items:center;gap:14px;margin-bottom:34px;font-size:clamp(32px,4vw,48px);font-weight:500;letter-spacing:-1.8px}
.mark{width:46px;height:46px;display:block;flex:0 0 auto}
.mark svg{display:block;width:100%;height:100%}
.search{height:52px;display:flex;align-items:center;gap:12px;width:100%;padding:0 18px;border:1px solid transparent;border-radius:26px;background:var(--surface);box-shadow:0 1px 6px rgba(0,0,0,.18);transition:.15s}
.search:hover{background:var(--hover)}.search:focus-within{border-color:var(--accent);box-shadow:0 1px 8px rgba(0,0,0,.3)}
.search-icon{width:18px;height:18px;border:2px solid var(--muted);border-radius:50%;position:relative;flex:0 0 auto}.search-icon:after{content:"";position:absolute;width:7px;height:2px;background:var(--muted);right:-5px;bottom:-2px;transform:rotate(45deg);border-radius:2px}
input{width:100%;border:0;outline:0;background:transparent;color:var(--text);font:inherit;font-size:16px}input::placeholder{color:var(--muted)}
.shortcut-grid{margin:34px auto 0;display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;max-width:560px}.shortcut{min-width:0;height:94px;border:0;border-radius:14px;background:transparent;color:var(--text);cursor:pointer;padding:10px 8px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:9px}
.shortcut:hover,.shortcut:focus-visible{background:rgba(255,255,255,.07);outline:none}.shortcut-icon{width:38px;height:38px;display:grid;place-items:center;border-radius:50%;background:var(--surface);color:var(--accent);font-weight:600;font-size:15px;text-transform:uppercase}.shortcut-title{display:block;width:100%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:12px}.empty{grid-column:1/-1;color:var(--muted);font-size:13px;padding:18px}.hint{margin-top:24px;color:var(--muted);font-size:12px}.privacy-note{margin:18px auto 0;color:var(--muted);font-size:13px}
@media(max-width:560px){.shortcut-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}
</style>
</head>
<body><main><div class="brand"><span class="mark" aria-hidden="true">__BRAND_MARK__</span><span>__BRAND_NAME__</span></div>
<form class="search" id="search-form" autocomplete="off"><span class="search-icon" aria-hidden="true"></span><input id="query" autofocus spellcheck="false" placeholder="Pesquisar no Google ou digitar um endereço" aria-label="Pesquisar ou digitar endereço"></form>
<div class="shortcut-grid">__SHORTCUTS__</div>__PRIVATE_NOTICE__<div class="hint">Ctrl+L seleciona a barra de endereço · Ctrl+T abre uma nova guia</div></main>
<script>
const send=value=>{const trimmed=(value||"").trim();if(!trimmed)return;window.chrome.webview.postMessage({type:"navigate",value:trimmed})};
document.getElementById("search-form").addEventListener("submit",e=>{e.preventDefault();send(document.getElementById("query").value)});
document.querySelectorAll("[data-url]").forEach(button=>button.addEventListener("click",()=>send(button.dataset.url)));
</script></body></html>
""";

        return template
            .Replace("__SHORTCUTS__", shortcutsBlock, StringComparison.Ordinal)
            .Replace("__PRIVATE_NOTICE__", privateNotice, StringComparison.Ordinal)
            .Replace("__BRAND_MARK__", Branding.MarkSvg, StringComparison.Ordinal)
            .Replace("__BRAND_NAME__", Branding.Name, StringComparison.Ordinal);
    }

    public static bool TryGetNavigationTarget(string json, out string value)
    {
        value = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "navigate") return false;
            if (!root.TryGetProperty("value", out var target)) return false;

            value = target.GetString()?.Trim() ?? string.Empty;
            return value.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool IsInternalSource(string? source) =>
        string.Equals(source, "about:blank", StringComparison.OrdinalIgnoreCase);

    private static string Initial(string? title, string url)
    {
        var source = string.IsNullOrWhiteSpace(title) ? url : title.Trim();

        foreach (var character in source)
        {
            if (char.IsLetterOrDigit(character)) return character.ToString();
        }

        return "•";
    }
}
