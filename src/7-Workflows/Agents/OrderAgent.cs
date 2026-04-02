using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace _7_Workflows.Agents;

internal static class OrderAgent
{
    public static ChatClientAgent GetOrderAgent(IChatClient chatClient) =>
    new(chatClient, new ChatClientAgentOptions()    {
        ChatOptions = new()
        {
            Instructions = "You are an assistant for processing orders. You will be given a description of an order and you need to extract the relevant information and return it in a structured format.",
            ResponseFormat = ChatResponseFormat.Text
        }
    });
}
