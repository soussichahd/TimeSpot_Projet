using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;

namespace MyAmazonstore3.Pages.User
{
    public class authModel : PageModel
    {
        private readonly MyAmazonstore3Context _context;
        private readonly IMemoryCache _cache; // attribut cache 

        public authModel(MyAmazonstore3Context context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache; //injection du cache
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }
        }

        public void OnGet()
        {
         
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            //on vas creer la cle de cache baser sur l email
            string cacheKey = $"user_login_{Input.Email.ToLower()}";

            // si la cle existe on vas recuperer la ligne ds user sinon on vas chercher l utilisateur et l enregisterer ds le cache 
            if (!_cache.TryGetValue(cacheKey, out Models.User user))
            {  
                user = _context.User
                    .FirstOrDefault(u => u.Email == Input.Email && u.Password == Input.Password);

                if (user != null)
                {
                    //stockage de l'utilisateur dans le cache pour 20 minutes
                    _cache.Set(cacheKey, user, TimeSpan.FromMinutes(20));
                }
            }

            if (user != null)
            {
                //creer un objet user pour stocker les information du user dans le cookie
                var userInfo = new
                {
                    user.UserId,
                    user.Nom,
                    user.Email,
                    user.Role
                };

                string userJson = JsonSerializer.Serialize(userInfo);

                // Cookie utilisateur
                Response.Cookies.Append("UserCookie", userJson, new CookieOptions
                {
                    HttpOnly = true,               // sécurité : inaccessible en JS
                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                });

                return RedirectToPage("/Acceuil/Acceuil");
            }
            else
            {
                ErrorMessage = "Email ou mot de passe incorrect.";
                return Page();
            }
        }
    }
}
