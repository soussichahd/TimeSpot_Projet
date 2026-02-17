
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyAmazonstore3.Models
{      public class Produit
        {
        public int ProduitId { get; set; }
        public string Nom { get; set; }
       
        public decimal Prix { get; set; }
        public string Description { get; set; }

        public int QtStock { get; set; }
        public string ImageUrl { get; set; }
        // Foreign Key vers Categorie un produit appartient a une categorie 
        public int CategorieId { get; set; }
        public Categorie Categorie { get; set; }


    }
    

}
