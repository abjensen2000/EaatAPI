using BudKlient;
using EaatAPI.Models;
using System.Net.Http.Json;
using System.Text;

public class Program
{
    private static HttpClient _httpClient = new HttpClient();
    private static Bud _currentBud;
    private static ModtagOrdreService _modtagOrdreService = new ModtagOrdreService();
    private static OrdreTagetService _ordreTagetService = new OrdreTagetService();
    private static List<Bud> _alleBuds;

    public static async Task Main(string[] args)
    {
        await _modtagOrdreService.StartAsync(new CancellationToken());
        while (_alleBuds == null) {
            try
            {
                _alleBuds = await _httpClient.GetFromJsonAsync<List<Bud>>("http://localhost:5063/api/eaat/buds");
            }
            catch
            {
                await Task.Delay(2000);
            }
        }

        Console.WriteLine("Indtast id");
        string input = Console.ReadLine();
        _currentBud = _alleBuds.Find(i => i.Id == Int32.Parse(input));

        if (_currentBud != null)
        {
            var eksisterendeBestillinger = await _httpClient.GetFromJsonAsync<List<Bestilling>>("http://localhost:5063/api/eaat/bestillinger");
            if (eksisterendeBestillinger != null)
            {
                foreach (var bestilling in eksisterendeBestillinger)
                {
                    if ((bestilling.BudId == 0 || bestilling.BudId == null) && !ModtagOrdreService.Bestillinger.Any(i => i.Id == bestilling.Id))
                    {
                        ModtagOrdreService.Bestillinger.Add(bestilling);
                    }
                }
            }
            PrintBestillinger();
            ModtagOrdreService.OnMessageReceived = () =>
            {
                PrintBestillinger();
            };
            await Task.Delay(-1);
        }
    }

    private static async Task PrintBestillinger()
    {
        Console.Clear();
        Console.WriteLine($"Logget ind som: {_currentBud.Id}");
        Console.WriteLine("-----------------------------------------");

        var alleBestillinger = ModtagOrdreService.Bestillinger.ToList();
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
        Console.WriteLine("Indtast ID på bestilling du vil tage: ");
        string input = Console.ReadLine();
        if (int.TryParse(input, out int valgtId))
        {
            var fundetBestilling = alleBestillinger.FirstOrDefault(i => i.Id == valgtId);

            if (fundetBestilling != null)
            {
                await _ordreTagetService.SendTagetBeskedAsync(valgtId, _currentBud.Id);

                Console.WriteLine("Besked sendt! Vent på opdatering...");
            }
            else
            {
                Console.WriteLine("ID findes ikke i listen.");
            }
        }

    }
}