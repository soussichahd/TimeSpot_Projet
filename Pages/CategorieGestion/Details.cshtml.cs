using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyAmazonstore3.Data;
using MyAmazonstore3.Models;

namespace MyAmazonstore3.Pages.CategorieGestion
{
    public class DetailsModel : PageModel
    {
        private readonly MyAmazonstore3.Data.MyAmazonstore3Context _context;

        public DetailsModel(MyAmazonstore3.Data.MyAmazonstore3Context context)
        {
            _context = context;
        }

        public Categorie Categorie { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categorie = await _context.Categorie.FirstOrDefaultAsync(m => m.CategorieId == id);
            if (categorie == null)
            {
                return NotFound();
            }
            else
            {
                Categorie = categorie;
            }
            return Page();
        }
    }
}
