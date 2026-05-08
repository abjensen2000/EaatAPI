namespace EaatAPI.Models
{
    public class Restaurant
    {
        public Restaurant() { }

        public Restaurant(string navn, string adresse)
        {
            Navn = navn;
            Adresse = adresse;
        }
        public int Id { get; set; }
        public string Navn { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
    }
}