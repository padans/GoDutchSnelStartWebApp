using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class BankAccountSettingsRepository : IBankAccountSettingsRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<BankAccountSettingsRepository> _logger;

    public BankAccountSettingsRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<BankAccountSettingsRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<BankAccountSetting?> GetByBankAccountIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.BankAccountSettings_GetByBankAccountId for bank account {BankAccountId}", bankAccountId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSettings_GetByBankAccountId", connection)
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

    public async Task<BankAccountSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.BankAccountSettings_GetById for settings {SettingsId}", id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSettings_GetById", connection)
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

    public async Task CreateAsync(BankAccountSetting settings, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.BankAccountSettings_Insert for settings {SettingsId}", settings.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSettings_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, settings);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(BankAccountSetting settings, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.BankAccountSettings_Update for settings {SettingsId}", settings.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSettings_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = settings.Id });
        AddParameters(command, settings, includeId: false);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.BankAccountSettings_Delete for settings {SettingsId}", id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccountSettings_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(SqlCommand command, BankAccountSetting settings, bool includeId = true)
    {
        if (includeId)
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = settings.Id });
        }

        command.Parameters.Add(new SqlParameter("@BankAccountId", SqlDbType.UniqueIdentifier) { Value = settings.BankAccountId });
        // Stored procedures still declare these params; pass NULL until the DB columns are dropped.
        command.Parameters.Add(new SqlParameter("@GoDutchApiBaseUrl", SqlDbType.NVarChar, 500) { Value = DBNull.Value });
        command.Parameters.Add(new SqlParameter("@GoDutchUsername", SqlDbType.NVarChar, 200) { Value = DBNull.Value });
        command.Parameters.Add(new SqlParameter("@GoDutchPasswordEncrypted", SqlDbType.NVarChar) { Value = DBNull.Value });

        command.Parameters.Add(new SqlParameter("@SnelStartAuthUrl", SqlDbType.NVarChar, 500) { Value = (object?)settings.SnelStartAuthUrl ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartApiBaseUrl", SqlDbType.NVarChar, 500) { Value = (object?)settings.SnelStartApiBaseUrl ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartClientKey", SqlDbType.NVarChar) { Value = (object?)settings.SnelStartClientKey ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartSubscriptionKeyEncrypted", SqlDbType.NVarChar) { Value = (object?)settings.SnelStartSubscriptionKeyEncrypted ?? DBNull.Value });

        command.Parameters.Add(new SqlParameter("@ExportFormat", SqlDbType.NVarChar, 50) { Value = settings.ExportFormat.ToString() });
        command.Parameters.Add(new SqlParameter("@SyncEnabled", SqlDbType.Bit) { Value = settings.SyncEnabled });
    }

    private static BankAccountSetting Map(SqlDataReader reader)
    {
        return new BankAccountSetting
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            BankAccountId = reader.GetGuid(reader.GetOrdinal("BankAccountId")),

            SnelStartAuthUrl = reader.IsDBNull(reader.GetOrdinal("SnelStartAuthUrl"))
                ? null
                : reader.GetString(reader.GetOrdinal("SnelStartAuthUrl")),
            SnelStartApiBaseUrl = reader.IsDBNull(reader.GetOrdinal("SnelStartApiBaseUrl"))
                ? null
                : reader.GetString(reader.GetOrdinal("SnelStartApiBaseUrl")),
            SnelStartClientKey = reader.IsDBNull(reader.GetOrdinal("SnelStartClientKey"))
                ? null
                : reader.GetString(reader.GetOrdinal("SnelStartClientKey")),
            SnelStartSubscriptionKeyEncrypted = reader.IsDBNull(reader.GetOrdinal("SnelStartSubscriptionKeyEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("SnelStartSubscriptionKeyEncrypted")),

            ExportFormat = Enum.Parse<SnelStartExportFormat>(reader.GetString(reader.GetOrdinal("ExportFormat")), ignoreCase: true),
            SyncEnabled = reader.GetBoolean(reader.GetOrdinal("SyncEnabled")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}