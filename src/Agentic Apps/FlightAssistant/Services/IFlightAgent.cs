namespace FlightAssistant.Services;

public interface IFlightAgent
{
    Task<string> AskQuestionAsync(string question);
}
