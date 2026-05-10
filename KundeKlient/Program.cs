using Global.Models;
using Global.Services; // Husk denne for ForbindTilRabbitService
using KundeKlient;
using System.Net.Http.Json;

public class Program
{
    private static HttpClient _httpClient = new HttpClient();
    private static Kunde _currentKunde;
    private static List<Kunde> alleKunder;
    private static ForbindTilRabbitService _forbindTilRabbitService = new ForbindTilRabbitService();
    private static ModtagNotifikationService _modtagNotifikationService;

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

        // 1. LOG-IND LOOP (Sørger for vi er logget ind først)
        while (_currentKunde == null)
        {
            Console.WriteLine("Indtast brugernavn:");
            string brugernavn = Console.ReadLine();
            _currentKunde = alleKunder.Find(i => string.Equals(i.Navn, brugernavn, StringComparison.OrdinalIgnoreCase));

            if (_currentKunde == null)
            {
                Console.WriteLine("Indtast din adresse for at oprette dig som ny kunde:");
                string adresse = Console.ReadLine();
                _currentKunde = new Kunde(brugernavn, adresse);

                var response = await _httpClient.PostAsJsonAsync("http://localhost:5063/api/eaat/kunder", _currentKunde);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Du er nu oprettet!");
                    var opdateretListe = await _httpClient.GetFromJsonAsync<List<Kunde>>("http://localhost:5063/api/eaat/kunder");
                    _currentKunde = opdateretListe.Find(i => string.Equals(i.Navn, brugernavn, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    Console.WriteLine("Fejl ved oprettelse af kunde.");
                    _currentKunde = null; // Tvinger loopet forfra
                }
            }
        } // Log-ind færdig

        Console.Clear();
        Console.WriteLine($"Velkommen {_currentKunde.Navn}!");

        // 2. START RABBITMQ LYTTEREN FOR KUNDENS ID
        ModtagNotifikationService.KundeId = _currentKunde.Id;
        _modtagNotifikationService = new ModtagNotifikationService(_forbindTilRabbitService);
        await _modtagNotifikationService.StartAsync(new CancellationToken());

        // Når servicen fanger en besked der er for vores KundeId, skal konsollen vise det
        ModtagNotifikationService.OnNotificationReceived = (bestilling) =>
        {
            Console.WriteLine($"\nOpdatering på din bestilling '{bestilling.Beskrivelse}':");

            if (bestilling.AccepteretAfRestaurant && (bestilling.BudId == 0 || bestilling.BudId == null))
            {
                Console.WriteLine("-> Restauranten er i gang med at forberede den!");
            }
            else if (bestilling.BudId != 0 && bestilling.BudId != null)
            {
                Console.WriteLine($"-> Bud (ID={bestilling.BudId}) har nu overtaget ordren og er på vej!");
            }
        };

        // 3. BESTILLINGS-LOOP 
        while (true)
        {
            try
            {
                var restauranter = await _httpClient.GetFromJsonAsync<List<Restaurant>>("http://localhost:5063/api/eaat/restauranter");

                if (restauranter != null && restauranter.Any())
                {
                    Console.WriteLine("\n==== RESTAURANTER ====");
                    foreach (Restaurant restaurant in restauranter)
                    {
                        Console.WriteLine($"- {restaurant.Navn}");
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("Indtast navnet på den restaurant du vil bestille fra (eller tryk ctrl+c for at lukke):");
                    string restaurantTilBestilling = Console.ReadLine();

                    Restaurant currentRestaurant = restauranter.Find(i => string.Equals(i.Navn, restaurantTilBestilling, StringComparison.OrdinalIgnoreCase));

                    if (currentRestaurant == null)
                    {
                        Console.WriteLine("Forkert indtastning, prøv igen.");
                        continue;
                    }

                    Console.WriteLine($"Indtast din bestilling fra {currentRestaurant.Navn}:");
                    string bestillingTekst = Console.ReadLine();

                    Bestilling newBestilling = new Bestilling(bestillingTekst, _currentKunde.Adresse, currentRestaurant.Adresse, _currentKunde.Id, currentRestaurant.Id);

                    var postResponse = await _httpClient.PostAsJsonAsync("http://localhost:5063/api/eaat/bestillinger", newBestilling);
                    if (postResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("\nDin bestilling er sendt afsted. Vi giver dig besked, når restauranten accepterer.");
                    }
                    else
                    {
                        Console.WriteLine("\nAPI'et afviste ordren. Prøv igen senere.");
                    }
                }
            }
            catch (HttpRequestException)
            {
                //Bestilling går tabt og skal genoprettes. Det er måske fint nok
                Console.WriteLine("\nFEJL: Kunne ikke få forbindelse til API'et. Serveren er muligvis nede. Prøver igen om 5 sekunder...");
                await Task.Delay(5000);
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nSYSTEM FEJL: Noget gik galt: {ex.Message}");
            }
            await Task.Delay(2000);
        }
    }
}