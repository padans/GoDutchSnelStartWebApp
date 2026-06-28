using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class TenantGoDutchConnectionRepository : ITenantGoDutchConnectionRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<TenantGoDutchConnectionRepository> _logger;

    public TenantGoDutchConnectionRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<TenantGoDutchConnectionRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<TenantGoDutchConnection?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantGoDutchConnections_GetByTenantId for tenant {TenantId}",
            tenantId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantGoDutchConnections_GetByTenantId", connection)
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

    public async Task<TenantGoDutchConnection?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantGoDutchConnections_GetById for connection {ConnectionId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantGoDutchConnections_GetById", connection)
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
        TenantGoDutchConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantGoDutchConnections_Insert for connection {ConnectionId}",
            connection.Id);

        await using var sqlConnection = _sqlConnectionFactory.CreateConnection();
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantGoDutchConnections_Insert", sqlConnection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, connection);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant GoDutch connection {ConnectionId} inserted successfully for tenant {TenantId}",
            connection.Id,
            connection.TenantId);
    }

    public async Task UpdateAsync(
        TenantGoDutchConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantGoDutchConnections_Update for connection {ConnectionId}",
            connection.Id);

        await using var sqlConnection = _sqlConnectionFactory.CreateConnection();
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantGoDutchConnections_Update", sqlConnection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = connection.Id });
        AddParameters(command, connection, includeId: false, includeCreatedUtc: false);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant GoDutch connection {ConnectionId} updated successfully for tenant {TenantId}",
            connection.Id,
            connection.TenantId);
    }

    public async Task DeleteAsync(
        Guid id,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantGoDutchConnections_Delete for connection {ConnectionId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantGoDutchConnections_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant GoDutch connection {ConnectionId} disabled successfully",
            id);
    }

    private static void AddParameters(
        SqlCommand command,
        TenantGoDutchConnection connection,
        bool includeId = true,
        bool includeCreatedUtc = true)
    {
        if (includeId)
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = connection.Id });
        }

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = connection.TenantId });
        command.Parameters.Add(new SqlParameter("@ApiBaseUrl", SqlDbType.NVarChar, 500) { Value = connection.ApiBaseUrl });
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 256) { Value = connection.Username });
        command.Parameters.Add(new SqlParameter("@PasswordEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.PasswordEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = connection.IsActive });

        if (includeCreatedUtc)
        {
            command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = connection.CreatedUtc });
        }

        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = (object?)connection.ModifiedUtc ?? DBNull.Value });
    }

    private static TenantGoDutchConnection Map(SqlDataReader reader)
    {
        return new TenantGoDutchConnection
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            ApiBaseUrl = reader.GetString(reader.GetOrdinal("ApiBaseUrl")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            PasswordEncrypted = reader.IsDBNull(reader.GetOrdinal("PasswordEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("PasswordEncrypted")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}
