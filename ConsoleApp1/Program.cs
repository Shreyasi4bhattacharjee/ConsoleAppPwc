using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherTool
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            bool exitRequested = false;

            do
            {
                Console.WriteLine("Enter the city (or type 'exit' to quit):");
                string city = Console.ReadLine();

                if (string.Equals(city, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    exitRequested = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(city))
                {
                    Console.WriteLine("Invalid city name. Please try again.");
                    continue;
                }

                // Retrieve latitude and longitude for the given city from the in.json file
                (decimal latitude, decimal longitude) = GetCoordinatesForCity(city);

                if (latitude == 0.0M && longitude == 0.0M)
                {
                    Console.WriteLine("City not found. Please try again.");
                    continue;
                }

                string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true";

                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(apiUrl).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        WeatherData weatherData = JsonSerializer.Deserialize<WeatherData>(jsonResponse);

                        double temperature = weatherData.current_weather.temperature;
                        double windSpeed = weatherData.current_weather.windspeed;

                        Console.WriteLine($"Temperature: {temperature}°C");
                        Console.WriteLine($"Wind Speed: {windSpeed} m/s");
                    }
                    else
                    {
                        Console.WriteLine("Failed to retrieve weather information. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                Console.WriteLine();
            } while (!exitRequested);
        }

        static (decimal, decimal) GetCoordinatesForCity(string city)
        {
            string jsonFilePath = "in.json";
            string jsonData = File.ReadAllText(jsonFilePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var cities = JsonSerializer.Deserialize<CityData[]>(jsonData, options);

            var selectedCity = Array.Find(cities, c => string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));

            if (selectedCity != null)
            {
                decimal latitude = decimal.Parse(selectedCity.lat);
                decimal longitude = decimal.Parse(selectedCity.lng);
                return (latitude, longitude);
            }

            return (0.0M, 0.0M);
        }
    }

    class CityData
    {
        public string City { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
    }

    class WeatherData
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public CurrentWeatherData current_weather { get; set; }
    }

    class CurrentWeatherData
    {
        public double temperature { get; set; }
        public double windspeed { get; set; }
        public double winddirection { get; set; }
    }
}
