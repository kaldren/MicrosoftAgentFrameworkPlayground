using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace _8_CustomerSupportFWorkflow.Agents;

internal static class ClassificationAgent
{
    public static AIAgent Create(IChatClient chatClient, string message)
    {
        return chatClient.AsAIAgent(
            name: "ClassificationAgent",
            instructions: $$"""
                ## Role
                Classification Agent for customer support.

                ## Task
                Classify the customer email by intent, urgency, and provide a one-sentence summary.

                ## Rules
                - **intent**: `Complaint`, `Request`, `Feedback`, or `Other`.
                - **urgency**: `Low` (general/positive), `Medium` (time-sensitive/moderate frustration), `High` (outage/legal/safety/repeated escalation).

                ## Output (strict JSON, no markdown, no commentary)
                {"from":"<email>","message":"<original message>","summary":"<one sentence>","intent":"<Complaint|Request|Feedback|Other>","urgency":"<Low|Medium|High>"}

                ## Examples
                Email: "I've been waiting 3 weeks for my refund and no one has responded to my last two emails."
                Output: {"from":"johndoe@example.com","message":"I've been waiting 3 weeks for my refund and no one has responded to my last two emails.","summary":"Customer waiting 3 weeks for a refund with no response.","intent":"Complaint","urgency":"High"}

                Email: "Could you tell me what your return policy is for opened items?"
                Output: {"from":"janedoe@example.com","message":"Could you tell me what your return policy is for opened items?","summary":"Customer asking about return policy for opened items.","intent":"Request","urgency":"Low"}

                ## Email
                {{message}}
                """
        );
    }
}
