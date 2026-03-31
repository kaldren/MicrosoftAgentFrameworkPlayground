namespace FlightAssistant.Models;

public class FlightModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public decimal Duration { get; set; }
    public int TotalSeats { get; set; }
    public int Seats { get; set; }
}
