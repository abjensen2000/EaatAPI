using EaatAPI.Models;
using RestaurantGUI;
using System.Net.Http.Json;

public class Program {

    private static HttpClient _httpClient = new HttpClient();
    private static Restaurant _currentRestaurant;
    private static ModtagKundebestillingService _modtagKundebestillingService = new ModtagKundebestillingService();
    //private static BestillingAccepteretService _bestillingAccepteretService = new BestillingAccepteretService();
    private static List<Restaurant> _alleRestauranter;
    private static bool _redrawRequested = false;
    public static async Task Main(string[] args)
    {
        while (_alleRestauranter == null)
        {
            try
            {
                _alleRestauranter = await _httpClient.GetFromJsonAsync<List<Restaurant>>("http://localhost:5063/api/eaat/restauranter");
            }
            catch
            {
                await Task.Delay(2000);
            }
        }

        while (_currentRestaurant == null)
        {
            Console.WriteLine("Indtast restaurantnavn (eller tryk ctrl+c for at lukke):");
            string input = Console.ReadLine();                  

            _currentRestaurant = _alleRestauranter.Find(i => string.Equals(i.Navn, input, StringComparison.OrdinalIgnoreCase));

            if (_currentRestaurant == null)
            {
                Console.WriteLine($"Kunne ikke finde en restaurant med navnet '{input}'. Prøv igen.\n");
            }
        }

        ModtagKundebestillingService.RestaurantId = _currentRestaurant.Id;
        await _modtagKundebestillingService.StartAsync(new CancellationToken());

        var eksisterendeBestillinger = await _httpClient.GetFromJsonAsync<List<Bestilling>>($"http://localhost:5063/api/eaat/bestillinger/restaurant/{_currentRestaurant.Id}");
        if (eksisterendeBestillinger != null)
        {
            foreach (var bestilling in eksisterendeBestillinger)
            {
                if ((bestilling.BudId == 0 || bestilling.BudId == null) && 
                    bestilling.RestaurantId == _currentRestaurant.Id && 
                    !ModtagKundebestillingService.Bestillinger.ContainsKey(bestilling.Id))
                {
                    ModtagKundebestillingService.Bestillinger.TryAdd(bestilling.Id, bestilling);
                }
            }
        }

        ModtagKundebestillingService.OnMessageReceived = () =>
        {
            _redrawRequested = true;
        };

        await BestillingsLoop();
    }

    private static async Task BestillingsLoop()
    {
        _redrawRequested = true;
        string currentInput = "";

        while (true)
        {
            if (_redrawRequested)
            {
                PrintSkærm(currentInput);
                _redrawRequested = false;
            }

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    if (int.TryParse(currentInput, out int valgtId))
                    {
                        var fundetBestilling = ModtagKundebestillingService.Bestillinger.Values.FirstOrDefault(i => i.Id == valgtId);

                        if (fundetBestilling != null)
                        {
                            // Kald API'et via HTTP PUT!
                            var response = await _httpClient.PutAsync($"http://localhost:5063/api/eaat/bestillinger/{valgtId}/accepter", null);

                            if (response.IsSuccessStatusCode)
                            {
                                ModtagKundebestillingService.Bestillinger.TryRemove(valgtId, out _);
                                Console.WriteLine("\nBestilling accepteret! Den er nu sendt til budene.");
                            }
                            else
                            {
                                Console.WriteLine("\nKunne ikke acceptere bestilling i API'et.");
                            }

                            await Task.Delay(1000);
                        }
                    }
                    currentInput = "";
                    _redrawRequested = true;
                }
                else if (key.Key == ConsoleKey.Backspace && currentInput.Length > 0)
                {
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    _redrawRequested = true;
                }
                else if (char.IsDigit(key.KeyChar))
                {
                    currentInput += key.KeyChar;
                    _redrawRequested = true;
                }
            }
            else
            {
                await Task.Delay(50);
            }
        }
    }

    private static void PrintSkærm(string inputSoFar)
    {
        Console.Clear();
        Console.WriteLine($"Logget ind som: {_currentRestaurant.Id}");
        Console.WriteLine("-----------------------------------------");

        var alleBestillinger = ModtagKundebestillingService.Bestillinger.Values.ToList();
        if (alleBestillinger.Count == 0)
        {
            Console.WriteLine("Ingen ledige bestillinger lige nu...");
        }
        else
        {
            foreach (Bestilling bestilling in alleBestillinger)
            {
                Console.WriteLine($"BestillingsID: {bestilling.Id}: {bestilling.Beskrivelse} fra {bestilling.FraAdresse} til {bestilling.TilAdresse}");
            }
        }
        Console.WriteLine("-----------------------------------------");
        Console.Write($"Indtast ID på bestilling du vil acceptere: ");
        Console.Write(inputSoFar);
    }
}
