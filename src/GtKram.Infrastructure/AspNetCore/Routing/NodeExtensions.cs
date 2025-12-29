namespace GtKram.Infrastructure.AspNetCore.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

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
}
