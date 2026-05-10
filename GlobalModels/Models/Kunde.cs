namespace Global.Models
{
    public class Kunde
    {
        public Kunde() { }
        public Kunde(string navn, string adresse)
        {
            Navn = navn;
            Adresse = adresse;
        }


        public int Id { get; set; }
        public string Navn { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public int? BestillingId { get; set; }


    }
}