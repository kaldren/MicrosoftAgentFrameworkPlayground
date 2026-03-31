using FlightAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightAssistant.Pages;

public class AssistantModel : PageModel
{
    // Inject IFlightAgent
    private readonly IFlightAgent _flightAgent;

    public AssistantModel(IFlightAgent flightAgent)
    {
        _flightAgent = flightAgent;
    }

    public void OnGet()
    {
    }

    // Create Agent workflow using Microsoft Agent Framework
    // See https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows?pivots=programming-language-csharp
    public void OnPost() 
    {
        IsLoading = true;

        _flightAgent.AskQuestionAsync("What is the status of flight AZ123?").ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                var answer = task.Result;
                AgentAnswer = answer;
            }
            else
            {
                // Handle error
            }
            IsLoading = false;
        });
    }

    // Public property for isLoading
    [BindProperty]
    public bool IsLoading { get; set; } = false;

    // Public property for the answer from the agent
    [BindProperty]
    public string AgentAnswer { get; set; } = string.Empty;
}

