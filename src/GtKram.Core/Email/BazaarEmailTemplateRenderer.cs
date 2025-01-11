using GtKram.Core.Models.Bazaar;
using System.Reflection;

namespace GtKram.Core.Email;

public class BazaarEmailTemplateRenderer
{
    private readonly TemplateRenderer _templateRenderer;

    public BazaarEmailTemplateRenderer()
    {
        _templateRenderer = new TemplateRenderer(GetType().GetTypeInfo().Assembly);
    }

    public Task<string> Render(BazaarEmailTemplate emailTemplate, object model)
    {
        var templateFile = GetTemplateFile(emailTemplate);
        return _templateRenderer.Render(templateFile, model);
    }

    private static string GetTemplateFile(BazaarEmailTemplate emailTemplate)
    {
        return emailTemplate switch
        {
            BazaarEmailTemplate.AcceptSeller => "AcceptSeller.html",
            BazaarEmailTemplate.DenySeller => "DenySeller.html",
            _ => throw new NotImplementedException($"unknown {nameof(BazaarEmailTemplate)} {emailTemplate}")
        };
    }
}
