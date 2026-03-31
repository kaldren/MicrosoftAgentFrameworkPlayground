using Microsoft.Agents.AI.Workflows;

namespace _7_Workflows.Executors;

[SendsMessage(typeof(string))]
internal sealed class TakeOrderExecutor : Executor<string>
{
    public TakeOrderExecutor(string id) : base(id)
    {
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Taking Order: {message}");
        await context.SendMessageAsync(message, cancellationToken);
    }
}
