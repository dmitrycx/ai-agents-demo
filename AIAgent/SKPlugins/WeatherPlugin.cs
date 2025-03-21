using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace AIAgent.SKPlugins;

public class WeatherPlugin
{
    [KernelFunction("gets_the_weather")]
    [Description("Gets the weather")]
    public string GetWeather(string destination)
    {
        return $"It's {(Random.Shared.NextDouble() > 0.5 ? "sunny" : "raining")} in {destination}";
    }
    
    [KernelFunction("gets_the_wind_speed")]
    [Description("Gets the wind speed")]
    public string GetWindSpeed()
    {
        return $"{Random.Shared.Next(0, 50)} m/s";
    }
}