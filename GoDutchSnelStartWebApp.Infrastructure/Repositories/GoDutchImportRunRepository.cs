using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class GoDutchImportRunRepository : IGoDutchImportRunRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<GoDutchImportRunRepository> _logger;

    public GoDutchImportRunRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<GoDutchImportRunRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<GoDutchImportRun?> GetLastSuccessfulByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP 1
    [Id],
    [TenantId],
    [BankAccountId],
    [Iban],
    [PeriodFromUtc],
    [PeriodToUtc],
    [TriggerSource],
    [Status],
    [TransactionCount],
    [RetryCount],
    [Message],
    [StartedUtc],
    [CompletedUtc]
FROM [dbo].[GoDutchImportRuns]
WHERE [BankAccountId] = @BankAccountId
  AND [Status] = 'Succeeded'
  AND [CompletedUtc] IS NOT NULL
ORDER BY [CompletedUtc] DESC;";

        _logger.LogDebug(
            "Ophalen laatste succesvolle GoDutch import run. BankAccountId: {BankAccountId}.",
            bankAccountId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.Add(new SqlParameter("@BankAccountId", SqlDbType.UniqueIdentifier)
        {
            Value = bankAccountId
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapImportRun(reader);
    }
    public async Task<GoDutchImportRun?> GetLastCompletedAsync(
    CancellationToken cancellationToken = default)
    {
        const string sql = @"
        SELECT TOP 1
            [Id],
            [TenantId],
            [BankAccountId],
            [Iban],
            [PeriodFromUtc],
            [PeriodToUtc],
            [TriggerSource],
            [Status],
            [TransactionCount],
            [RetryCount],
            [Message],
            [StartedUtc],
            [CompletedUtc]
        FROM [dbo].[GoDutchImportRuns]
        WHERE [Status] IN ('Succeeded', 'Skipped')
          AND [CompletedUtc] IS NOT NULL
        ORDER BY [CompletedUtc] DESC;";

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapImportRun(reader);
    }

    public async Task<GoDutchImportRun?> GetLastCompletedByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
        SELECT TOP 1
            [Id],
            [TenantId],
            [BankAccountId],
            [Iban],
            [PeriodFromUtc],
            [PeriodToUtc],
            [TriggerSource],
            [Status],
            [TransactionCount],
            [RetryCount],
            [Message],
            [StartedUtc],
            [CompletedUtc]
        FROM [dbo].[GoDutchImportRuns]
        WHERE [BankAccountId] = @BankAccountId
          AND [Status] IN ('Succeeded', 'Skipped')
          AND [CompletedUtc] IS NOT NULL
        ORDER BY [CompletedUtc] DESC;";

        _logger.LogDebug(
            "Ophalen laatste afgeronde GoDutch import run. BankAccountId: {BankAccountId}.",
            bankAccountId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.Add(new SqlParameter("@BankAccountId", SqlDbType.UniqueIdentifier)
        {
            Value = bankAccountId
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapImportRun(reader);
    }

    public async Task CreateAsync(
        GoDutchImportRun importRun,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
        INSERT INTO [dbo].[GoDutchImportRuns]
        (
            [Id],
            [TenantId],
            [BankAccountId],
            [Iban],
            [PeriodFromUtc],
            [PeriodToUtc],
            [TriggerSource],
            [Status],
            [TransactionCount],
            [RetryCount],
            [Message],
            [StartedUtc],
            [CompletedUtc]
        )
        VALUES
        (
            @Id,
            @TenantId,
            @BankAccountId,
            @Iban,
            @PeriodFromUtc,
            @PeriodToUtc,
            @TriggerSource,
            @Status,
            @TransactionCount,
            @RetryCount,
            @Message,
            @StartedUtc,
            @CompletedUtc
        );";

        _logger.LogDebug(
            "Aanmaken GoDutch import run. ImportRunId: {ImportRunId}, BankAccountId: {BankAccountId}, Status: {Status}.",
            importRun.Id,
            importRun.BankAccountId,
            importRun.Status);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        AddParameters(command, importRun);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        GoDutchImportRun importRun,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
        UPDATE [dbo].[GoDutchImportRuns]
        SET
            [TenantId] = @TenantId,
            [BankAccountId] = @BankAccountId,
            [Iban] = @Iban,
            [PeriodFromUtc] = @PeriodFromUtc,
            [PeriodToUtc] = @PeriodToUtc,
            [TriggerSource] = @TriggerSource,
            [Status] = @Status,
            [TransactionCount] = @TransactionCount,
            [RetryCount] = @RetryCount,
            [Message] = @Message,
            [StartedUtc] = @StartedUtc,
            [CompletedUtc] = @CompletedUtc
        WHERE [Id] = @Id;";

        _logger.LogDebug(
            "Bijwerken GoDutch import run. ImportRunId: {ImportRunId}, BankAccountId: {BankAccountId}, Status: {Status}.",
            importRun.Id,
            importRun.BankAccountId,
            importRun.Status);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        AddParameters(command, importRun);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }


    private static void AddParameters(SqlCommand command, GoDutchImportRun importRun)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier)
        {
            Value = importRun.Id
        });

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier)
        {
            Value = importRun.TenantId
        });

        command.Parameters.Add(new SqlParameter("@BankAccountId", SqlDbType.UniqueIdentifier)
        {
            Value = importRun.BankAccountId
        });

        command.Parameters.Add(new SqlParameter("@Iban", SqlDbType.NVarChar, 100)
        {
            Value = (object?)importRun.Iban ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@PeriodFromUtc", SqlDbType.DateTime2)
        {
            Value = importRun.PeriodFromUtc
        });

        command.Parameters.Add(new SqlParameter("@PeriodToUtc", SqlDbType.DateTime2)
        {
            Value = importRun.PeriodToUtc
        });

        command.Parameters.Add(new SqlParameter("@TriggerSource", SqlDbType.NVarChar, 100)
        {
            Value = importRun.TriggerSource.ToString()
        });

        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50)
        {
            Value = importRun.Status.ToString()
        });

        command.Parameters.Add(new SqlParameter("@TransactionCount", SqlDbType.Int)
        {
            Value = importRun.TransactionCount
        });

        command.Parameters.Add(new SqlParameter("@RetryCount", SqlDbType.Int)
        {
            Value = importRun.RetryCount
        });

        command.Parameters.Add(new SqlParameter("@Message", SqlDbType.NVarChar, -1)
        {
            Value = (object?)importRun.Message ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@StartedUtc", SqlDbType.DateTime2)
        {
            Value = importRun.StartedUtc
        });

        command.Parameters.Add(new SqlParameter("@CompletedUtc", SqlDbType.DateTime2)
        {
            Value = importRun.CompletedUtc ?? (object)DBNull.Value
        });
    }

    private static GoDutchImportRun MapImportRun(SqlDataReader reader)
    {
        return GoDutchImportRun.Reconstitute(
            id: reader.GetGuid(reader.GetOrdinal("Id")),
            tenantId: reader.GetGuid(reader.GetOrdinal("TenantId")),
            bankAccountId: reader.GetGuid(reader.GetOrdinal("BankAccountId")),
            iban: reader.IsDBNull(reader.GetOrdinal("Iban"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Iban")),
            periodFrom: reader.GetDateTime(reader.GetOrdinal("PeriodFromUtc")),
            periodTo: reader.GetDateTime(reader.GetOrdinal("PeriodToUtc")),
            triggerSource: reader.IsDBNull(reader.GetOrdinal("TriggerSource"))
                ? ImportRunTriggerSource.BackgroundWorker
                : Enum.Parse<ImportRunTriggerSource>(reader.GetString(reader.GetOrdinal("TriggerSource")), ignoreCase: true),
            status: reader.IsDBNull(reader.GetOrdinal("Status"))
                ? ImportRunStatus.Started
                : Enum.Parse<ImportRunStatus>(reader.GetString(reader.GetOrdinal("Status")), ignoreCase: true),
            transactionCount: reader.GetInt32(reader.GetOrdinal("TransactionCount")),
            retryCount: reader.IsDBNull(reader.GetOrdinal("RetryCount"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("RetryCount")),
            message: reader.IsDBNull(reader.GetOrdinal("Message"))
                ? null
                : reader.GetString(reader.GetOrdinal("Message")),
            startedUtc: reader.GetDateTime(reader.GetOrdinal("StartedUtc")),
            completedUtc: reader.IsDBNull(reader.GetOrdinal("CompletedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("CompletedUtc")));
    }
}