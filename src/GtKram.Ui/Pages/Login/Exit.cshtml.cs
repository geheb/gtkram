using GtKram.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Login;

[Authorize]
public class ExitModel : PageModel
{
    private readonly EmailAuth _emailAuth;

    public ExitModel(EmailAuth emailAuth) => _emailAuth = emailAuth;

    public async Task<IActionResult> OnGetAsync()
    {
        await _emailAuth.SignOut(User);

        return LocalRedirect("/");
    }
}
