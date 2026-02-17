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
    public class IndexModel : PageModel
    {
        private readonly MyAmazonstore3.Data.MyAmazonstore3Context _context;

        public IndexModel(MyAmazonstore3.Data.MyAmazonstore3Context context)
        {
            _context = context;
        }

        public IList<Categorie> Categorie { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Categorie = await _context.Categorie.ToListAsync();
        }
    }
}
