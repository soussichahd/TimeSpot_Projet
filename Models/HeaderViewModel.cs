using System.Collections.Generic;

namespace MyAmazonstore3.Models
{
    public class HeaderViewModel
    {
        public List<Categorie> Categories { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string Search { get; set; }//le nom utiliser pour chercher le produit
    }
}
