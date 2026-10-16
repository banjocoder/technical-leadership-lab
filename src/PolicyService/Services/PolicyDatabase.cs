using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Data;

namespace PolicyService.Services;

public sealed class PolicyDatabase(IConfiguration configuration)
{
    public async Task VerifyIssuanceDependencyAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("PolicyDatabase");

        // Reject a missing/blank connection string.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The connection string for the PolicyDatabase is missing or blank.");
        }
        
        // Open a SqlConnection asynchronously.
        await using var connection = new SqlConnection(connectionString);
        Activity.Current?.SetTag("database.operation.stage", "open_connection");
        await connection.OpenAsync(cancellationToken);
        
        // Execute SELECT 1 asynchronously.
        await using var command = new SqlCommand("SELECT 1", connection)
        {
            // Bound the command timeout and dispose resources.
            CommandTimeout = 5 // Set an appropriate command timeout in seconds.
        };
        Activity.Current?.SetTag("database.operation.stage", "execute_query");
        await command.ExecuteScalarAsync(cancellationToken);
    }


    public async Task ValidateLineOfBusinessAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("PolicyDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The policy database connection string is missing.");
        }

        const string sql = """
            SELECT TOP (1) LOB
            FROM dbo.LineOfBusiness
            WHERE LOB = @lob;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 3
        };

        command.Parameters.Add("@lob", SqlDbType.VarChar, 100).Value = "Fire";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is not string lob ||
            !string.Equals(lob, "Fire", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The required line-of-business value 'Fire' is unavailable.");
        }
    }
}