using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class BankAccountSnelStartLinkRepository : IBankAccountSnelStartLinkRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<BankAccountSnelStartLinkRepository> _logger;

    public BankAccountSnelStartLinkRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<BankAccountSnelStartLinkRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<BankAccountSnelStartLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_GetById for link {LinkId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return Map(reader);
        }

        return null;
    }

    public async Task<BankAccountSnelStartLink?> GetByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_GetByBankAccountId for bank account {BankAccountId}",
            bankAccountId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_GetByBankAccountId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@BankAccountId", SqlDbType.UniqueIdentifier) { Value = bankAccountId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return Map(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<BankAccountSnelStartLink>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.BankAccountSnelStartLinks_GetActive");

        var result = new List<BankAccountSnelStartLink>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_GetActive", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<BankAccountSnelStartLink>> GetDueForAutoSyncAsync(
        DateTime dueUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_GetDueForAutoSync for due utc {DueUtc}",
            dueUtc);

        var result = new List<BankAccountSnelStartLink>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_GetDueForAutoSync", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@DueUtc", SqlDbType.DateTime2) { Value = dueUtc });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task CreateAsync(
        BankAccountSnelStartLink link,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_Insert for link {LinkId}",
            link.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, link);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        BankAccountSnelStartLink link,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_Update for link {LinkId}",
            link.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = link.Id });
        AddParameters(command, link, includeId: false);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAutoSyncScheduleAsync(
        Guid id,
        DateTime? lastRunUtc,
        DateTime? nextRunUtc,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_UpdateAutoSyncSchedule for link {LinkId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_UpdateAutoSyncSchedule", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@LastRunUtc", SqlDbType.DateTime2) { Value = (object?)lastRunUtc ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@NextRunUtc", SqlDbType.DateTime2) { Value = (object?)nextRunUtc ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.BankAccountSnelStartLinks_Delete for link {LinkId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSnelStartLinks_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
        SqlCommand command,
        BankAccountSnelStartLink link,
        bool includeId = true)
    {
        if (includeId)
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = link.Id });
        }

        command.Parameters.Add(new SqlParameter("@BankAccountId", SqlDbType.UniqueIdentifier) { Value = link.BankAccountId });
        command.Parameters.Add(new SqlParameter("@SnelStartAdministrationId", SqlDbType.UniqueIdentifier) { Value = link.SnelStartAdministrationId });
        command.Parameters.Add(new SqlParameter("@ExportFormat", SqlDbType.NVarChar, 50) { Value = link.ExportFormat.ToString() });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = link.IsActive });
        command.Parameters.Add(new SqlParameter("@AutoSyncEnabled", SqlDbType.Bit) { Value = link.AutoSyncEnabled });
        command.Parameters.Add(new SqlParameter("@SyncIntervalMinutes", SqlDbType.Int) { Value = link.SyncIntervalMinutes });
        command.Parameters.Add(new SqlParameter("@LastRunUtc", SqlDbType.DateTime2) { Value = (object?)link.LastRunUtc ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@NextRunUtc", SqlDbType.DateTime2) { Value = (object?)link.NextRunUtc ?? DBNull.Value });
    }

    private static BankAccountSnelStartLink Map(SqlDataReader reader)
    {
        return new BankAccountSnelStartLink
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            BankAccountId = reader.GetGuid(reader.GetOrdinal("BankAccountId")),
            SnelStartAdministrationId = reader.GetGuid(reader.GetOrdinal("SnelStartAdministrationId")),
            ExportFormat = Enum.Parse<SnelStartExportFormat>(reader.GetString(reader.GetOrdinal("ExportFormat")), ignoreCase: true),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            AutoSyncEnabled = reader.GetBoolean(reader.GetOrdinal("AutoSyncEnabled")),
            SyncIntervalMinutes = reader.GetInt32(reader.GetOrdinal("SyncIntervalMinutes")),
            LastRunUtc = reader.IsDBNull(reader.GetOrdinal("LastRunUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("LastRunUtc")),
            NextRunUtc = reader.IsDBNull(reader.GetOrdinal("NextRunUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("NextRunUtc")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}
