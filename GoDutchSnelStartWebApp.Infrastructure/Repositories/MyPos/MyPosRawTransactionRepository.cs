using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories.MyPos;

public sealed class MyPosRawTransactionRepository : IMyPosRawTransactionRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<MyPosRawTransactionRepository> _logger;

    public MyPosRawTransactionRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<MyPosRawTransactionRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<MyPosRawTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.MyPosRawTransactions_GetById", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<MyPosRawTransaction?> GetByMyPosTransactionIdAsync(Guid tenantId, long myPosTransactionId, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.MyPosRawTransactions_GetByMyPosTransactionId", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });
        command.Parameters.Add(new SqlParameter("@MyPosTransactionId", SqlDbType.BigInt) { Value = myPosTransactionId });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public Task<IReadOnlyList<MyPosRawTransaction>> GetByTenantAsync(Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        GetRangeAsync("dbo.MyPosRawTransactions_GetByTenant", tenantId, fromUtc, toUtc, cancellationToken);

    public Task<IReadOnlyList<MyPosRawTransaction>> GetUnexportedAsync(Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        GetRangeAsync("dbo.MyPosRawTransactions_GetUnexported", tenantId, fromUtc, toUtc, cancellationToken);

    public async Task<MyPosRawTransactionUpsertResultDto> UpsertRangeAsync(
    IEnumerable<MyPosRawTransaction> transactions,
    CancellationToken cancellationToken = default)
    {
        if (transactions is null)
        {
            throw new ArgumentNullException(nameof(transactions));
        }

        var transactionList = transactions.ToList();

        var result = new MyPosRawTransactionUpsertResultDto
        {
            InputCount = transactionList.Count
        };

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        foreach (var transaction in transactionList)
        {
            await using var command = new SqlCommand("dbo.MyPosRawTransactions_Upsert", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(command, transaction);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                result.InsertedCount += reader.GetInt32(reader.GetOrdinal("InsertedCount"));
                result.UpdatedCount += reader.GetInt32(reader.GetOrdinal("UpdatedCount"));
                result.SkippedCount += reader.GetInt32(reader.GetOrdinal("SkippedCount"));
            }
            else
            {
                result.SkippedCount++;
            }
        }

        _logger.LogInformation(
            "myPOS raw transactions upsert afgerond. Input: {InputCount}, Inserted: {InsertedCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}, DatabaseOperations: {DatabaseOperationCount}.",
            result.InputCount,
            result.InsertedCount,
            result.UpdatedCount,
            result.SkippedCount,
            result.DatabaseOperationCount);

        return result;
    }

    public async Task MarkExportedAsync(Guid exportBatchId, IEnumerable<long> myPosTransactionIds, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        foreach (var myPosTransactionId in myPosTransactionIds.Distinct())
        {
            await using var command = new SqlCommand("dbo.MyPosRawTransactions_MarkExported", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@ExportBatchId", SqlDbType.UniqueIdentifier) { Value = exportBatchId });
            command.Parameters.Add(new SqlParameter("@MyPosTransactionId", SqlDbType.BigInt) { Value = myPosTransactionId });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<MyPosRawTransaction>> GetRangeAsync(string procedure, Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var result = new List<MyPosRawTransaction>();
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });
        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc });
        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    private static void AddParameters(SqlCommand command, MyPosRawTransaction transaction)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = transaction.Id });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = transaction.TenantId });
        command.Parameters.Add(new SqlParameter("@TenantMyPosConnectionId", SqlDbType.UniqueIdentifier) { Value = transaction.TenantMyPosConnectionId });
        command.Parameters.Add(new SqlParameter("@MyPosTransactionId", SqlDbType.BigInt) { Value = transaction.MyPosTransactionId });
        command.Parameters.Add(new SqlParameter("@AccountNumber", SqlDbType.NVarChar, 100) { Value = DbValue(transaction.AccountNumber) });
        command.Parameters.Add(new SqlParameter("@Ruid", SqlDbType.NVarChar, 100) { Value = DbValue(transaction.Ruid) });
        command.Parameters.Add(new SqlParameter("@ReferenceNumberType", SqlDbType.Int) { Value = (object?)transaction.ReferenceNumberType ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@BillingDescriptor", SqlDbType.NVarChar, 256) { Value = DbValue(transaction.BillingDescriptor) });
        command.Parameters.Add(new SqlParameter("@PanMasked", SqlDbType.NVarChar, 64) { Value = DbValue(transaction.PanMasked) });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 512) { Value = DbValue(transaction.Description) });
        command.Parameters.Add(new SqlParameter("@PaymentReference", SqlDbType.NVarChar, 256) { Value = DbValue(transaction.PaymentReference) });
        command.Parameters.Add(new SqlParameter("@TransactionType", SqlDbType.NVarChar, 20) { Value = transaction.TransactionType });
        command.Parameters.Add(new SqlParameter("@TransactionCurrency", SqlDbType.NVarChar, 10) { Value = DbValue(transaction.TransactionCurrency) });
        command.Parameters.Add(new SqlParameter("@TransactionAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = transaction.TransactionAmount });
        command.Parameters.Add(new SqlParameter("@OriginalCurrency", SqlDbType.NVarChar, 10) { Value = DbValue(transaction.OriginalCurrency) });
        command.Parameters.Add(new SqlParameter("@OriginalAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = (object?)transaction.OriginalAmount ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@TransactionUtc", SqlDbType.DateTime2) { Value = transaction.TransactionUtc });
        command.Parameters.Add(new SqlParameter("@Sign", SqlDbType.NVarChar, 10) { Value = DbValue(transaction.Sign) });
        command.Parameters.Add(new SqlParameter("@ReferenceNumber", SqlDbType.NVarChar, 100) { Value = DbValue(transaction.ReferenceNumber) });
        command.Parameters.Add(new SqlParameter("@TerminalId", SqlDbType.NVarChar, 100) { Value = DbValue(transaction.TerminalId) });
        command.Parameters.Add(new SqlParameter("@SerialNumber", SqlDbType.NVarChar, 100) { Value = DbValue(transaction.SerialNumber) });
        command.Parameters.Add(new SqlParameter("@RequestId", SqlDbType.NVarChar, 100) { Value = transaction.RequestId });
        command.Parameters.Add(new SqlParameter("@RawJson", SqlDbType.NVarChar, -1) { Value = transaction.RawJson });
        command.Parameters.Add(new SqlParameter("@ImportedUtc", SqlDbType.DateTime2) { Value = transaction.ImportedUtc });
        command.Parameters.Add(new SqlParameter("@IsExported", SqlDbType.Bit) { Value = transaction.IsExported });
        command.Parameters.Add(new SqlParameter("@ExportBatchId", SqlDbType.UniqueIdentifier) { Value = (object?)transaction.ExportBatchId ?? DBNull.Value });
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    private static MyPosRawTransaction Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
        TenantMyPosConnectionId = reader.GetGuid(reader.GetOrdinal("TenantMyPosConnectionId")),
        MyPosTransactionId = reader.GetInt64(reader.GetOrdinal("MyPosTransactionId")),
        AccountNumber = GetNullableString(reader, "AccountNumber"),
        Ruid = GetNullableString(reader, "Ruid"),
        ReferenceNumberType = reader.IsDBNull(reader.GetOrdinal("ReferenceNumberType")) ? null : reader.GetInt32(reader.GetOrdinal("ReferenceNumberType")),
        BillingDescriptor = GetNullableString(reader, "BillingDescriptor"),
        PanMasked = GetNullableString(reader, "PanMasked"),
        Description = GetNullableString(reader, "Description"),
        PaymentReference = GetNullableString(reader, "PaymentReference"),
        TransactionType = reader.GetString(reader.GetOrdinal("TransactionType")),
        TransactionCurrency = GetNullableString(reader, "TransactionCurrency"),
        TransactionAmount = reader.GetDecimal(reader.GetOrdinal("TransactionAmount")),
        OriginalCurrency = GetNullableString(reader, "OriginalCurrency"),
        OriginalAmount = reader.IsDBNull(reader.GetOrdinal("OriginalAmount")) ? null : reader.GetDecimal(reader.GetOrdinal("OriginalAmount")),
        TransactionUtc = reader.GetDateTime(reader.GetOrdinal("TransactionUtc")),
        Sign = GetNullableString(reader, "Sign"),
        ReferenceNumber = GetNullableString(reader, "ReferenceNumber"),
        TerminalId = GetNullableString(reader, "TerminalId"),
        SerialNumber = GetNullableString(reader, "SerialNumber"),
        RequestId = reader.GetString(reader.GetOrdinal("RequestId")),
        RawJson = reader.GetString(reader.GetOrdinal("RawJson")),
        ImportedUtc = reader.GetDateTime(reader.GetOrdinal("ImportedUtc")),
        IsExported = reader.GetBoolean(reader.GetOrdinal("IsExported")),
        ExportBatchId = reader.IsDBNull(reader.GetOrdinal("ExportBatchId")) ? null : reader.GetGuid(reader.GetOrdinal("ExportBatchId"))
    };

    private static string? GetNullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public async Task<int> MarkExportedForBatchAsync(
    Guid tenantId,
    Guid exportBatchId,
    DateTime fromUtc,
    DateTime toUtc,
    DateTime importedUtc,
    CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.MyPosRawTransactions_MarkExportedForBatch";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier)
        {
            Value = tenantId
        });

        command.Parameters.Add(new SqlParameter("@ExportBatchId", SqlDbType.UniqueIdentifier)
        {
            Value = exportBatchId
        });

        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTime2)
        {
            Value = fromUtc
        });

        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTime2)
        {
            Value = toUtc
        });

        command.Parameters.Add(new SqlParameter("@ImportedUtc", SqlDbType.DateTime2)
        {
            Value = importedUtc
        });

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctTransactionTypesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.MyPosRawTransactions_GetDistinctTransactionTypesByTenant", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(type))
                result.Add(type);
        }
        return result;
    }
}
