using System.Reflection;

namespace GtKram.Application.Options;

public sealed class AppSettings
{
    public required string PublicUrl { get; set; }
    public string Version { get; }
    public string? Slogan { get; set; }
    public required string Title { get; set; }
    public required string HeaderTitle { get; set; }
    public required string Organizer { get; set; }
    public string? DefaultEventLocation { get; set; }
    public required string RegisterRulesUrl { get; set; }

    public AppSettings()
    {
        Version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    }
}
