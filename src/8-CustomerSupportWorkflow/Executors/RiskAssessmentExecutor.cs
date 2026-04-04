using _8_CustomerSupportFWorkflow.Agents;
using _8_CustomerSupportFWorkflow.Infrastructure;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace _8_CustomerSupportFWorkflow.Executors;

[SendsMessage(typeof(string))]
internal sealed class RiskAssessmentExecutor : Executor<string>
{
    private readonly TicketDatabase _ticketDatabase;

    public RiskAssessmentExecutor(string id, TicketDatabase ticketDatabase) : base(id)
    {
        _ticketDatabase = ticketDatabase;
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var chatClient = ChatClientFactory.Create();
        var agent = RiskAssessmentAgent.Create(chatClient, message, _ticketDatabase);
        var result = await agent.RunAsync();

        var text = result.AsChatResponse().Text;

        Console.WriteLine(text);

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        await _ticketDatabase.InsertTicketRiskAsync(
            sentiment: root.GetProperty("sentiment").GetString()!,
            customerFrustration: root.GetProperty("customer_frustration").GetString()!,
            churnRisk: root.GetProperty("churn_risk").GetBoolean(),
            reputationRisk: root.GetProperty("reputation_risk").GetBoolean(),
            refundRisk: root.GetProperty("refund_risk").GetBoolean(),
            escalationRecommended: root.GetProperty("escalation_recommended").GetBoolean(),
            riskSummary: root.GetProperty("risk_summary").GetString()!,
            confidence: root.GetProperty("confidence").GetDouble(),
            cancellationToken: cancellationToken);

        await context.SendMessageAsync(text, cancellationToken);
    }
}
