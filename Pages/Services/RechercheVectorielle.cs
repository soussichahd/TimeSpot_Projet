using MyAmazonstore3.Data;
using MyAmazonstore3.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
namespace MyAmazonstore3.Pages.Services
{
    public class RechercheVectorielle
    {
        private readonly MyAmazonstore3Context _context;
        private readonly Vectorizer _vectorizer;
        private readonly IMemoryCache _cache;//pour stocker les produit embedding en memoire

        public RechercheVectorielle( MyAmazonstore3Context db, Vectorizer vectorizer,IMemoryCache cache)
        {
            _context = db;
            _vectorizer = vectorizer;
            _cache = cache;
        }
        
        //Faire construire le cache key depuis l id est nom+desc(hasher) pour obtimiser la memoire cahce
        //on a ajouter le nom et la description a cache key car si on  change la descption ou le nom on doit changer la cle
        private string GetCacheKey(int produitId, string nom, string desc)
        {
            // 1️ Normalisation (évite faux changements tabulation , retoure ligne ....)
            var texte = $"{nom} {desc}"
                .Trim()
                .ToLowerInvariant()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");

            //Hash SHA-256 pour g=hasher nom+desc 
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(texte);
            byte[] hash = sha256.ComputeHash(bytes);

            // retourner Clé de cache finale
            return $"produit_embedding_{produitId}_{Convert.ToBase64String(hash)}";
        }

        /*********** Métriques de similarité/distance ***********************/

        //simulation similarité cosinus

        private float CosineSimilarity(float[] v1, float[] v2)
        {
           

            float dot = 0f, mag1 = 0f, mag2 = 0f;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }

            float result = dot / (MathF.Sqrt(mag1) * MathF.Sqrt(mag2));
            return float.IsNaN(result) ? 0f : result;
        }

        // Distance euclidienne
        private float EuclideanDistance(float[] v1, float[] v2)
        {
            float sum = 0f;
            for (int i = 0; i < v1.Length; i++)
                sum += MathF.Pow(v1[i] - v2[i], 2);

            return MathF.Sqrt(sum);
        }
       
        // Produit scalaire
        private float DotProduct(float[] v1, float[] v2)
        {
            float dot = 0f;
            for (int i = 0; i < v1.Length; i++)
                dot += v1[i] * v2[i];

            return dot;
        }



        /**********************************************************/
        /*GetOrCreateAsync cherche dans le cache si key existe

        Si oui → retourne directement l’objet stocké

        Si non → appelle la fonction factory pour créer l’objet,
        le stocke dans le cache, puis le retourne
        */
            public async Task<List<Produit>> Search(float[] questionEmbedding)
            {
                

                var swTotalRecherche = Stopwatch.StartNew();
                var swTotalEmbeddingProduits = 0L;

                var produits = _context.Produit.ToList();
                var resultatsAvecScore = new List<(Produit Produit, float Score)>();

                foreach (var p in produits)
                {
                    Debug.WriteLine($"--- Produit : {p.Nom} ---");

                    string cacheKey = GetCacheKey(p.ProduitId, p.Nom, p.Description);//on a utiliser l id ds la cle de cache pour garder l unicite 

                    var swEmbeddingProduit = Stopwatch.StartNew();

                // recherche ds le cache is la cle existe on prendre ds le cache sinon on calcule l embedding et on le stocke dans le cache
                float[] produitVector = await _cache.GetOrCreateAsync(
                        cacheKey,
                        async entry =>
                        {
                            entry.SlidingExpiration = TimeSpan.FromHours(6);
                            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

                            Debug.WriteLine("❌ Embedding NON trouvé dans le cache → Calcul en cours");

                            return await _vectorizer.GetEmbeddingAsync(
                                p.Nom + " " + p.Description
                            );
                        });

                    swEmbeddingProduit.Stop();
                    swTotalEmbeddingProduits += swEmbeddingProduit.ElapsedMilliseconds;

                    Debug.WriteLine("✅ Embedding récupéré depuis le cache");

                    // 🔢 Calcul de similarité
                    var swSimilarite = Stopwatch.StartNew();

                    float score = DotProduct(questionEmbedding, produitVector);

                    swSimilarite.Stop();

                    Debug.WriteLine($"⏱ Similarité : {swSimilarite.ElapsedTicks} ticks");
                    Debug.WriteLine($"Score : {score}");

                    resultatsAvecScore.Add((p, score));
                }

                swTotalRecherche.Stop();

                Debug.WriteLine("========= FIN RECHERCHE VECTORIELLE =========");
                Debug.WriteLine($"⏱ Temps TOTAL embeddings produits : {swTotalEmbeddingProduits} ms");
                Debug.WriteLine($"⏱ Temps TOTAL recherche : {swTotalRecherche.ElapsedMilliseconds} ms");

                return resultatsAvecScore
                    .OrderByDescending(x => x.Score)
                    .Take(6)
                    .Select(x => x.Produit)
                    .ToList();
            }

        
        /* fonction pour tester les performance du cache et l embedding pour une liste de 1000 produits*/
      /* public async Task Search2(float[] questionEmbedding)
        {
            // Produit factice pour tester le cache
            var produit = new Produit
            {
                ProduitId = 1,
                Nom = "Montre Test",
                Description = "Description du produit test"
            };

          

            int repetitions = 200;
            var swTotal = Stopwatch.StartNew();

            for (int i = 0; i < repetitions; i++)
            {
                Debug.WriteLine($"=== Boucle {i + 1} ===");
                produit.ProduitId = i + 1; // Changer l'ID pour simuler des produits différents
                string cacheKey = GetCacheKey(produit.ProduitId, produit.Nom, produit.Description);

                var swEmbeddingProduit = Stopwatch.StartNew();

                // ✅ UTILISER L'APPEL RÉEL AU VECTORIZER
                float[] produitVector = await _cache.GetOrCreateAsync(
                    cacheKey,
                    async entry =>
                    {
                        entry.SlidingExpiration = TimeSpan.FromHours(6);
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

                        Debug.WriteLine("❌ Embedding NON trouvé dans le cache → Calcul en cours");

                        // Ici, ton vrai calcul d'embedding
                        return await _vectorizer.GetEmbeddingAsync(
                            produit.Nom + " " + produit.Description
                        );
                    });

                swEmbeddingProduit.Stop();

                Debug.WriteLine($"⏱ Temps embedding boucle {i + 1} : {swEmbeddingProduit.ElapsedMilliseconds} ms");

                // Calcul de similarité
                float score = DotProduct(questionEmbedding, produitVector);
                Debug.WriteLine($"Score : {score}");
            }

            swTotal.Stop();
            Debug.WriteLine($"⏱ Temps total pour {repetitions} produits : {swTotal.ElapsedMilliseconds} ms");

        } 
      */

    }
}