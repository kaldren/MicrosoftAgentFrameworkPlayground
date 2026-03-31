using Microsoft.Agents.AI.Workflows;

namespace _7_Workflows.Executors;

[SendsMessage(typeof(string))]
internal sealed class OrderPreparationExecutor : Executor<string>
{
    public OrderPreparationExecutor(string id) : base(id)
    {
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Preparing Order: {message}");
        await context.SendMessageAsync(message, cancellationToken);
    }
}
