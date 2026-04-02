using Npgsql;
using Testcontainers.PostgreSql;

namespace _8_CustomerSupportFanInWorkflow.Infrastructure;

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

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
