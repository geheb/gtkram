using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBillings;

[Node("Meine Kasse", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "billing,admin")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
