using FlightAssistant.Models;
using Npgsql;

namespace FlightAssistant.Services;

public class PostgreService(NpgsqlDataSource dataSource) : IPostgreService
{
    public async Task<FlightModel?> GetFlightInfoAsync(string flightNumber)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, description, from_city, to_city, duration, total_seats, seats
            FROM flights WHERE name = @name LIMIT 1
            """;
        cmd.Parameters.AddWithValue("name", flightNumber);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapFlight(reader) : null;
    }

    public async Task CreateFlightAsync(FlightModel model)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO flights (name, description, from_city, to_city, duration, total_seats, seats)
            VALUES (@name, @description, @from, @to, @duration, @totalSeats, @seats)
            """;
        cmd.Parameters.AddWithValue("name", model.Name);
        cmd.Parameters.AddWithValue("description", model.Description);
        cmd.Parameters.AddWithValue("from", model.From);
        cmd.Parameters.AddWithValue("to", model.To);
        cmd.Parameters.AddWithValue("duration", model.Duration);
        cmd.Parameters.AddWithValue("totalSeats", model.TotalSeats);
        cmd.Parameters.AddWithValue("seats", model.Seats);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<FlightModel>> GetAllFlightsAsync()
    {
        var flights = new List<FlightModel>();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, description, from_city, to_city, duration, total_seats, seats
            FROM flights ORDER BY id
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            flights.Add(MapFlight(reader));
        return flights;
    }

    private static FlightModel MapFlight(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Description = reader.GetString(2),
        From = reader.GetString(3),
        To = reader.GetString(4),
        Duration = reader.GetDecimal(5),
        TotalSeats = reader.GetInt32(6),
        Seats = reader.GetInt32(7),
    };
}
