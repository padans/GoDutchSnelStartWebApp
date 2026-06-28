using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.ValueObjects;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories.MyPos;

public sealed class TenantMyPosConnectionRepository : ITenantMyPosConnectionRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<TenantMyPosConnectionRepository> _logger;

    public TenantMyPosConnectionRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<TenantMyPosConnectionRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<TenantMyPosConnection?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantMyPosConnections_GetByTenantId for tenant {TenantId}",
            tenantId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantMyPosConnections_GetByTenantId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return Map(reader);
        }

        return null;
    }

    public async Task<TenantMyPosConnection?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantMyPosConnections_GetById for connection {ConnectionId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantMyPosConnections_GetById", connection)
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

    public async Task CreateAsync(
        TenantMyPosConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantMyPosConnections_Insert for connection {ConnectionId}",
            connection.Id);

        await using var sqlConnection = _sqlConnectionFactory.CreateConnection();
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantMyPosConnections_Insert", sqlConnection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        TenantMyPosConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantMyPosConnections_Update for connection {ConnectionId}",
            connection.Id);

        await using var sqlConnection = _sqlConnectionFactory.CreateConnection();
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantMyPosConnections_Update", sqlConnection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = connection.Id });
        AddParameters(command, connection, includeId: false, includeCreatedUtc: false);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantMyPosConnections_Delete for connection {ConnectionId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantMyPosConnections_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
        SqlCommand command,
        TenantMyPosConnection connection,
        bool includeId = true,
        bool includeCreatedUtc = true)
    {
        if (includeId)
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = connection.Id });
        }

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = connection.TenantId });
        command.Parameters.Add(new SqlParameter("@AuthUrl", SqlDbType.NVarChar, 500) { Value = connection.AuthUrl });
        command.Parameters.Add(new SqlParameter("@TransactionsApiBaseUrl", SqlDbType.NVarChar, 500) { Value = connection.TransactionsApiBaseUrl });
        command.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.NVarChar, 256) { Value = connection.ClientId });
        command.Parameters.Add(new SqlParameter("@ClientSecretEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.ClientSecretEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ApiKeyEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.ApiKeyEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartBankDagboekId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)connection.SnelStartBankDagboek?.Id ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankDagboekNummer", SqlDbType.NVarChar, 50)
        {
            Value = (object?)connection.SnelStartBankDagboek?.Code ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankDagboekNaam", SqlDbType.NVarChar, 500)
        {
            Value = (object?)connection.SnelStartBankDagboek?.Naam ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankIban", SqlDbType.NVarChar, 50)
        {
            Value = (object?)connection.SnelStartBankIban ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = connection.IsActive });

        if (includeCreatedUtc)
        {
            command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = connection.CreatedUtc });
        }

        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = (object?)connection.ModifiedUtc ?? DBNull.Value });
    }

    private static TenantMyPosConnection Map(SqlDataReader reader)
    {
        return new TenantMyPosConnection
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            AuthUrl = reader.GetString(reader.GetOrdinal("AuthUrl")),
            TransactionsApiBaseUrl = reader.GetString(reader.GetOrdinal("TransactionsApiBaseUrl")),
            ClientId = reader.GetString(reader.GetOrdinal("ClientId")),
            ClientSecretEncrypted = reader.IsDBNull(reader.GetOrdinal("ClientSecretEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("ClientSecretEncrypted")),
            ApiKeyEncrypted = reader.IsDBNull(reader.GetOrdinal("ApiKeyEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("ApiKeyEncrypted")),
            SnelStartBankDagboek = reader.IsDBNull(reader.GetOrdinal("SnelStartBankDagboekId"))
                ? null
                : new SnelStartDagboekRef(
                    reader.GetGuid(reader.GetOrdinal("SnelStartBankDagboekId")),
                    reader.IsDBNull(reader.GetOrdinal("SnelStartBankDagboekNummer"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("SnelStartBankDagboekNummer")),
                    reader.IsDBNull(reader.GetOrdinal("SnelStartBankDagboekNaam"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("SnelStartBankDagboekNaam"))),

            SnelStartBankIban = reader.IsDBNull(reader.GetOrdinal("SnelStartBankIban"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("SnelStartBankIban")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}
