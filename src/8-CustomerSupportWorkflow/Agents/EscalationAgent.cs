using _8_CustomerSupportFanInWorkflow.Tools;
using _8_CustomerSupportFWorkflow.Infrastructure;
using _8_CustomerSupportFWorkflow.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace _8_CustomerSupportFWorkflow.Agents;

internal static class EscalationAgent
{
    public static AIAgent Create(IChatClient chatClient, string message, TicketDatabase ticketDatabase)
    {
        return chatClient.AsAIAgent(
        name: "EscalationAgent",
        instructions: $$"""
            ## Role
            Escalation Agent for customer support.

            ## Task
            Analyze the RiskAssessmentAgent JSON output. If escalation is recommended, determine which executive to notify and send an SMS using the SendSms tool.

            ## Input
            {"from":"<email>","message":"<original message>","sentiment":"<Positive|Neutral|Negative>","customer_frustration":"<Low|Medium|High>","churn_risk":<true|false>,"reputation_risk":<true|false>,"refund_risk":<true|false>,"escalation_recommended":<true|false>,"risk_summary":"<one sentence>","confidence":<0.0-1.0>}

            ## Escalation Rules (first match wins, only when `escalation_recommended` is `true`)

            1. **CEO** — `churn_risk` AND `reputation_risk` AND `customer_frustration` = `High`.
            2. **CTO** — Critical product defect, security vulnerability, data breach, system outage, or technical failure.
            3. **Legal Counsel** — Legal threats, lawsuit mentions, regulatory/compliance issues, or data privacy violations.

            If `escalation_recommended` is `true` but no rule matches, default to **CEO**.

            **SMS Requirement**: For EVERY escalation, call SendSms with body formatted as:
            "To [department]: [customer email] - [concise risk summary and recommended action]"

            When `escalation_recommended` is `false`, set `escalation_department` and `teams_message` to `null`. Do not send SMS.

            ## Output (strict JSON, no markdown, no commentary)
            {"sentiment":"<Positive|Neutral|Negative>","customer_frustration":"<Low|Medium|High>","churn_risk":<true|false>,"reputation_risk":<true|false>,"refund_risk":<true|false>,"escalation_recommended":<true|false>,"escalation_department":"<CEO|CTO|Legal Counsel|null>","teams_message":"<concise professional message or null>","risk_summary":"<one sentence>","confidence":<0.0-1.0>}

            ## Examples
            Input: {"from":"johndoe@example.com","message":"I ordered a laptop 10 days ago and it still hasn't arrived. This is unacceptable. If I don't get it soon I will cancel and leave a bad review.","sentiment":"Negative","customer_frustration":"High","churn_risk":true,"reputation_risk":true,"refund_risk":false,"escalation_recommended":true,"risk_summary":"Highly frustrated customer with churn and reputation risk.","confidence":0.96}
            Output: {"sentiment":"Negative","customer_frustration":"High","churn_risk":true,"reputation_risk":true,"refund_risk":false,"escalation_recommended":true,"escalation_department":"CEO","teams_message":"Urgent: johndoe@example.com is highly frustrated, threatens cancellation and bad review. Immediate retention action recommended.","risk_summary":"Highly frustrated customer with churn and reputation risk.","confidence":0.96}

            Input: {"from":"janedoe@example.com","message":"Could you tell me what your return policy is for opened items?","sentiment":"Neutral","customer_frustration":"Low","churn_risk":false,"reputation_risk":false,"refund_risk":false,"escalation_recommended":false,"risk_summary":"Low-risk informational request.","confidence":0.9}
            Output: {"sentiment":"Neutral","customer_frustration":"Low","churn_risk":false,"reputation_risk":false,"refund_risk":false,"escalation_recommended":false,"escalation_department":null,"teams_message":null,"risk_summary":"Low-risk informational request.","confidence":0.9}

            ## Risk Assessment Record
            {{message}}
            """,
        tools: [
            AIFunctionFactory.Create((int ticketId) => DbTools.GetTicketStatusById(ticketId, ticketDatabase), nameof(DbTools.GetTicketStatusById), "Get the status of a ticket by its id."),
            AIFunctionFactory.Create((string body) => NotificationTools.SendSms(body), nameof(NotificationTools.SendSms), "Send an SMS notification for executive escalations.")
        ]
        );
    }
}
