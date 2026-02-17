using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;

namespace MyAmazonstore3.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly MyAmazonstore3Context _context;

        public HeaderViewComponent(MyAmazonstore3Context context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // récupérer catégories
            var categories = await _context.Categorie.ToListAsync();

            //// récupérer paramètres GET
            int? selectedCategoryId = null;
            if (int.TryParse(HttpContext.Request.Query["SelectedCategoryId"], out int id))
                selectedCategoryId = id;

            var search = HttpContext.Request.Query["Search"].ToString();

            // envoyer uniquement ce qui existe déjà
            ViewData["Categories"] = categories;
            ViewData["SelectedCategoryId"] = selectedCategoryId;
            ViewData["Search"] = search;

            return View();
        }
    }
}
