
using EaatAPI.Models;
using System.Net.Http.Json;

public class Program
{
    private static HttpClient _httpClient = new HttpClient();
    private static Kunde _currentKunde;
    public static async Task Main(string[] args)
    {
        while (true)
        {
            List<Kunde> alleKunder = await _httpClient.GetFromJsonAsync<List<Kunde>>("http://localhost:5063/api/eaat/kunder");
            Console.WriteLine("Indtast brugernavn");
            string brugernavn = Console.ReadLine();
            _currentKunde = alleKunder.Find(i => i.Name.Equals(brugernavn, StringComparison.OrdinalIgnoreCase));
            if (_currentKunde == null)
            {
                Console.WriteLine("Indtast adresse");
                string adresse = Console.ReadLine();
                _currentKunde = new Kunde(brugernavn, adresse);

                // 1. Send kunden til API
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5063/api/eaat/kunder", _currentKunde);

                // 2. VIGTIGT: Hent kunden igen for at få det ID, som databasen lige har genereret!
                if (response.IsSuccessStatusCode)
                {
                    var opdateretListe = await _httpClient.GetFromJsonAsync<List<Kunde>>("http://localhost:5063/api/eaat/kunder");
                    _currentKunde = opdateretListe.Find(i => i.Name.Equals(brugernavn, StringComparison.OrdinalIgnoreCase));
                }
            }


            List<Restaurant> restauranter = await _httpClient.GetFromJsonAsync<List<Restaurant>>("http://localhost:5063/api/eaat/restauranter");

            foreach (Restaurant restaurant in restauranter)
            {
                Console.WriteLine(restaurant.Name);
            }
            Console.WriteLine();
            Console.WriteLine("Indtast Restaurant");
            string restaurantTilBestilling = Console.ReadLine();

            Restaurant currentRestaurant = restauranter.Find(i => i.Name.Equals(restaurantTilBestilling, StringComparison.OrdinalIgnoreCase));
            if (currentRestaurant == null)
            {
                Console.WriteLine("Forkert indtastning, prøv igen.");
            }
            else
            {
                Console.WriteLine("Indtast din bestilling");
                string bestilling = Console.ReadLine();
                Bestilling newBestilling = new Bestilling(bestilling, _currentKunde.Adresse, currentRestaurant.Adresse, _currentKunde.Id);
                await _httpClient.PostAsJsonAsync("http://localhost:5063/api/eaat/bestillinger", newBestilling);
            }




        }
    }




}
