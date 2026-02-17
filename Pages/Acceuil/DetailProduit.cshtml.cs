using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyAmazonstore3.Pages.Acceuil
{
    public class DetailProduitModel : PageModel
    {
        private readonly MyAmazonstore3Context _context;
        private readonly IMemoryCache _cache;
        public DetailProduitModel(MyAmazonstore3Context context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public Produit Produit { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            string cacheKey = $"produit_{id}";
             //Essayer de récupérer depuis le cache
            if (!_cache.TryGetValue(cacheKey, out Produit produit))//on cherche si la cle existe
            {    //ile produit information n existe pas ds le cache
                produit = await _context.Produit
                    .Include(p => p.Categorie)
                    .AsNoTracking() // Lecture seule = plus rapide
                    .FirstOrDefaultAsync(p => p.ProduitId == id);

                if (produit == null)
                {
                    return NotFound();
                }
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))  // Les détails produit changent rarement
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))   // Renouvelle si accédé
                    .SetPriority(CacheItemPriority.High);             // Priorité haute car souvent consulté

                _cache.Set(cacheKey, produit, cacheOptions);
            }

            Produit = produit;
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
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
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
            return RedirectToPage();
        }
    }
}
