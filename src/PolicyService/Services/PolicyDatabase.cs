using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Data;
using PolicyService.Configuration;

namespace PolicyService.Services;

public sealed class PolicyDatabase(
    IConfiguration configuration,
    IOptions<PolicyRulesOptions> policyRules)
{
    private const string ConnectionStringName = "PolicyDatabase";

    private readonly PolicyRulesOptions _policyRules = policyRules.Value;

    // Single owner of connection acquisition for every database operation.
    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The connection string for the PolicyDatabase is missing or blank.");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task VerifyConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
    }

    public async Task VerifyIssuanceDependencyAsync(CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("database.operation.stage", "open_connection");
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("SELECT 1", connection)
        {
            CommandTimeout = 5
        };
        Activity.Current?.SetTag("database.operation.stage", "execute_query");
        await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task ValidateLineOfBusinessAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) LOB
            FROM dbo.LineOfBusiness
            WHERE LOB = @lob;
            """;

        var requiredLob = _policyRules.RequiredLineOfBusiness;

        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 3
        };

        command.Parameters.Add("@lob", SqlDbType.VarChar, 100).Value = requiredLob;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is not string lob ||
            !string.Equals(lob, requiredLob, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The required line-of-business value '{requiredLob}' is unavailable.");
        }
    }
}