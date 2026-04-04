using _8_CustomerSupportFWorkflow.Models;
using Npgsql;
using Testcontainers.PostgreSql;

namespace _8_CustomerSupportFWorkflow.Infrastructure;

internal sealed class TicketDatabase : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("supportdb")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _container.StartAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS tickets (
                id SERIAL PRIMARY KEY,
                "from" TEXT NOT NULL,
                message TEXT NOT NULL,
                summary TEXT NOT NULL,
                intent TEXT NOT NULL,
                urgency TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS tickets_risk (
                id SERIAL PRIMARY KEY,
                sentiment TEXT NOT NULL,
                customer_frustration TEXT NOT NULL,
                churn_risk BOOLEAN NOT NULL,
                reputation_risk BOOLEAN NOT NULL,
                refund_risk BOOLEAN NOT NULL,
                escalation_recommended BOOLEAN NOT NULL,
                risk_summary TEXT NOT NULL,
                confidence DOUBLE PRECISION NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS ticket_escalation (
                id SERIAL PRIMARY KEY,
                sentiment TEXT NOT NULL,
                customer_frustration TEXT NOT NULL,
                churn_risk BOOLEAN NOT NULL,
                reputation_risk BOOLEAN NOT NULL,
                refund_risk BOOLEAN NOT NULL,
                escalation_recommended BOOLEAN NOT NULL,
                escalation_department TEXT,
                teams_message TEXT,
                risk_summary TEXT NOT NULL,
                confidence DOUBLE PRECISION NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """, connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertTicketAsync(
        string from,
        string message,
        string summary,
        string intent,
        string urgency,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            INSERT INTO tickets ("from", message, summary, intent, urgency)
            VALUES (@from, @message, @summary, @intent, @urgency);
            """, connection);

        command.Parameters.AddWithValue("from", from);
        command.Parameters.AddWithValue("message", message);
        command.Parameters.AddWithValue("summary", summary);
        command.Parameters.AddWithValue("intent", intent);
        command.Parameters.AddWithValue("urgency", urgency);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Get ticket by id
    public async Task<Ticket?> GetTicketByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT id, "from", message, summary, intent, urgency, created_at
            FROM tickets
            WHERE id = @id;
            """, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Ticket
            {
                Id = reader.GetInt32(0),
                From = reader.GetString(1),
                Message = reader.GetString(2),
                Summary = reader.GetString(3),
                Intent = reader.GetString(4),
                Urgency = reader.GetString(5),
                CreatedAt = reader.GetDateTime(6)
            };
        }
        return null;
    }

    public async Task InsertTicketRiskAsync(
        string sentiment,
        string customerFrustration,
        bool churnRisk,
        bool reputationRisk,
        bool refundRisk,
        bool escalationRecommended,
        string riskSummary,
        double confidence,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            INSERT INTO tickets_risk (sentiment, customer_frustration, churn_risk, reputation_risk, refund_risk, escalation_recommended, risk_summary, confidence)
            VALUES (@sentiment, @customer_frustration, @churn_risk, @reputation_risk, @refund_risk, @escalation_recommended, @risk_summary, @confidence);
            """, connection);

        command.Parameters.AddWithValue("sentiment", sentiment);
        command.Parameters.AddWithValue("customer_frustration", customerFrustration);
        command.Parameters.AddWithValue("churn_risk", churnRisk);
        command.Parameters.AddWithValue("reputation_risk", reputationRisk);
        command.Parameters.AddWithValue("refund_risk", refundRisk);
        command.Parameters.AddWithValue("escalation_recommended", escalationRecommended);
        command.Parameters.AddWithValue("risk_summary", riskSummary);
        command.Parameters.AddWithValue("confidence", confidence);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertTicketEscalationAsync(
        string sentiment,
        string customerFrustration,
        bool churnRisk,
        bool reputationRisk,
        bool refundRisk,
        bool escalationRecommended,
        string? escalationDepartment,
        string? teamsMessage,
        string riskSummary,
        double confidence,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("""
            INSERT INTO ticket_escalation (sentiment, customer_frustration, churn_risk, reputation_risk, refund_risk, escalation_recommended, escalation_department, teams_message, risk_summary, confidence)
            VALUES (@sentiment, @customer_frustration, @churn_risk, @reputation_risk, @refund_risk, @escalation_recommended, @escalation_department, @teams_message, @risk_summary, @confidence);
            """, connection);

        command.Parameters.AddWithValue("sentiment", sentiment);
        command.Parameters.AddWithValue("customer_frustration", customerFrustration);
        command.Parameters.AddWithValue("churn_risk", churnRisk);
        command.Parameters.AddWithValue("reputation_risk", reputationRisk);
        command.Parameters.AddWithValue("refund_risk", refundRisk);
        command.Parameters.AddWithValue("escalation_recommended", escalationRecommended);
        command.Parameters.AddWithValue("escalation_department", (object?)escalationDepartment ?? DBNull.Value);
        command.Parameters.AddWithValue("teams_message", (object?)teamsMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("risk_summary", riskSummary);
        command.Parameters.AddWithValue("confidence", confidence);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
