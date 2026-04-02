using _7_PizzaOrderDirectWorkflow.Executors;
using Microsoft.Agents.AI.Workflows;

// Workflow to prepare a pizza order and then execute it, and deliver it to the customer.

var takeOrderExec = new TakeOrderExecutor("TakeOrderExecutor");
var orderPrepExec = new OrderPreparationExecutor("OrderPreparationExecutor");
var deliveryExec = new OrderDeliveryExecutor("OrderDeliveryExecutor");
var orderCompletedExec = new OrderCompletedExecutor("OrderCompletedExecutor");

var workflow = new WorkflowBuilder(takeOrderExec)
    .AddEdge(takeOrderExec, orderPrepExec)
    .AddEdge(orderPrepExec, deliveryExec)
    .AddEdge(deliveryExec, orderCompletedExec)
    .WithOutputFrom(orderCompletedExec)
    .Build();

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, "3 XXL pipperoni with extra cheeze. Two capricciosas, small. Five diet cokes, one large bottle of water. Couple of french fries.", cancellationToken: default);

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is WorkflowOutputEvent outputEvent)
    {
        Console.WriteLine($"Workflow Output: {outputEvent}");
    }
};