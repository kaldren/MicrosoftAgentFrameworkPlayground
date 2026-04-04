using _8_CustomerSupportFWorkflow.Infrastructure;
using _8_CustomerSupportFWorkflow.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace _8_CustomerSupportFWorkflow.Agents;

internal static class RiskAssessmentAgent
{
    public static AIAgent Create(IChatClient chatClient, string message, TicketDatabase ticketDatabase)
    {
        return chatClient.AsAIAgent(
        name: "RiskAssessmentAgent",
        instructions: $$"""
            ## Role
            Risk Assessment Agent for customer support.

            ## Task
            Analyze the ClassificationAgent JSON output. Assess sentiment, frustration, and business risks. Pass through `from` and `message` from the input.

            ## Input
            {"from":"<email>","message":"<original message>","summary":"<one sentence>","intent":"<Complaint|Request|Feedback|Other>","urgency":"<Low|Medium|High>"}

            ## Rules
            - **sentiment**: `Positive`, `Neutral`, or `Negative`.
            - **customer_frustration**: `Low` (calm/positive), `Medium` (moderate dissatisfaction), `High` (threats/repeated issues/explicit dissatisfaction).
            - **churn_risk**: `true` if customer suggests cancellation, leaving, or switching.
            - **reputation_risk**: `true` if customer threatens bad review or public escalation.
            - **refund_risk**: `true` if customer requests or implies a refund.
            - **escalation_recommended**: `true` if serious dissatisfaction, business risk, or urgent attention needed.

            ## Output (strict JSON, no markdown, no commentary)
            {"from":"<email>","message":"<original message>","sentiment":"<Positive|Neutral|Negative>","customer_frustration":"<Low|Medium|High>","churn_risk":<true|false>,"reputation_risk":<true|false>,"refund_risk":<true|false>,"escalation_recommended":<true|false>,"risk_summary":"<one sentence>","confidence":<0.0-1.0>}

            ## Examples
            Input: {"from":"johndoe@example.com","message":"I ordered a laptop 10 days ago and it still hasn't arrived. This is unacceptable. If I don't get it soon I will cancel and leave a bad review.","summary":"Customer complains about delayed laptop delivery and threatens cancellation.","intent":"Complaint","urgency":"High"}
            Output: {"from":"johndoe@example.com","message":"I ordered a laptop 10 days ago and it still hasn't arrived. This is unacceptable. If I don't get it soon I will cancel and leave a bad review.","sentiment":"Negative","customer_frustration":"High","churn_risk":true,"reputation_risk":true,"refund_risk":false,"escalation_recommended":true,"risk_summary":"Highly frustrated customer with churn and reputation risk.","confidence":0.96}

            Input: {"from":"janedoe@example.com","message":"Could you tell me what your return policy is for opened items?","summary":"Customer asking about return policy.","intent":"Request","urgency":"Low"}
            Output: {"from":"janedoe@example.com","message":"Could you tell me what your return policy is for opened items?","sentiment":"Neutral","customer_frustration":"Low","churn_risk":false,"reputation_risk":false,"refund_risk":false,"escalation_recommended":false,"risk_summary":"Low-risk informational request.","confidence":0.9}

            ## Classification Record
            {{message}}
            """,
        tools: [AIFunctionFactory.Create((int ticketId) => DbTools.GetTicketStatusById(ticketId, ticketDatabase), nameof(DbTools.GetTicketStatusById), "Get the status of a ticket by its id.")]
        );
    }
}
