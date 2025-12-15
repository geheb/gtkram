namespace GtKram.WebApp.Html;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public static class HtmlExtensions
{
    public static IHtmlContent AjaxCsrfToken(this IHtmlHelper helper)
    {
        var antiForgery = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiForgery.GetTokens(helper.ViewContext.HttpContext);
        return new HtmlString($"{tokens.FormFieldName}:'{tokens.RequestToken}'");
    }

    public static string AppendVersion(this IHtmlHelper helper, string path) =>
        helper.ViewContext.HttpContext
            .RequestServices
            .GetRequiredService<IFileVersionProvider>()
            .AddFileVersionToPath(helper.ViewContext.HttpContext.Request.PathBase, path);

    public static IHtmlContent CreateTagContent(this IHtmlHelper helper, string[] items, string tag)
    {
        if (items.Length == 0)
        {
            return HtmlString.Empty;
        }
        return new HtmlString($"<{tag}>" + string.Join($"</{tag}><{tag}>", items) + $"</{tag}>");
    }

    public static async Task<IHtmlContent> ReadTextContent(this IHtmlHelper helper, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return helper.Raw("Missing file: " + path);
        }
        var env = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var fullPath = path.StartsWith("~/", StringComparison.OrdinalIgnoreCase) ? Path.Combine(env.WebRootPath, path[2..]) : path;
        if (!File.Exists(fullPath))
        {
            return helper.Raw("Missing file: " + path);
        }
        return helper.Raw(await File.ReadAllTextAsync(fullPath));
    }

    public static string RandomFontawesomeFace(this IHtmlHelper _)
    {
        var values = new[]
        {
            "fa-face-smile",
            "fa-face-smile-wink",
            "fa-face-smile-beam",
            "fa-face-grin-wink",
            "fa-face-grin-wide",
            "fa-face-grin-hearts",
            "fa-face-grin-stars"
        };
        var random = new Random(Environment.TickCount);
        return values[random.Next(values.Length)];
    }
}
