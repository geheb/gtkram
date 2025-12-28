namespace GtKram.Infrastructure.AspNetCore.Routing;

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing.Template;

public sealed class NodeGeneratorService
{
    private static bool IsRazorPage(Type type) => type != null && typeof(PageModel).IsAssignableFrom(type);
    private readonly Dictionary<Type, Node> _nodes = new();
    private readonly IActionSelector _actionSelector;
    private readonly CompiledPageActionDescriptor[] _actionDescriptors;

    public NodeGeneratorService(
        IActionSelector actionSelector,
        IActionDescriptorCollectionProvider descriptorCollectionProvider)
    {
        _actionSelector = actionSelector;
        _actionDescriptors = descriptorCollectionProvider.ActionDescriptors.Items.OfType<CompiledPageActionDescriptor>().ToArray();
    }

    public Node? Find(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = returnUrl;
        httpContext.Request.Method = HttpMethods.Get;
        var actionCandidates = _actionDescriptors.Where(a =>
            !string.IsNullOrEmpty(a.AttributeRouteInfo?.Template) &&
            TryMatch(a.AttributeRouteInfo.Template, returnUrl))
            .ToArray();
        try
        {
            var descriptor = _actionSelector.SelectBestCandidate(new(httpContext), actionCandidates);
            if (descriptor is not CompiledPageActionDescriptor pageActionDescriptor)
            {
                return null;
            }
            return _nodes.TryGetValue(pageActionDescriptor.ModelTypeInfo ?? pageActionDescriptor.HandlerTypeInfo, out var node) ? node : null;
        }
        catch (AmbiguousActionException)
        {
            return null;
        }
    }

    public Node GetNode<T>() where T : PageModel => GetNode(typeof(T));

    public Node GetNode(Type pageType)
    {
        if (!IsRazorPage(pageType))
        {
            throw new InvalidOperationException($"'{pageType.FullName}' isn't a Razor page");
        }

        var node = _nodes[pageType];
        return node;
    }

    public void AddNodes()
    {
        foreach (var actionDescriptor in _actionDescriptors)
        {
            var pageType = actionDescriptor.HandlerTypeInfo;
            if (_nodes.ContainsKey(pageType))
            {
                continue;
            }

            if (pageType.GetCustomAttribute<NodeAttribute>() is not NodeAttribute nodeAttribute)
            {
                continue;
            }

            var pagePath = actionDescriptor.ViewEnginePath;
            var auth = pageType.GetCustomAttribute<AuthorizeAttribute>();

            var node = CreateBreadcrumbNode(pageType, nodeAttribute, pagePath, auth);
            _nodes.Add(node.Key, node);
        }

        GenerateHierarchy(_nodes);
    }

    private static bool TryMatch(string routeTemplate, string requestPath)
    {
        var template = TemplateParser.Parse(routeTemplate);
        var matcher = new TemplateMatcher(template, new(template.ToRoutePattern().Defaults));
        return matcher.TryMatch(requestPath, new());
    }

    private static void GenerateHierarchy(Dictionary<Type, Node> entries)
    {
        if (entries.Count < 1)
        {
            return;
        }

        var defaultEntry = entries.Values.Single(e => e.IsDefault);
        var defaultNodeKey = defaultEntry.Key;

        foreach (var entry in entries.Values)
        {
            if (entry.IsDefault)
            {
                continue;
            }

            var fromKey = entry.FromKey ?? defaultNodeKey;
            if (!entries.TryGetValue(fromKey, out var node))
            {
                throw new InvalidOperationException($"No node exists that has a '{fromKey.FullName}' as a key. Make sure that Razor page has a {nameof(NodeAttribute)}");
            }

            entry.Parent = node;
        }
    }

    private static Node CreateBreadcrumbNode(Type pageType, NodeAttribute node, string pagePath, AuthorizeAttribute? auth)
    {
        if (node.FromPage is not null && !IsRazorPage(node.FromPage))
        {
            throw new InvalidOperationException($"FromPage '{node.FromPage.FullName}' isn't a Razor Page");
        }

        return new Node(pageType, pagePath, node.IsDefault, node.Title)
        {
            FromKey = node.FromPage,
            AllowedRoles = auth?.Roles?.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToArray(),
            AllowedPolicy = auth?.Policy
        };
    }
}
