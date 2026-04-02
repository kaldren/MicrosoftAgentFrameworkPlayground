using _7_PizzaOrderDirectWorkflow.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace _7_PizzaOrderDirectWorkflow.Executors;

[SendsMessage(typeof(string))]
internal sealed class OrderDeliveryExecutor : Executor<string>
{
    public OrderDeliveryExecutor(string id) : base(id)
    {
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var chatClient = ChatClientFactory.Create();
        var agent = OrderDeliveryAgent.Create(chatClient, message);
        var result = await agent.RunAsync();

        await context.SendMessageAsync(result.AsChatResponse().Text, cancellationToken);
    }
}
