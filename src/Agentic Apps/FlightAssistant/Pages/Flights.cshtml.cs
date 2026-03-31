using FlightAssistant.Models;
using FlightAssistant.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightAssistant.Pages;

public class FlightsModel(IPostgreService postgreService) : PageModel
{
    public IEnumerable<FlightModel> Flights { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Flights = await postgreService.GetAllFlightsAsync();
    }
}
