namespace GtKram.Ui.Routing;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

public static class HtmlHelperExtensions
{
    public static IHtmlContent CreateBreadcrumb(this IHtmlHelper helper, params object[] routeValues)
    {
        var nav = new TagBuilder("nav");
        nav.AddCssClass("breadcrumb");
        nav.Attributes.Add("aria-label", "breadcrumbs");

        var breadcrumbGenerator = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<NodeGeneratorService>();
        var linkGenerator = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

        if (helper.ViewContext.ActionDescriptor is not CompiledPageActionDescriptor actionDescriptor)
        {
            throw new InvalidOperationException($"Current route isn't a Razor page");
        }

        var node = breadcrumbGenerator.GetNode(actionDescriptor.HandlerTypeInfo);
        var routeEnum = routeValues.GetEnumerator();
        var htmlContent = new StringBuilder();

        while (node != null)
        {
            var path = linkGenerator.GetPathByPage(node.Page, null, routeEnum.MoveNext() ? routeEnum.Current : null);

            if (htmlContent.Length < 1) // the first is the active one
            {
                htmlContent.Insert(0, $"<li class=\"is-active\"><a href=\"{path}\" aria-current=\"page\">{node.Title}</a></li>");
            }
            else
            {
                htmlContent.Insert(0, $"<li><a href=\"{path}\">{node.Title}</a></li>");
            }
            node = node.Parent;
        }

        htmlContent.Insert(0, "<ul>");
        htmlContent.Append("</ul>");

        nav.InnerHtml.AppendHtml(htmlContent.ToString());

        return nav;
    }
}

