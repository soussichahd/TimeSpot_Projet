using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyAmazonstore3.Pages
{
    public class AcceuilModel : PageModel
    {
        private readonly MyAmazonstore3Context _context;
        private readonly IMemoryCache _cache;
        public AcceuilModel(MyAmazonstore3Context context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public List<Produit> Produits { get; set; } = new List<Produit>();
        

        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        public string PanierId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            
            //Récupérer ou créer PanierId
            PanierId = Request.Cookies["PanierId"];
            if (string.IsNullOrEmpty(PanierId))
            {
                PanierId = Guid.NewGuid().ToString();
                Response.Cookies.Append("PanierId", PanierId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30)//duree de vie du cookie 30 jours
                });
            }

            //  Créer une clé de cache unique basée sur les filtres
            string searchNormalized = Search?.ToLower().Trim() ?? "";
            string cacheKey = $"produits_{SelectedCategoryId ?? 0}_{searchNormalized}";//key cache pour ce filtre 

            //Essayer de récupérer depuis le cache
            if (!_cache.TryGetValue(cacheKey, out List<Produit> produits))
            {//le TryGetValue test si la cle existe si  oui on affecte a la table produits le contenu de cle existante sinon on entre pour creer cette table et la stocker ds le cache 

                //Pas dans le cache → Requête vers la base de données
                IQueryable<Produit> query = _context.Produit
                    .Include(p => p.Categorie)
                    .AsNoTracking(); // Optimisation pour lecture seule

                // Filtrage par recherche texte
                if (!string.IsNullOrEmpty(Search))
                {
                    string searchLower = Search.ToLower();
                    query = query.Where(p => p.Nom.ToLower().Contains(searchLower));
                }

                // Filtrage par catégorie
                if (SelectedCategoryId.HasValue && SelectedCategoryId.Value != 0)
                {
                    query = query.Where(p => p.CategorieId == SelectedCategoryId.Value);
                }

                produits = await query.ToListAsync();


                _cache.Set(cacheKey, produits, TimeSpan.FromMinutes(20));//en l enregistre ds le cache avec la cle creer pendant 20min
            }

            Produits = produits;
            return Page();
        }

        public IActionResult OnPostAjouterAuPanier(int produitId, int quantite)
        {
            var panierId = Request.Cookies["PanierId"];
            if (string.IsNullOrEmpty(panierId))
            {
                panierId = Guid.NewGuid().ToString();
                Response.Cookies.Append("PanierId", panierId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30)//le panier s expire en 30jours
                });
            }

            var panierCookie = Request.Cookies["Panier"];
            var panier = new Dictionary<int, int>();
            if (!string.IsNullOrEmpty(panierCookie))
            {
                panier = JsonSerializer.Deserialize<Dictionary<int, int>>(panierCookie);
            }

            if (panier.ContainsKey(produitId))
                panier[produitId] += quantite;
            else
                panier[produitId] = quantite;

            Response.Cookies.Append("Panier", JsonSerializer.Serialize(panier),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            TempData["Message"] = "Produit ajouté au panier !";

            // Redirection en conservant le filtre catégorie et recherche
            return RedirectToPage(new { SelectedCategoryId = SelectedCategoryId, Search = Search });
        }
    }
}

