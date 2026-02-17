using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;

namespace MyAmazonstore3.Pages.Services
{
    public class RagService
    {
        private readonly Vectorizer _embedding;
        private readonly RechercheVectorielle _search;
        private readonly LLMService _llm;
        private readonly MyAmazonstore3Context _context;
        private readonly IHttpContextAccessor _http;

        // Panier 
        public Dictionary<int, int> Panier { get; set; } = new();
        //public Dictionary<Produit, int> PanierContenu { get; set; } = new();
        
        // Constructor
        public RagService(
            Vectorizer embedding,
            RechercheVectorielle search,
            LLMService llm,
            IHttpContextAccessor http, MyAmazonstore3Context cont)
        {
            _embedding = embedding;
            _search = search;
            _llm = llm;
            _http = http;
            _context = cont;
        }
        //pour afficher le prompt dans le debug
        private void LogPromptToDebug(string finalPrompt)
        {
            Debug.WriteLine("======================= PROMPT ENVOYÉ AU LLM =======================");
            Debug.WriteLine(finalPrompt);
            Debug.WriteLine("====================================================================");
        }
        // Méthode principale pour traiter une question utilisateur
        public async Task<string> ProcessAsync(string question)
        {
            var httpContext = _http.HttpContext;
            if (httpContext == null)
                return "Erreur : contexte HTTP indisponible.";

            // 1️⃣ Récupérer le panier depuis le cookie
            var panierCookie = httpContext.Request.Cookies["panier"];
            if (!string.IsNullOrEmpty(panierCookie))
            {
                try
                {
                    Panier = JsonSerializer.Deserialize<Dictionary<int, int>>(panierCookie)
                             ?? new Dictionary<int, int>();
                }
                catch
                {
                    Panier = new Dictionary<int, int>();
                }
            }

            // 2️⃣ Récupérer les produits
            var products = _context.Produit.ToList();
            if (products.Count == 0)
                return "Aucun produit trouvé en base de données.";

            if (products.Count > 10)
            {
                // Embedding de la question
                float[] qEmbedding = await _embedding.GetEmbeddingAsync(question);

                // Recherche vectorielle
                products = await _search.Search(qEmbedding);
               
            }

            var userInfo = httpContext.Request.Cookies["UserCookie"] ?? "Utilisateur inconnu";
            var categori_liste = _context.Categorie.ToList();

            // 3️⃣ Construire le contexte produit
            var context = "Produits trouvés :\n";
            foreach (var p in products)
            {
                var categorie = categori_liste.FirstOrDefault(c => c.CategorieId == p.CategorieId)?.Nom ?? "Inconnue";
                context += $"- ProduitId:{p.ProduitId} / Nom:{p.Nom} / Description:{p.Description} / Prix:{p.Prix} / Stock:{p.QtStock} / Catégorie:{categorie}\n";
            }

            context += "\nInformations utilisateur :\n";
            context += $"- User cookie : {userInfo}\n";

            // 4️⃣ Créer PanierProduit : liste avec toutes les infos
            var PanierProduit = new List<object>();
            foreach (var panierItem in Panier)
            {
                var produit = _context.Produit.FirstOrDefault(p => p.ProduitId == panierItem.Key);
                if (produit != null)
                {
                    var categorie = _context.Categorie.FirstOrDefault(c => c.CategorieId == produit.CategorieId)?.Nom ?? "Inconnue";
                    PanierProduit.Add(new
                    {
                        ProduitId = produit.ProduitId,
                        Nom = produit.Nom,
                        Description = produit.Description,
                        Prix = produit.Prix,
                        Stock = produit.QtStock,
                        Categorie = categorie,
                        Quantite = panierItem.Value
                    });
                }
            }

            // Ajouter le panier détaillé dans le contexte
            context += $"- Contenu du panier : {PanierProduit.Count} produit(s) :\n";
            foreach (var p in PanierProduit)
            {
                dynamic item = p;
                context += $"- Article : {item.Nom} - Quantité : {item.Quantite} - Prix : {item.Prix} - Stock : {item.Stock} - Catégorie : {item.Categorie}\n";
            }

            // Instructions spéciales pour le LLM
            var instructions = @"
RÈGLES DE FORMATAGE IMPORTANTES :

1. STRUCTURE DE LA RÉPONSE :
   - Commence toujours par un titre ou une introduction claire
   - Utilise des paragraphes séparés par des lignes vides (\n\n)
   - Termine par une conclusion ou question si pertinent

2. PRÉSENTATION DES PRODUITS :
   - Chaque produit doit être sur plusieurs lignes distinctes
   - Format recommandé :
     Nom du produit
     Description : [texte]
     Prix : [montant]
     Stock disponible : [quantité]
     Catégorie : [nom]
     [ligne vide]

3. PRÉSENTATION DU PANIER :
   - Liste chaque article du panier sur une ligne séparée
   - Format : • Article : [nom] - Quantité : [nombre]
   - Ajoute le total si demandé

4. FORMATAGE INTERDIT :
   - N'utilise JAMAIS ** ou * pour le gras ou italique
   - N'utilise JAMAIS # pour les titres
   - N'utilise JAMAIS des puces markdown (-, *)
   - Utilise plutôt : • ou - suivi d'un espace au début de ligne

5. ESPACEMENT :
   - Une ligne vide (\n\n) entre chaque section
   - Deux espaces avant chaque sous-information
   - Un retour à la ligne (\n) pour chaque information distincte

6. SÉCURITÉ ET CONFIDENTIALITÉ :
   - N'affiche JAMAIS les ID de produits aux clients
   - Ne mentionne JAMAIS les identifiants techniques
   - Utilise uniquement les noms de produits

7. COMPORTEMENT :
   - Tu es un assistant conseil UNIQUEMENT
   - Tu ne peux PAS modifier le panier
   - Tu peux seulement informer et recommander
";

            // 5️⃣ Prompt final
            var prompt = $"""
        Tu es un assistant e-commerce intelligent.

        Question utilisateur :
        {question}

        Contexte :
        {context}
        {instructions}

        Réponds de manière claire et utile.
    """;

            LogPromptToDebug(prompt);

            // 6️⃣ Appel LLM
            return _llm.Ask(prompt);
        }


    }
}
