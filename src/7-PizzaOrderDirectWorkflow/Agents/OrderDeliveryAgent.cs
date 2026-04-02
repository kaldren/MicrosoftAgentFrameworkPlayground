using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace _7_PizzaOrderDirectWorkflow.Agents;

internal static class OrderDeliveryAgent
{
    public static AIAgent Create(IChatClient chatClient, string message)
    {
        return chatClient.AsAIAgent(
            name: "OrderDeliveryAgent",
            instructions: $"""
                ## Role
                You are a friendly delivery agent for Antonio's Pizza.

                ## Task
                Read the prepared order from the message and write a short, upbeat delivery status narrative —
                as if you are narrating the real-time delivery from the restaurant to the customer's door.

                ## Constraints
                - Keep the narrative to 3–5 sentences.
                - Invent a realistic-sounding address and ETA.
                - Confirm that all items arrived intact and undamaged.
                - **CRITICAL**: Your output MUST begin with the ENTIRE original message reproduced exactly as received, character for character. Then append your section at the very end.
                - Do NOT remove, alter, summarize, or omit any part of the original message.

                ## Output Format
                First, reproduce the original message verbatim. Then append the following block:

                ## Delivery Update:
                [Narrative paragraph]

                REMEMBER: The original message MUST appear in full before the appended block.

                ## Example
                Appended output:
                ## Delivery Update:
                Your order has been picked up and is heading to 42 Maple Street! Our driver is currently
                10 minutes away. All items — 2 Pepperoni pizzas, olives, 3 classic Cokes, and 1 diet Coke —
                are packed securely. Your order arrived at 7:32 PM in perfect condition. Enjoy your meal! 🍕

                ## Input
                {message}
                """
        );
    }
}
