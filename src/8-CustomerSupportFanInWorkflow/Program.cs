using _8_CustomerSupportFanInWorkflow.Executors;
using _8_CustomerSupportFanInWorkflow.Infrastructure;
using Microsoft.Agents.AI.Workflows;

await using var ticketDatabase = new TicketDatabase();
await ticketDatabase.StartAsync();

Console.WriteLine($"PostgreSQL started. Connection string: {ticketDatabase.ConnectionString}");


var classificationExecutor = new ClassificationExecutor("ClassificationExecutor", ticketDatabase);
var riskAssessmentExecutor = new RiskAssessmentExecutor("RiskAssessmentExecutor", ticketDatabase);

var workflow = new WorkflowBuilder(classificationExecutor)
    .AddEdge(classificationExecutor, riskAssessmentExecutor)
    .WithOutputFrom(riskAssessmentExecutor)
    .Build();

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, "Guys, you are terrible! I've been waiting for 3 days for your services to start working! I'll probably look for someone else soon.", cancellationToken: default);

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is WorkflowOutputEvent outputEvent)
    {
        Console.WriteLine($"Workflow Output: {outputEvent}");
    }
}

Console.ReadLine();