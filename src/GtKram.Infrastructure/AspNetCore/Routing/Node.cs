namespace GtKram.Infrastructure.AspNetCore.Routing;

using System.Diagnostics;

[DebuggerDisplay("{Page} - {Title}")]
public sealed class Node
{
    public Type Key { get; internal set; }
    public Type? FromKey { get; internal set; }
    public string Page { get; internal set; }
    public bool IsDefault { get; internal set; }
    public Node? Parent { get; internal set; }
    public string Title { get; internal set; }
    public string[]? AllowedRoles { get; internal set; }
    public string? AllowedPolicy { get; internal set; }

    public Node(Type key, string page, bool isDefault, string title)
    {
        Key = key;
        Page = page;
        IsDefault = isDefault;
        Title = title;
    }
}
