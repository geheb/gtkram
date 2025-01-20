using GtKram.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Login;

[Authorize]
public class ExitModel : PageModel
{
    private readonly IEmailAuth _emailAuth;

    public ExitModel(IEmailAuth emailAuth) => _emailAuth = emailAuth;

    public async Task<IActionResult> OnGetAsync()
    {
        await _emailAuth.SignOut(User);

        return LocalRedirect("/");
    }
}
