using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Domain.Entities.SnelStart;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories.SnelStart;

public sealed class TenantSnelStartConnectionRepository : ITenantSnelStartConnectionRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<TenantSnelStartConnectionRepository> _logger;

    public TenantSnelStartConnectionRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<TenantSnelStartConnectionRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<TenantSnelStartConnection?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantSnelStartConnections_GetByTenantId for tenant {TenantId}",
            tenantId);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantSnelStartConnections_GetByTenantId", connection)
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

    public async Task<TenantSnelStartConnection?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantSnelStartConnections_GetById for connection {ConnectionId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantSnelStartConnections_GetById", connection)
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
        TenantSnelStartConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantSnelStartConnections_Insert for connection {ConnectionId}",
            connection.Id);

        await using var sqlConnection = _sqlConnectionFactory.CreateConnection();
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantSnelStartConnections_Insert", sqlConnection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        TenantSnelStartConnection connection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.TenantSnelStartConnections_Update for connection {ConnectionId}",
            connection.Id);

        await using var sqlConnection = _sqlConnectionFactory.CreateConnection();
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantSnelStartConnections_Update", sqlConnection)
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
            "Executing dbo.TenantSnelStartConnections_Delete for connection {ConnectionId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.TenantSnelStartConnections_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
        SqlCommand command,
        TenantSnelStartConnection connection,
        bool includeId = true,
        bool includeCreatedUtc = true)
    {
        if (includeId)
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = connection.Id });
        }

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = connection.TenantId });
        command.Parameters.Add(new SqlParameter("@ConnectionType", SqlDbType.NVarChar, 50) { Value = connection.ConnectionType.ToString() });
        command.Parameters.Add(new SqlParameter("@AuthUrl", SqlDbType.NVarChar, 500) { Value = connection.AuthUrl });
        command.Parameters.Add(new SqlParameter("@ApiBaseUrl", SqlDbType.NVarChar, 500) { Value = connection.ApiBaseUrl });
        command.Parameters.Add(new SqlParameter("@SubscriptionKeyEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.SubscriptionKeyEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ClientKeyEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.ClientKeyEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OAuthAccessTokenEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.OAuthAccessTokenEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OAuthRefreshTokenEncrypted", SqlDbType.NVarChar, -1) { Value = (object?)connection.OAuthRefreshTokenEncrypted ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OAuthExpiresUtc", SqlDbType.DateTime2) { Value = (object?)connection.OAuthExpiresUtc ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = connection.IsActive });

        if (includeCreatedUtc)
        {
            command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = connection.CreatedUtc });
        }

        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = (object?)connection.ModifiedUtc ?? DBNull.Value });
    }

    private static TenantSnelStartConnection Map(SqlDataReader reader)
    {
        return new TenantSnelStartConnection
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            ConnectionType = Enum.Parse<SnelStartConnectionType>(reader.GetString(reader.GetOrdinal("ConnectionType")), ignoreCase: true),
            AuthUrl = reader.GetString(reader.GetOrdinal("AuthUrl")),
            ApiBaseUrl = reader.GetString(reader.GetOrdinal("ApiBaseUrl")),
            SubscriptionKeyEncrypted = reader.IsDBNull(reader.GetOrdinal("SubscriptionKeyEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("SubscriptionKeyEncrypted")),
            ClientKeyEncrypted = reader.IsDBNull(reader.GetOrdinal("ClientKeyEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("ClientKeyEncrypted")),
            OAuthAccessTokenEncrypted = reader.IsDBNull(reader.GetOrdinal("OAuthAccessTokenEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("OAuthAccessTokenEncrypted")),
            OAuthRefreshTokenEncrypted = reader.IsDBNull(reader.GetOrdinal("OAuthRefreshTokenEncrypted"))
                ? null
                : reader.GetString(reader.GetOrdinal("OAuthRefreshTokenEncrypted")),
            OAuthExpiresUtc = reader.IsDBNull(reader.GetOrdinal("OAuthExpiresUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("OAuthExpiresUtc")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}
