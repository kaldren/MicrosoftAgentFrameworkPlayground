using _7_Workflows.Executors;
using Microsoft.Agents.AI.Workflows;

// Workflow to prepare a pizza order and then execute it, and deliver it to the customer.

var orderExecutor = new TakeOrderExecutor("TakeOrderExecutor");
var prepExecutor = new OrderPreparationExecutor("OrderPreparationExecutor");
var deliveryExecutor = new OrderDeliveryExecutor("OrderDeliveryExecutor");
var finalizedExecutor = new OrderFinalizedExecutor("OrderFinalizedExecutor");

var workflow = new WorkflowBuilder(orderExecutor)
    .AddEdge(orderExecutor, prepExecutor)
    .AddEdge(prepExecutor, deliveryExecutor)
    .AddEdge(deliveryExecutor, finalizedExecutor)
    .WithOutputFrom(finalizedExecutor)
    .Build();

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, "Piperoni with extra cheese", cancellationToken: default);

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is WorkflowOutputEvent outputEvent)
    {
        Console.WriteLine($"Workflow Output: {outputEvent}");
    }
};