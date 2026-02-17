using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MyAmazonstore3.Pages.User
{
    public class registerModel : PageModel
    {
        private readonly MyAmazonstore3Context _context;
        
        public registerModel(MyAmazonstore3Context context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string Nom { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }

            public string Tel { get; set; }

            [Required]
            public string Role { get; set; }
        }

        public void OnGet()
        {
            // rien à faire pour GET
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Vérifier si l'email existe déjà
            var existingUser = _context.User.FirstOrDefault(u => u.Email == Input.Email);
            if (existingUser != null)
            {
                ErrorMessage = "Cet email est déjà utilisé.";
                return Page();
            }

            // Créer un nouvel utilisateur
            var user = new Models.User
            {
                Nom = Input.Nom,
                Email = Input.Email,
                Password = Input.Password, // pour plus de sécurité, on peut hasher le mot de passe
                tel = Input.Tel,
                Role = Input.Role
            };

            _context.User.Add(user);
            _context.SaveChanges();

            // Redirection après inscription réussie
            return RedirectToPage("/User/auth"); // redirige vers la page de connexion
        }
    }
}
