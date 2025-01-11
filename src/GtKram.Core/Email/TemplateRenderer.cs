namespace GtKram.Core.Email;

using System.Reflection;

internal sealed class TemplateRenderer
{
    private readonly Assembly _assembly;
    private readonly string[] _assemblyResourceNames;

    public TemplateRenderer(Assembly assembly)
    {
        _assembly = assembly;
        _assemblyResourceNames = _assembly.GetManifestResourceNames();
    }

    public async Task<string> Render(string templateFile, object model)
    {
        var name = _assemblyResourceNames.First(n => n.EndsWith(templateFile, StringComparison.OrdinalIgnoreCase));

        var stream = _assembly.GetManifestResourceStream(name);
        if (stream == null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream);

        var content = await reader.ReadToEndAsync();

        var template = Scriban.Template.Parse(content);
        if (template.HasErrors)
        {
            throw new InvalidOperationException("Parse template failed : " + string.Join(",", template.Messages.Select(m => m.Message)));
        }

        return await template.RenderAsync(model);
    }
}
