using Microsoft.Agents.AI.Workflows;

namespace _7_Workflows.Executors;

[SendsMessage(typeof(string))]
internal sealed class OrderDeliveryExecutor : Executor<string>
{
    public OrderDeliveryExecutor(string id) : base(id)
    {
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Order Delivery: {message}");
        await context.SendMessageAsync(message, cancellationToken);
    }
}
