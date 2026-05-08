namespace EaatAPI.Models
{
    public class Kunde
    {
        public Kunde() { }
        public Kunde(string name, string adresse)
        {
            Name = name;
            Adresse = adresse;
        }


        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public int? BestillingId { get; set; }


    }
}