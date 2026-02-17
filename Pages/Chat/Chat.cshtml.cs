using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyAmazonstore3.Pages.Services;

namespace MyAmazonstore3.Pages
{
    public class ChatModel : PageModel
    {
        private readonly RagService _ragService;

        [BindProperty]
        public string UserMessage { get; set; }

        public string BotResponse { get; set; }

        public ChatModel(RagService ragService)
        {
            _ragService = ragService;
        }

        public void OnGet(int? ClearChat)
        {
            if (ClearChat == 1)
            {
                UserMessage = string.Empty;
                BotResponse = string.Empty;
            }
        }

        // Méthode Post asynchrone
        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(UserMessage))
            {
                BotResponse = await _ragService.ProcessAsync(UserMessage);
                BotResponse = BotResponse.Replace("-", ".\n");
            }

            return Page();
        }
    }
}
