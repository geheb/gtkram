using GtKram.WebApp.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages;

[Node("Startseite", IsDefault = true)]
[AllowAnonymous]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
