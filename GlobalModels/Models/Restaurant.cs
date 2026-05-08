namespace EaatAPI.Models
{
    public class Restaurant
    {
        public Restaurant() { }

        public Restaurant(string name, string adresse)
        {
            Name = name;
            Adresse = adresse;
        }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
    }
}