namespace GtKram.Ui.Routing;

using Microsoft.AspNetCore.Mvc.RazorPages;

public static class NodeExtensions
{
    public static void UseNodeGenerator(this IApplicationBuilder app)
    {
        var nodeGenerator = app.ApplicationServices.GetRequiredService<NodeGeneratorService>();
        nodeGenerator.AddNodes();
    }

    public static Node GetNode(this PageModel model)
    {
        var breadcrumbGenerator = model.HttpContext.RequestServices.GetRequiredService<NodeGeneratorService>();
        return breadcrumbGenerator.GetNode(model.GetType());
    }

    public static Node GetNode<T>(this PageModel model) where T : PageModel
    {
        var breadcrumbGenerator = model.HttpContext.RequestServices.GetRequiredService<NodeGeneratorService>();
        return breadcrumbGenerator.GetNode<T>();
    }

    public static bool HasNodeAccess(this PageModel model, params Type[] pageModels)
    {
        if (!model.User.Identity?.IsAuthenticated ?? false)
        {
            return false;
        }

        var breadcrumbGenerator = model.HttpContext.RequestServices.GetRequiredService<NodeGeneratorService>();

        var nodes = pageModels.Select(breadcrumbGenerator.GetNode).ToArray();
        foreach (var node in nodes)
        {
            var hasRole =
                node.AllowedRoles == null ||
                node.AllowedRoles.Length == 0 ||
                node.AllowedRoles.Any(model.User.IsInRole);

            if (hasRole)
            {
                return true;
            }
        }

        return nodes.Length < 1;
    }

    public static void BuildHeaderMenu(this PageModel model, object? routeValues = null)
    {
        if (model.User.Identity == null ||
            !model.User.Identity.IsAuthenticated)
        {
            return;
        }

        var breadcrumbGenerator = model.HttpContext.RequestServices.GetRequiredService<NodeGeneratorService>();
        var linkGenerator = model.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

        var node = breadcrumbGenerator.GetNode(model.GetType());

        var path = linkGenerator.GetPathByPage(node.Page, null, routeValues);
        if (path == null)
        {
            return;
        }

        var menu = new MenuItem(node.Title, path);

        model.ViewData["HeaderMenu"] = menu;
    }
}
