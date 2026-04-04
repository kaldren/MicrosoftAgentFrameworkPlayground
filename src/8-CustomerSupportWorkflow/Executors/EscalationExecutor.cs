using _8_CustomerSupportFWorkflow.Agents;
using _8_CustomerSupportFWorkflow.Infrastructure;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System.Text.Json;

namespace _8_CustomerSupportFWorkflow.Executors;

[SendsMessage(typeof(string))]
[YieldsOutput(typeof(string))]
internal sealed class EscalationExecutor : Executor<string>
{
    private readonly TicketDatabase _ticketDatabase;

    public EscalationExecutor(string id, TicketDatabase ticketDatabase) : base(id)
    {
        _ticketDatabase = ticketDatabase;
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var chatClient = ChatClientFactory.Create();
        var agent = EscalationAgent.Create(chatClient, message, _ticketDatabase);
        var result = await agent.RunAsync();

        var functionApprovalRequests = result.Messages
            .SelectMany(x => x.Contents)
            .ToList();

        var text = result.AsChatResponse().Text;

        Console.WriteLine(text);

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        string? escalationDepartment = root.GetProperty("escalation_department").ValueKind == JsonValueKind.Null
            ? null
            : root.GetProperty("escalation_department").GetString();

        string? teamsMessage = root.GetProperty("teams_message").ValueKind == JsonValueKind.Null
            ? null
            : root.GetProperty("teams_message").GetString();

        await _ticketDatabase.InsertTicketEscalationAsync(
            sentiment: root.GetProperty("sentiment").GetString()!,
            customerFrustration: root.GetProperty("customer_frustration").GetString()!,
            churnRisk: root.GetProperty("churn_risk").GetBoolean(),
            reputationRisk: root.GetProperty("reputation_risk").GetBoolean(),
            refundRisk: root.GetProperty("refund_risk").GetBoolean(),
            escalationRecommended: root.GetProperty("escalation_recommended").GetBoolean(),
            escalationDepartment: escalationDepartment,
            teamsMessage: teamsMessage,
            riskSummary: root.GetProperty("risk_summary").GetString()!,
            confidence: root.GetProperty("confidence").GetDouble(),
            cancellationToken: cancellationToken);

        await context.YieldOutputAsync(text, cancellationToken);
    }
}
