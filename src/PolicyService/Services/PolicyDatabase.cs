using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Data;
using PolicyService.Configuration;
using PolicyService.Models;

namespace PolicyService.Services;

public sealed class PolicyDatabase(
    IConfiguration configuration,
    IOptions<PolicyRulesOptions> policyRules) : IPolicyRepository
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
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Failed to open database connection.", ex);
        }
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

    public async Task<Policy?> GetPolicyAsync(int policyId, CancellationToken cancellationToken)
    {
        // Query by the parameterized PolicyID.
        const string sql = """
        SELECT
            PolicyID,
            PolicyHolderName,
            Lob,
            CreatedAt,
            PolicyNumber,
            TermNumber,
            EffectiveDate,
            ExpirationDate
        FROM dbo.Policy
        WHERE PolicyID = @policyId;
        """;
        
        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 5
        };
        
        command.Parameters.Add("@policyId", SqlDbType.Int).Value = policyId;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        // SQL datetime2 has no offset; our storage convention is UTC.
        var createdAtUtc = DateTime.SpecifyKind(
            reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            DateTimeKind.Utc);

        // Return null when no row exists.

        // Map a found row to Policy.
        return new Policy(
            Id: reader.GetInt32(reader.GetOrdinal("PolicyID")),
            PolicyHolderName: reader.GetString(
                reader.GetOrdinal("PolicyHolderName")),
            LineOfBusiness: reader.GetString(reader.GetOrdinal("Lob")),
            CreatedAt: new DateTimeOffset(createdAtUtc),
            PolicyNumber: reader.GetString(reader.GetOrdinal("PolicyNumber")),
            TermNumber: reader.GetInt32(reader.GetOrdinal("TermNumber")),
            EffectiveDate: DateOnly.FromDateTime(
                reader.GetDateTime(reader.GetOrdinal("EffectiveDate"))),
            ExpirationDate: DateOnly.FromDateTime(
                reader.GetDateTime(reader.GetOrdinal("ExpirationDate"))));

        // Let database errors propagate rather than treating them as “not found.”
    }

    public async Task<CreatePolicyResponse> CreatePolicyAsync(
        CreatePolicyRequest request, 
        CancellationToken cancellationToken)
    {
        await using var connection = 
            await OpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(
            "dbo.IssuePolicy",
            connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 5
        };

        command.Parameters.Add("@PolicyHolderName",SqlDbType.NVarChar,100).Value = request.PolicyHolderName;
        command.Parameters.Add("@LineOfBusiness",SqlDbType.NVarChar,50).Value = request.LineOfBusiness;
        command.Parameters.Add("@RequestID",SqlDbType.UniqueIdentifier).Value = request.RequestId;
        command.Parameters.Add("@EffectiveDate",SqlDbType.Date).Value =request.EffectiveDate.ToDateTime(TimeOnly.MinValue);

        await using var reader = 
            await command.ExecuteReaderAsync(cancellationToken);

        if(!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Policy Issuance returned no policy record.");
        }

        var createdAtUtc = DateTime.SpecifyKind(
            reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            DateTimeKind.Utc);

        var policy = new Policy(
            Id: reader.GetInt32(reader.GetOrdinal("PolicyID")),
            PolicyHolderName: reader.GetString(
                reader.GetOrdinal("PolicyHolderName")),
            LineOfBusiness: reader.GetString(reader.GetOrdinal("Lob")),
            CreatedAt: new DateTimeOffset(createdAtUtc),
            PolicyNumber: reader.GetString(reader.GetOrdinal("PolicyNumber")),
            TermNumber: reader.GetInt32(reader.GetOrdinal("TermNumber")),
            EffectiveDate: DateOnly.FromDateTime(
                reader.GetDateTime(reader.GetOrdinal("EffectiveDate"))),
            ExpirationDate: DateOnly.FromDateTime(
                reader.GetDateTime(reader.GetOrdinal("ExpirationDate"))));

        return new CreatePolicyResponse(
            Policy: policy,
            IsNew: reader.GetBoolean(reader.GetOrdinal("IsNew"))
        );
    }
}