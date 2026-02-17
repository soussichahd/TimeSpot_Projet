using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyAmazonstore3.Pages.Services
{
    public class LLMService
    {
        private readonly HttpClient _http;//client HTTP pour appeler l’API Groq

        private readonly string _apiKey;//key groq

        public LLMService(IConfiguration configuration)//injection de la configuration pour récupérer la clé API
        {
            _http = new HttpClient();
            _apiKey = configuration["Groq:ApiKey"]
                ?? throw new Exception("Clé API Groq manquante");
        }
        //la fonction qui recoit le prompt envoyer par le ragservice 
        public string Ask(string prompt)
        {
            Console.WriteLine(prompt);

            //construction de corps de la requette
            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "system", content = "Tu es un assistant e-commerce. Réponds uniquement avec le contexte fourni." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var json = JsonSerializer.Serialize(requestBody);//transformer le requette a un format json

            //creation de requette http
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions"
            );
            //modifier le header de la requette par l autorisation
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
            //indique qu on attend une reponse en json(type de retour)
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            //on mis le content avec le json
            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var sw = System.Diagnostics.Stopwatch.StartNew(); // ⏱ Démarrer le chronomètre


            var response = _http.Send(request);//envoyer la requette

            var responseText = response.Content.ReadAsStringAsync().Result;

            //gestion des erreur de reponse
            if (!response.IsSuccessStatusCode)
            {
                return $"Erreur Groq {((int)response.StatusCode)} : {responseText}";
            }
            sw.Stop(); //on stop 
            using var doc = JsonDocument.Parse(responseText);//parsing de json  reponse
            Debug.WriteLine($"⏱ Temps de réponse Groq : {sw.ElapsedMilliseconds} ms");

            return doc
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? "Réponse vide";
        }
    }
}