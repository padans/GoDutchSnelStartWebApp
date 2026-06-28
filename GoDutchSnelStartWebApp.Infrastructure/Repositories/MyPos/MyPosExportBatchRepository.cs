using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Domain.ValueObjects;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;



namespace GoDutchSnelStartWebApp.Infrastructure.Repositories.MyPos;

public sealed class MyPosExportBatchRepository : IMyPosExportBatchRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<MyPosExportBatchRepository> _logger;

    public MyPosExportBatchRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<MyPosExportBatchRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<MyPosExportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        MyPosExportBatch? batch = null;

        await using (var command = new SqlCommand("dbo.MyPosExportBatches_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        })
        {
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                batch = MapBatch(reader);
            }
        }

        if (batch is null)
        {
            return null;
        }

        batch.Lines = (await GetLinesAsync(connection, batch.Id, cancellationToken)).ToList();
        return batch;
    }

    public async Task<IReadOnlyList<MyPosExportBatch>> GetByTenantAsync(
    Guid tenantId,
    DateTime fromUtc,
    DateTime toUtc,
    CancellationToken cancellationToken = default)
    {
        var result = new List<MyPosExportBatch>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand("dbo.MyPosExportBatches_GetByTenant", connection)
        {
            CommandType = CommandType.StoredProcedure
        })
        {
            command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });
            command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc });
            command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(MapBatch(reader));
            }
        }

        // Pas ná het sluiten van de eerste DataReader de regels ophalen.
        foreach (var batch in result)
        {
            batch.Lines = (await GetLinesAsync(connection, batch.Id, cancellationToken)).ToList();
        }

        return result;
    }

    public async Task CreateAsync(MyPosExportBatch batch, CancellationToken cancellationToken = default)
    {
        if (batch is null)
        {
            throw new ArgumentNullException(nameof(batch));
        }

        _logger.LogInformation(
            "myPOS exportbatch opslaan gestart. BatchId: {BatchId}, TenantId: {TenantId}, Lines: {LineCount}.",
            batch.Id,
            batch.TenantId,
            batch.Lines.Count);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var command = new SqlCommand("dbo.MyPosExportBatches_Insert", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                AddBatchParameters(command, batch);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var line in batch.Lines)
            {
                await using var command = new SqlCommand("dbo.MyPosExportBatchLines_Insert", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };

                AddLineParameters(command, line);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateStatusAsync(
        Guid id,
        MyPosExportBatchStatus status,
        string? validationMessage,
        string? errorMessage,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosExportBatches_UpdateStatus", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = status.ToString() });
        command.Parameters.Add(new SqlParameter("@ValidationMessage", SqlDbType.NVarChar, 1000) { Value = DbValue(validationMessage) });
        command.Parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 2000) { Value = DbValue(errorMessage) });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<MyPosExportBatchLine>> GetLinesAsync(
        SqlConnection connection,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var result = new List<MyPosExportBatchLine>();

        await using var command = new SqlCommand("dbo.MyPosExportBatchLines_GetByBatchId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@BatchId", SqlDbType.UniqueIdentifier) { Value = batchId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapLine(reader));
        }

        return result;
    }

    private static void AddBatchParameters(SqlCommand command, MyPosExportBatch batch)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = batch.Id });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = batch.TenantId });
        command.Parameters.Add(new SqlParameter("@TenantMyPosConnectionId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)batch.TenantMyPosConnectionId ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@ExportTarget", SqlDbType.NVarChar, 50)
        {
            Value = batch.ExportTarget.ToString()
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankDagboekId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)batch.SnelStartBankDagboek?.Id ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankDagboekNummer", SqlDbType.NVarChar, 50)
        {
            Value = DbValue(batch.SnelStartBankDagboek?.Code)
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankDagboekNaam", SqlDbType.NVarChar, 500)
        {
            Value = DbValue(batch.SnelStartBankDagboek?.Naam)
        });

        command.Parameters.Add(new SqlParameter("@SnelStartBankIban", SqlDbType.NVarChar, 50)
        {
            Value = DbValue(batch.SnelStartBankIban)
        });

        command.Parameters.Add(new SqlParameter("@PeriodFromUtc", SqlDbType.DateTime2)
        {
            Value = batch.PeriodFromUtc
        });
        command.Parameters.Add(new SqlParameter("@PeriodToUtc", SqlDbType.DateTime2) { Value = batch.PeriodToUtc });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = batch.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@Currency", SqlDbType.NVarChar, 10) { Value = batch.Currency });
        command.Parameters.Add(new SqlParameter("@RawTransactionCount", SqlDbType.Int) { Value = batch.RawTransactionCount });
        command.Parameters.Add(new SqlParameter("@LineCount", SqlDbType.Int) { Value = batch.LineCount });
        command.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = batch.TotalAmount });
        command.Parameters.Add(new SqlParameter("@IsReadyForExport", SqlDbType.Bit) { Value = batch.IsReadyForExport });
        command.Parameters.Add(new SqlParameter("@ValidationMessage", SqlDbType.NVarChar, 1000) { Value = DbValue(batch.ValidationMessage) });
        command.Parameters.Add(new SqlParameter("@SnelStartReference", SqlDbType.NVarChar, 200) { Value = DbValue(batch.SnelStartReference) });
        command.Parameters.Add(new SqlParameter("@BookYear", SqlDbType.Int)
        {
            Value = (object?)batch.BookYear ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@PeriodFromLocalDate", SqlDbType.Date)
        {
            Value = (object?)batch.PeriodFromLocalDate?.Date ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@PeriodToLocalDate", SqlDbType.Date)
        {
            Value = (object?)batch.PeriodToLocalDate?.Date ?? DBNull.Value
        });

        command.Parameters.Add(new SqlParameter("@BookYearValidationMessage", SqlDbType.NVarChar, 1000)
        {
            Value = DbValue(batch.BookYearValidationMessage)
        });
        command.Parameters.Add(new SqlParameter("@ExportedUtc", SqlDbType.DateTime2) { Value = (object?)batch.ExportedUtc ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 2000) { Value = DbValue(batch.ErrorMessage) });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = batch.CreatedUtc });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = (object?)batch.ModifiedUtc ?? DBNull.Value });
    }

    private static void AddLineParameters(SqlCommand command, MyPosExportBatchLine line)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = line.Id });
        command.Parameters.Add(new SqlParameter("@BatchId", SqlDbType.UniqueIdentifier) { Value = line.BatchId });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = line.TenantId });
        command.Parameters.Add(new SqlParameter("@TransactionType", SqlDbType.NVarChar, 50) { Value = line.TransactionType });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 500) { Value = line.Description });
        command.Parameters.Add(new SqlParameter("@TransactionCount", SqlDbType.Int) { Value = line.TransactionCount });
        command.Parameters.Add(new SqlParameter("@TotalAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = line.TotalAmount });
        command.Parameters.Add(new SqlParameter("@Currency", SqlDbType.NVarChar, 10) { Value = line.Currency });
        command.Parameters.Add(new SqlParameter("@FirstTransactionUtc", SqlDbType.DateTime2) { Value = line.FirstTransactionUtc });
        command.Parameters.Add(new SqlParameter("@LastTransactionUtc", SqlDbType.DateTime2) { Value = line.LastTransactionUtc });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekId", SqlDbType.UniqueIdentifier) { Value = (object?)line.SnelStartGrootboek?.Id ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNummer", SqlDbType.NVarChar, 50) { Value = DbValue(line.SnelStartGrootboek?.Nummer) });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNaam", SqlDbType.NVarChar, 500) { Value = DbValue(line.SnelStartGrootboek?.Naam) });
        command.Parameters.Add(new SqlParameter("@BtwBerekening", SqlDbType.NVarChar, 30)
        {
            Value = string.IsNullOrWhiteSpace(line.BtwBerekening)
         ? "Geen"
         : line.BtwBerekening
        });

        command.Parameters.Add(new SqlParameter("@BtwSoort", SqlDbType.NVarChar, 30)
        {
            Value = DbValue(line.BtwSoort)
        });

        command.Parameters.Add(new SqlParameter("@BtwPercentage", SqlDbType.Decimal)
        {
            Precision = 9,
            Scale = 4,
            Value = (object?)line.BtwPercentage ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@HasMapping", SqlDbType.Bit) { Value = line.HasMapping });
        command.Parameters.Add(new SqlParameter("@HasActiveMapping", SqlDbType.Bit) { Value = line.HasActiveMapping });
        command.Parameters.Add(new SqlParameter("@IsReadyForExport", SqlDbType.Bit) { Value = line.IsReadyForExport });
        command.Parameters.Add(new SqlParameter("@MappingWarning", SqlDbType.NVarChar, 1000) { Value = DbValue(line.MappingWarning) });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = line.CreatedUtc });
    }

    private static MyPosExportBatch MapBatch(SqlDataReader reader)
    {
        return new MyPosExportBatch
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            TenantMyPosConnectionId = reader.IsDBNull(reader.GetOrdinal("TenantMyPosConnectionId"))
                ? null
                : reader.GetGuid(reader.GetOrdinal("TenantMyPosConnectionId")),

            ExportTarget = Enum.TryParse<MyPosExportTarget>(GetNullableString(reader, "ExportTarget"), out var et)
                ? et
                : MyPosExportTarget.SnelStartBankboek,

            SnelStartBankDagboek = reader.IsDBNull(reader.GetOrdinal("SnelStartBankDagboekId"))
                ? null
                : new SnelStartDagboekRef(
                    reader.GetGuid(reader.GetOrdinal("SnelStartBankDagboekId")),
                    GetNullableString(reader, "SnelStartBankDagboekNummer") ?? string.Empty,
                    GetNullableString(reader, "SnelStartBankDagboekNaam") ?? string.Empty),

            SnelStartBankIban = GetNullableString(reader, "SnelStartBankIban"),
            BookYear = reader.IsDBNull(reader.GetOrdinal("BookYear"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("BookYear")),

                        PeriodFromLocalDate = reader.IsDBNull(reader.GetOrdinal("PeriodFromLocalDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("PeriodFromLocalDate")),

                        PeriodToLocalDate = reader.IsDBNull(reader.GetOrdinal("PeriodToLocalDate"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("PeriodToLocalDate")),

            BookYearValidationMessage = GetNullableString(reader, "BookYearValidationMessage"),

            PeriodFromUtc = reader.GetDateTime(reader.GetOrdinal("PeriodFromUtc")),
            PeriodToUtc = reader.GetDateTime(reader.GetOrdinal("PeriodToUtc")),
            Status = Enum.Parse<MyPosExportBatchStatus>(reader.GetString(reader.GetOrdinal("Status")), ignoreCase: true),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            RawTransactionCount = reader.GetInt32(reader.GetOrdinal("RawTransactionCount")),
            LineCount = reader.GetInt32(reader.GetOrdinal("LineCount")),
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
            IsReadyForExport = reader.GetBoolean(reader.GetOrdinal("IsReadyForExport")),
            ValidationMessage = GetNullableString(reader, "ValidationMessage"),
            SnelStartReference = GetNullableString(reader, "SnelStartReference"),
            ExportedUtc = reader.IsDBNull(reader.GetOrdinal("ExportedUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("ExportedUtc")),
            ErrorMessage = GetNullableString(reader, "ErrorMessage"),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }

    private static MyPosExportBatchLine MapLine(SqlDataReader reader)
    {
        return new MyPosExportBatchLine
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            BatchId = reader.GetGuid(reader.GetOrdinal("BatchId")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            TransactionType = reader.GetString(reader.GetOrdinal("TransactionType")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            TransactionCount = reader.GetInt32(reader.GetOrdinal("TransactionCount")),
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            FirstTransactionUtc = reader.GetDateTime(reader.GetOrdinal("FirstTransactionUtc")),
            LastTransactionUtc = reader.GetDateTime(reader.GetOrdinal("LastTransactionUtc")),
            SnelStartGrootboek = reader.IsDBNull(reader.GetOrdinal("SnelStartGrootboekId"))
                ? null
                : new SnelStartGrootboekRef(
                    reader.GetGuid(reader.GetOrdinal("SnelStartGrootboekId")),
                    GetNullableString(reader, "SnelStartGrootboekNummer") ?? string.Empty,
                    GetNullableString(reader, "SnelStartGrootboekNaam") ?? string.Empty),
            BtwBerekening = GetNullableString(reader, "BtwBerekening") ?? "Geen",
            BtwSoort = GetNullableString(reader, "BtwSoort"),
            BtwPercentage = reader.IsDBNull(reader.GetOrdinal("BtwPercentage"))
            ? null
            : reader.GetDecimal(reader.GetOrdinal("BtwPercentage")),
            HasMapping = reader.GetBoolean(reader.GetOrdinal("HasMapping")),
            HasActiveMapping = reader.GetBoolean(reader.GetOrdinal("HasActiveMapping")),
            IsReadyForExport = reader.GetBoolean(reader.GetOrdinal("IsReadyForExport")),
            MappingWarning = GetNullableString(reader, "MappingWarning"),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc"))
        };
    }

    public async Task MarkExportedAsync(
    Guid batchId,
    string snelStartReference,
    DateTime exportedUtc,
    CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosExportBatches_MarkExported", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier)
        {
            Value = batchId
        });

        command.Parameters.Add(new SqlParameter("@SnelStartReference", SqlDbType.NVarChar, 200)
        {
            Value = snelStartReference
        });

        command.Parameters.Add(new SqlParameter("@ExportedUtc", SqlDbType.DateTime2)
        {
            Value = exportedUtc
        });

        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2)
        {
            Value = DateTime.UtcNow
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkExportFailedAsync(
        Guid batchId,
        string errorMessage,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosExportBatches_MarkExportFailed", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier)
        {
            Value = batchId
        });

        command.Parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 2000)
        {
            Value = errorMessage
        });

        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2)
        {
            Value = modifiedUtc
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? GetNullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
}
