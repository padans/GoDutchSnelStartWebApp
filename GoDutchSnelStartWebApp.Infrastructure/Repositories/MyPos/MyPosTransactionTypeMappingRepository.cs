using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.ValueObjects;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories.MyPos;

public sealed class MyPosTransactionTypeMappingRepository : IMyPosTransactionTypeMappingRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<MyPosTransactionTypeMappingRepository> _logger;

    public MyPosTransactionTypeMappingRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<MyPosTransactionTypeMappingRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<MyPosTransactionTypeMapping?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.MyPosTransactionTypeMappings_GetById for mapping {MappingId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosTransactionTypeMappings_GetById", connection)
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

    public async Task<MyPosTransactionTypeMapping?> GetByCodeAsync(
        Guid tenantId,
        string transactionCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.MyPosTransactionTypeMappings_GetByCode for tenant {TenantId}, code {TransactionCode}",
            tenantId,
            transactionCode);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosTransactionTypeMappings_GetByCode", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });
        command.Parameters.Add(new SqlParameter("@TransactionCode", SqlDbType.NVarChar, 50) { Value = transactionCode });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return Map(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<MyPosTransactionTypeMapping>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.MyPosTransactionTypeMappings_GetByTenant for tenant {TenantId}",
            tenantId);

        var result = new List<MyPosTransactionTypeMapping>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosTransactionTypeMappings_GetByTenant", connection)
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

    public async Task UpsertAsync(
        MyPosTransactionTypeMapping mapping,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.MyPosTransactionTypeMappings_Upsert for tenant {TenantId}, code {TransactionCode}",
            mapping.TenantId,
            mapping.TransactionCode);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosTransactionTypeMappings_Upsert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, mapping);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing dbo.MyPosTransactionTypeMappings_Delete for mapping {MappingId}",
            id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosTransactionTypeMappings_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(SqlCommand command, MyPosTransactionTypeMapping mapping)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = mapping.Id });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = mapping.TenantId });
        command.Parameters.Add(new SqlParameter("@TransactionCode", SqlDbType.NVarChar, 50) { Value = mapping.TransactionCode });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 500) { Value = mapping.Description });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekId", SqlDbType.UniqueIdentifier) { Value = (object?)mapping.SnelStartGrootboek?.Id ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNummer", SqlDbType.NVarChar, 50) { Value = (object?)mapping.SnelStartGrootboek?.Nummer ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNaam", SqlDbType.NVarChar, 500) { Value = (object?)mapping.SnelStartGrootboek?.Naam ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@BtwBerekening", SqlDbType.NVarChar, 30)
        {
            Value = string.IsNullOrWhiteSpace(mapping.BtwBerekening)
        ? "Geen"
        : mapping.BtwBerekening
        });

        command.Parameters.Add(new SqlParameter("@BtwSoort", SqlDbType.NVarChar, 30)
        {
            Value = string.IsNullOrWhiteSpace(mapping.BtwSoort)
                ? DBNull.Value
                : mapping.BtwSoort
        });

        command.Parameters.Add(new SqlParameter("@BtwPercentage", SqlDbType.Decimal)
        {
            Precision = 9,
            Scale = 4,
            Value = (object?)mapping.BtwPercentage ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = mapping.IsActive });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = mapping.CreatedUtc });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = (object?)mapping.ModifiedUtc ?? DBNull.Value });
    }

    private static MyPosTransactionTypeMapping Map(SqlDataReader reader)
    {
        return new MyPosTransactionTypeMapping
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            TransactionCode = reader.GetString(reader.GetOrdinal("TransactionCode")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            SnelStartGrootboek = reader.IsDBNull(reader.GetOrdinal("SnelStartGrootboekId"))
                ? null
                : new SnelStartGrootboekRef(
                    reader.GetGuid(reader.GetOrdinal("SnelStartGrootboekId")),
                    reader.IsDBNull(reader.GetOrdinal("SnelStartGrootboekNummer"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("SnelStartGrootboekNummer")),
                    reader.IsDBNull(reader.GetOrdinal("SnelStartGrootboekNaam"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("SnelStartGrootboekNaam"))),
            BtwBerekening = reader.IsDBNull(reader.GetOrdinal("BtwBerekening"))
                ? "Geen"
                : reader.GetString(reader.GetOrdinal("BtwBerekening")),

            BtwSoort = reader.IsDBNull(reader.GetOrdinal("BtwSoort"))
            ? null
            : reader.GetString(reader.GetOrdinal("BtwSoort")),

            BtwPercentage = reader.IsDBNull(reader.GetOrdinal("BtwPercentage"))
            ? null
            : reader.GetDecimal(reader.GetOrdinal("BtwPercentage")),

            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),

            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}
