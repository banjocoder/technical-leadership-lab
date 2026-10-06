using Microsoft.Data.SqlClient;

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
        await connection.OpenAsync(cancellationToken);
        
        // Execute SELECT 1 asynchronously.
        await using var command = new SqlCommand("SELECT 1", connection)
        {
            // Bound the command timeout and dispose resources.
            CommandTimeout = 5 // Set an appropriate command timeout in seconds.
        };
        await command.ExecuteScalarAsync(cancellationToken);
    }
}