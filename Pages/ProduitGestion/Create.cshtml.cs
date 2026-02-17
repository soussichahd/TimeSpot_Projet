using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using MyAmazonstore3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyAmazonstore.Pages.ProduitGestion
{
    public class CreateModel : PageModel
    {
        private readonly MyAmazonstore3.Data.MyAmazonstore3Context _context;

        public CreateModel(MyAmazonstore3.Data.MyAmazonstore3Context context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            // Utiliser Set<Categorie>() de manière cohérente
            ViewData["CategorieId"] = new SelectList(_context.Set<Categorie>(), "CategorieId", "Nom");
            return Page();
        }

        [BindProperty]
        public Produit Produit { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Retirer la propriété de navigation de la validation
            ModelState.Remove("Produit.Categorie");

            if (!ModelState.IsValid)
            {
                // Recharger la liste des catégories en cas d'erreur
                ViewData["CategorieId"] = new SelectList(_context.Set<Categorie>(), "CategorieId", "Nom");
                return Page();
            }

            // Chercher la catégorie par ID et l'attacher au produit
            var categorie = await _context.Set<Categorie>()
                .FindAsync(Produit.CategorieId);

            if (categorie == null)
            {
                ModelState.AddModelError("Produit.CategorieId", "La catégorie sélectionnée n'existe pas.");
                ViewData["CategorieId"] = new SelectList(_context.Set<Categorie>(), "CategorieId", "Nom");
                return Page();
            }

            // Attacher la catégorie au produit
            Produit.Categorie = categorie;

            _context.Produit.Add(Produit);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}