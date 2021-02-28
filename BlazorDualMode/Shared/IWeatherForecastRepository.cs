using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDualMode.Shared
{
    public interface IWeatherForecastRepository
    {
        Task<WeatherForecast[]> GetWeatherForecast();
    }
}
