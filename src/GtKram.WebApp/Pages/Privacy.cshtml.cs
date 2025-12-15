using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages;

[AllowAnonymous]
public class PrivacyModel : PageModel
{
    public void OnGet()
    {
    }
}
