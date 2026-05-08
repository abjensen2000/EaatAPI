
using EaatAPI.Models;
using System.Net.Http.Json;

public class Program
{
    private static HttpClient _httpClient = new HttpClient();
    private static Kunde _currentKunde;
    private static List<Kunde> alleKunder;
    public static async Task Main(string[] args)
    {

        while (alleKunder == null)
        {
            try
            {
                alleKunder = await _httpClient.GetFromJsonAsync<List<Kunde>>("http://localhost:5063/api/eaat/kunder");
            }
            catch
            {
                await Task.Delay(2000);
            }
        }
        while (true)
        {
            Console.WriteLine("Indtast brugernavn");
            string brugernavn = Console.ReadLine();
            _currentKunde = alleKunder.Find(i => i.Navn.Equals(brugernavn, StringComparison.OrdinalIgnoreCase));
            if (_currentKunde == null)
            {
                Console.WriteLine("Indtast adresse");
                string adresse = Console.ReadLine();
                _currentKunde = new Kunde(brugernavn, adresse);

                var response = await _httpClient.PostAsJsonAsync("http://localhost:5063/api/eaat/kunder", _currentKunde);

                if (response.IsSuccessStatusCode)
                {
                    var opdateretListe = await _httpClient.GetFromJsonAsync<List<Kunde>>("http://localhost:5063/api/eaat/kunder");
                    _currentKunde = opdateretListe.Find(i => i.Navn.Equals(brugernavn, StringComparison.OrdinalIgnoreCase));
                }
            }


            List<Restaurant> restauranter = await _httpClient.GetFromJsonAsync<List<Restaurant>>("http://localhost:5063/api/eaat/restauranter");

            foreach (Restaurant restaurant in restauranter)
            {
                Console.WriteLine(restaurant.Navn);
            }
            Console.WriteLine();
            Console.WriteLine("Indtast Restaurant");
            string restaurantTilBestilling = Console.ReadLine();

            Restaurant currentRestaurant = restauranter.Find(i => i.Navn.Equals(restaurantTilBestilling, StringComparison.OrdinalIgnoreCase));
            if (currentRestaurant == null)
            {
                Console.WriteLine("Forkert indtastning, prøv igen.");
            }
            else
            {
                Console.WriteLine("Indtast din bestilling");
                string bestilling = Console.ReadLine();
                Bestilling newBestilling = new Bestilling(bestilling, _currentKunde.Adresse, currentRestaurant.Adresse, _currentKunde.Id, currentRestaurant.Id);
                await _httpClient.PostAsJsonAsync("http://localhost:5063/api/eaat/bestillinger", newBestilling);
            }




        }
    }




}
