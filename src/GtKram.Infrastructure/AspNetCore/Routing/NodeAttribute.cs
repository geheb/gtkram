namespace GtKram.Infrastructure.AspNetCore.Routing;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NodeAttribute : Attribute
{
    /// <summary>
    /// Title of the page to display
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Whether this is the default breadcrumb or not
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// The type of the page this breadcrumb item comes from.
    /// </summary>
    public Type? FromPage { get; set; }

    public NodeAttribute(string title)
    {
        Title = title;
    }
}
