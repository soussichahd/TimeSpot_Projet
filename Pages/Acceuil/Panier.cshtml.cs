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

namespace MyAmazonstore3.Pages.Acceuil
{
    public class PanierModel : PageModel
    {
        private readonly MyAmazonstore3Context _context;
        private readonly IMemoryCache _cache;
        public PanierModel(MyAmazonstore3Context context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public Dictionary<int, int> Panier { get; set; } = new Dictionary<int, int>(); // produitId -> quantité
        public List<Produit> ProduitsPanier { get; set; } = new List<Produit>();
        public decimal TotalPanier { get; set; }
        public IActionResult OnGet()
        {
            // Vérifier si le cookie PanierId existe
            if (!Request.Cookies.ContainsKey("Panier"))
            {
                // Panier vide, rediriger vers l'accueil
                return RedirectToPage("/Acceuil/Acceuil");
            }

            // Récupérer le panier depuis le cookie
            var panierCookie = Request.Cookies["Panier"];
            Panier = JsonSerializer.Deserialize<Dictionary<int, int>>(panierCookie);

            // 🔑 Créer une clé de cache basée sur les IDs des produits
            var produitIds = Panier.Keys.OrderBy(x => x).ToList(); // Trier les id pour toujour trouver le meme cle par exemple si 4_3_1 on trouve la meme 4_3_1 non pas 1_4_3
            string cacheKey = $"produits_panier_{string.Join("_", produitIds)}";

            //Essayer de récupérer depuis le cache
            if (!_cache.TryGetValue(cacheKey, out List<Produit> produitsPanier))
            {
                //Pas dans le cache → Requête vers la base de données
                produitsPanier = _context.Produit
                    .AsNoTracking() // Lecture seule
                    .Where(p => produitIds.Contains(p.ProduitId))
                    .ToList();

                //Mettre en cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))  // Les prix peuvent changer
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(cacheKey, produitsPanier, cacheOptions);
            }

            ProduitsPanier = produitsPanier;

            // Calculer le total
            TotalPanier = ProduitsPanier.Sum(p => p.Prix * Panier[p.ProduitId]);

            return Page();
        }
        public IActionResult OnPostModifierQuantite(int produitId, int quantite)
        {
            if (!Request.Cookies.ContainsKey("Panier"))
                return RedirectToPage("/Acceuil/Acceuil");

            var panier = JsonSerializer.Deserialize<Dictionary<int, int>>(Request.Cookies["Panier"]);

            if (panier.ContainsKey(produitId))
            {
                if (quantite > 0)
                    panier[produitId] = quantite;
                else
                    panier.Remove(produitId);
            }

            Response.Cookies.Append("Panier", JsonSerializer.Serialize(panier),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            return RedirectToPage();
        }

        public IActionResult OnPostSupprimerProduit(int produitId)
        {
            if (!Request.Cookies.ContainsKey("Panier"))
                return RedirectToPage("/Acceuil/Acceuil");

            var panier = JsonSerializer.Deserialize<Dictionary<int, int>>(Request.Cookies["Panier"]);

            if (panier.ContainsKey(produitId))
                panier.Remove(produitId);//on le supp 

            Response.Cookies.Append("Panier", JsonSerializer.Serialize(panier),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            return RedirectToPage();
        }
    }
}
