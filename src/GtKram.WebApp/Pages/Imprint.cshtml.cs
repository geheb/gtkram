using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages;

[AllowAnonymous]
public class ImprintModel : PageModel
{
    public void OnGet()
    {
    }
}
