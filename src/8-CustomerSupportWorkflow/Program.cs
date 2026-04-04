using _8_CustomerSupportFWorkflow.Executors;
using _8_CustomerSupportFWorkflow.Infrastructure;
using Microsoft.Agents.AI.Workflows;

await using var ticketDatabase = new TicketDatabase();
await ticketDatabase.StartAsync();

Console.WriteLine($"PostgreSQL started. Connection string: {ticketDatabase.ConnectionString}");


var classificationExecutor = new ClassificationExecutor("ClassificationExecutor", ticketDatabase);
var riskAssessmentExecutor = new RiskAssessmentExecutor("RiskAssessmentExecutor", ticketDatabase);
var escalationExecutor = new EscalationExecutor("EscalationExecutor", ticketDatabase);

var workflow = new WorkflowBuilder(classificationExecutor)
    .AddEdge(classificationExecutor, riskAssessmentExecutor)
    .AddEdge(riskAssessmentExecutor, escalationExecutor)
    .WithOutputFrom(escalationExecutor)
    .Build();

while(true)
{
    var email = Console.ReadLine();

    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, email, cancellationToken: default);

    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        if (evt is WorkflowOutputEvent outputEvent)
        {
            Console.WriteLine($"Workflow Output: {outputEvent}");
        }
    }
}