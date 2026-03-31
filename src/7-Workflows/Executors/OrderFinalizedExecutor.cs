using Microsoft.Agents.AI.Workflows;

namespace _7_Workflows.Executors;

[SendsMessage(typeof(string))]
[YieldsOutput(typeof(string))]
internal sealed class OrderFinalizedExecutor : Executor<string>
{
    public OrderFinalizedExecutor(string id) : base(id)
    {
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Order Finalized: {message}");
        await context.YieldOutputAsync(message, cancellationToken);
    }
}