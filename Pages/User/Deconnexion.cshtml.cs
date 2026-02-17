using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyAmazonstore3.Pages.User
{
    public class DeconnexionModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Supprimer le cookie utilisateur
            if (Request.Cookies.ContainsKey("UserCookie"))
            {
                Response.Cookies.Delete("UserCookie");
            }

            // Rediriger directement vers la page d'accueil
            return RedirectToPage("/Acceuil/Acceuil");
        }
    }
}
