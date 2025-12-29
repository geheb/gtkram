namespace GtKram.Infrastructure.AspNetCore.Routing;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

        var currentNode = breadcrumbGenerator.GetNode(actionDescriptor.HandlerTypeInfo);
        var routeEnum = routeValues.GetEnumerator();
        var nodes = new List<(Node, string?)>();

        while (currentNode != null)
        {
            var path = linkGenerator.GetPathByPage(currentNode.Page, null, routeEnum.MoveNext() ? routeEnum.Current : null);
            nodes.Add((currentNode, path));
            currentNode = currentNode.Parent;
        }

        nodes.Reverse();
        var htmlContent = new StringBuilder("<ul>");

        for (var i = 0; i < nodes.Count; i++)
        {
            var (node, path) = nodes[i];

            if (nodes.Count > 2 && i > 0 && i < nodes.Count - 1)
            {
                htmlContent.Append($"<li><a href=\"{path}\">");
                htmlContent.Append($"<span class=\"is-hidden-mobile\">{node.Title}</span>");
                htmlContent.Append($"<span class=\"is-hidden-tablet\">...</span>");
                htmlContent.Append("</a></li>");
            }
            else
            {
                if (i == nodes.Count - 1)
                {
                    htmlContent.Append("<li class=\"is-active\">");
                    htmlContent.Append($"<a href=\"{path}\" aria-current=\"page\">{node.Title}</a>");
                    htmlContent.Append("</li>");
                }
                else
                {
                    htmlContent.Append($"<li><a href=\"{path}\">{node.Title}</a></li>");
                }
            }
        }
        htmlContent.Append("</ul>");

        nav.InnerHtml.AppendHtml(htmlContent.ToString());

        return nav;
    }
}

