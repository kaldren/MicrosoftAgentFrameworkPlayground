namespace _8_CustomerSupportFWorkflow.Models;

internal class TicketEscalation
{
    public int Id { get; set; }
    public string Sentiment { get; set; } = string.Empty;
    public string CustomerFrustration { get; set; } = string.Empty;
    public bool ChurnRisk { get; set; }
    public bool ReputationRisk { get; set; }
    public bool RefundRisk { get; set; }
    public bool EscalationRecommended { get; set; }
    public string? EscalationDepartment { get; set; }
    public string? TeamsMessage { get; set; }
    public string RiskSummary { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}
