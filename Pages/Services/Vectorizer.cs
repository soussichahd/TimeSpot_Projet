using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MyAmazonstore3.Pages.Services
{
    public class Vectorizer
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public Vectorizer(IConfiguration configuration)
        {
            _apiKey = configuration["NomicAtlas:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Erreur : la clé Nomic Atlas n'est pas définie !");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "nk-FwgWOvvegGicY0dMc2yMcwJwUSNS5T8J3cpaoTLxBaQ");
        }

      

public async Task<float[]> GetEmbeddingAsync(string text)
    {
            if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Le texte ne peut pas être vide");

        // 1. L'URL exacte pour l'embedding de texte chez Nomic
        string url = "https://api-atlas.nomic.ai/v1/embedding/text";

        // 2. Le corps de la requête avec le paramètre "model" obligatoire
        var requestBody = new
        {
            texts = new[] { text },
            model = "nomic-embed-text-v1.5", // Spécifier le modèle ici
            task_type = "search_query"       // Utilise "search_query" pour les questions utilisateur
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync();
                throw new Exception($"Nomic Error {response.StatusCode}: {errorDetail}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            // 3. Extraction corrigée selon la structure de réponse Nomic
            var embeddingArray = doc.RootElement
                .GetProperty("embeddings")
                .EnumerateArray()
                .First() // Premier texte envoyé
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();

            return embeddingArray;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur Nomic : {ex.Message}");
        }
    }
}
}
