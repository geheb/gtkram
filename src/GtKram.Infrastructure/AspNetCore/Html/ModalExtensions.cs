namespace GtKram.Infrastructure.AspNetCore.Html;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

public static class ModalExtensions
{
    private static readonly string ModalStart = @"
<div id=""{0}"" class=""modal"" aria-hidden=""true"">
    <div class=""modal-background""></div>
    <div class=""modal-card"">
        <header class=""modal-card-head"">
            <p class=""modal-card-title"">{1}</p>
            <button class=""delete close-modal"" aria-label=""close""></button>
        </header>
        <section class=""modal-card-body"">
";

    private static readonly string ModalOkEnd = @"
            <div class=""fa-3x loading-value has-text-centered is-hidden"">
                <i class=""fas fa-spinner fa-spin""></i>
            </div>
        </section>
        <footer class=""modal-card-foot"">
            <button class=""button close-modal"" aria-label=""close"">OK</button>
        </footer>
    </div>
</div>
";

    private static readonly string ModalOkCancelEnd = @"
            <div class=""fa-3x loading-value has-text-centered is-hidden"">
                <i class=""fas fa-spinner fa-spin""></i>
            </div>
        </section>
        <footer class=""modal-card-foot"">
            <button class=""button is-danger confirm"" disabled>OK</button>
            <button class=""button close-modal"" aria-label=""close"">Abbrechen</button>
        </footer>
    </div>
</div>
";

    public static IHtmlContent CreateModalTemplateStart(this IHtmlHelper htmlHelper, string id, string title)
        => htmlHelper.Raw(string.Format(CultureInfo.InvariantCulture, ModalStart, id, title));

    public static IHtmlContent CreateModalTemplateEnd(this IHtmlHelper htmlHelper, bool showOkAndCancel)
        => htmlHelper.Raw(showOkAndCancel ? ModalOkCancelEnd : ModalOkEnd);

    public static IHtmlContent CreateModalTemplateWithOkCancel(this IHtmlHelper htmlHelper, string id, string title, string body)
        => htmlHelper.Raw(string.Format(CultureInfo.InvariantCulture, ModalStart, id, title) + body + ModalOkCancelEnd);
}
