namespace MyAmazonstore3.Models
{
    public class Categorie
    {
        public int CategorieId { get; set; }
        public string Nom { get; set; }

        // Relation : une catégorie contient plusieurs produits
        public List<Produit> Produits { get; set; } = new();
    }

}
