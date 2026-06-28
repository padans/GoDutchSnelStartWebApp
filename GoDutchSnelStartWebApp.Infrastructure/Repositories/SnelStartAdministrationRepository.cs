using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class SnelStartAdministrationRepository : ISnelStartAdministrationRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<SnelStartAdministrationRepository> _logger;

    public SnelStartAdministrationRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<SnelStartAdministrationRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<SnelStartAdministration?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.SnelStartAdministrations_GetById for administration {AdministrationId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.SnelStartAdministrations_GetById", connection)
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

    public async Task<IReadOnlyList<SnelStartAdministration>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.SnelStartAdministrations_GetByTenantId for tenant {TenantId}",
            tenantId);

        var result = new List<SnelStartAdministration>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.SnelStartAdministrations_GetByTenantId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task CreateAsync(
        SnelStartAdministration administration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.SnelStartAdministrations_Insert for administration {AdministrationId}",
            administration.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.SnelStartAdministrations_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, administration);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        SnelStartAdministration administration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.SnelStartAdministrations_Update for administration {AdministrationId}",
            administration.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.SnelStartAdministrations_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = administration.Id });
        AddParameters(command, administration, includeId: false);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.SnelStartAdministrations_Delete for administration {AdministrationId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.SnelStartAdministrations_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(
        SqlCommand command,
        SnelStartAdministration administration,
        bool includeId = true)
    {
        if (includeId)
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = administration.Id });
        }

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = administration.TenantId });
        command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = administration.Name });
        command.Parameters.Add(new SqlParameter("@AdministrationClientKeyEncrypted", SqlDbType.NVarChar) { Value = administration.AdministrationClientKeyEncrypted });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = administration.IsActive });
    }

    private static SnelStartAdministration Map(SqlDataReader reader)
    {
        return new SnelStartAdministration
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            AdministrationClientKeyEncrypted = reader.GetString(reader.GetOrdinal("AdministrationClientKeyEncrypted")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}