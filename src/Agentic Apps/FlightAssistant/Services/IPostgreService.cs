using FlightAssistant.Models;

namespace FlightAssistant.Services;

public interface IPostgreService
{
    Task<FlightModel?> GetFlightInfoAsync(string flightNumber);
    Task CreateFlightAsync(FlightModel model);
    Task<IEnumerable<FlightModel>> GetAllFlightsAsync();
}
