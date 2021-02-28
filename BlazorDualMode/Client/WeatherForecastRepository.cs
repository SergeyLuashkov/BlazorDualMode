using BlazorDualMode.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlazorDualMode.Client
{
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly HttpClient httpClient;

        public WeatherForecastRepository(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        public async Task<WeatherForecast[]> GetWeatherForecast()
        {
            return await httpClient.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
        }
    }
}
