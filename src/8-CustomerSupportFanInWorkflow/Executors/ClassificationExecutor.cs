using _8_CustomerSupportFanInWorkflow.Agents;
using _8_CustomerSupportFanInWorkflow.Infrastructure;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace _8_CustomerSupportFanInWorkflow.Executors;

[SendsMessage(typeof(string))]
internal sealed class ClassificationExecutor : Executor<string>
{
    private readonly TicketDatabase _ticketDatabase;

    public ClassificationExecutor(string id, TicketDatabase ticketDatabase) : base(id)
    {
        _ticketDatabase = ticketDatabase;
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var chatClient = ChatClientFactory.Create();
        var agent = ClassificationAgent.Create(chatClient, message);
        var result = await agent.RunAsync();

        var text = result.AsChatResponse().Text;

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        await _ticketDatabase.InsertTicketAsync(
            from: root.GetProperty("from").GetString()!,
            message: root.GetProperty("message").GetString()!,
            summary: root.GetProperty("summary").GetString()!,
            intent: root.GetProperty("intent").GetString()!,
            urgency: root.GetProperty("urgency").GetString()!,
            cancellationToken: cancellationToken);

        await context.SendMessageAsync(text, cancellationToken);
    }
}
