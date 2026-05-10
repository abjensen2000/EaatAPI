namespace Global.Models
{
    public class Bestilling
    {

        public Bestilling(string beskrivelse, string tilAdresse, string fraAdresse, int kundeId, int restaurantId)
        {
            Beskrivelse = beskrivelse;
            TilAdresse = tilAdresse;
            FraAdresse = fraAdresse;
            KundeId = kundeId;
            RestaurantId = restaurantId;
        }
        public Bestilling() { }

        public int Id { get; set; }
        public string FraAdresse { get; set; } = string.Empty;
        public string TilAdresse { get; set; } = string.Empty;
        public string Beskrivelse { get; set; } = string.Empty;
        public int RestaurantId { get; set; }
        public bool AccepteretAfRestaurant { get; set; }
        public int BudId { get; set; }
        public int KundeId { get; set; }


    }
}
