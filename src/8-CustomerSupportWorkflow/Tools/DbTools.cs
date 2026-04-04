using _8_CustomerSupportFWorkflow.Infrastructure;
using _8_CustomerSupportFWorkflow.Models;
using System.ComponentModel;

namespace _8_CustomerSupportFWorkflow.Tools;

internal static class DbTools
{
    [Description("Get the status of a ticket by its id.")]
    public static async Task<Ticket?> GetTicketStatusById(int ticketId, TicketDatabase db)
    {
        var ticket = await db.GetTicketByIdAsync(ticketId);

        return ticket;
    }
}
