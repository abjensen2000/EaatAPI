using BudKlient;
using EaatAPI.Models;
using System.Net.Http.Json;
using System.Text;

public class Program
{
    private static HttpClient _httpClient = new HttpClient();
    private static Bud _currentBud;
    private static ModtagBestillingService _modtagOrdreService = new ModtagBestillingService();
    private static BestillingTagetService _ordreTagetService = new BestillingTagetService();
    private static List<Bud> _alleBuds;
    private static bool _redrawRequested = false;

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
                    if ((bestilling.BudId == 0 || bestilling.BudId == null) && !ModtagBestillingService.Bestillinger.ContainsKey(bestilling.Id))
                    {
                        ModtagBestillingService.Bestillinger.TryAdd(bestilling.Id, bestilling);
                    }
                }
            }
            
            ModtagBestillingService.OnMessageReceived = () =>
            {
                _redrawRequested = true;
            };
            
            await BestillingsLoop();
        }
    }

    private static async Task BestillingsLoop()
    {
        _redrawRequested = true;
        string currentInput = "";
        
        while(true)
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
                        var fundetBestilling = ModtagBestillingService.Bestillinger.Values.FirstOrDefault(i => i.Id == valgtId);

                        if (fundetBestilling != null)
                        {
                            await _ordreTagetService.SendTagetBeskedAsync(valgtId, _currentBud.Id);
                            ModtagBestillingService.Bestillinger.TryRemove(valgtId, out _);
                            Console.WriteLine("\nBesked sendt! Vent på opdatering...");
                            await Task.Delay(1000);
                        }
                        else
                        {
                            Console.WriteLine("\nID findes ikke i listen.");
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
        Console.WriteLine($"Logget ind som: {_currentBud.Id}");
        Console.WriteLine("-----------------------------------------");

        var alleBestillinger = ModtagBestillingService.Bestillinger.Values.ToList();
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
        Console.Write($"Indtast ID på bestilling du vil tage: ");
        Console.Write(inputSoFar);
    }
}