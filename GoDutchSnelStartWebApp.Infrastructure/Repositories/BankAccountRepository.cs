using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.ValueObjects;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<BankAccountRepository> _logger;

    public BankAccountRepository(ISqlConnectionFactory sqlConnectionFactory, ILogger<BankAccountRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BankAccount>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.BankAccounts_GetByTenantId for tenant {TenantId}", tenantId);

        var bankAccounts = new List<BankAccount>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccounts_GetByTenantId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            bankAccounts.Add(MapBankAccount(reader));
        }

        _logger.LogInformation("Retrieved {BankAccountCount} bank accounts for tenant {TenantId}", bankAccounts.Count, tenantId);

        return bankAccounts;
    }

    public async Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.BankAccounts_GetById for bank account {BankAccountId}", id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccounts_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return MapBankAccount(reader);
        }

        return null;
    }

    public async Task CreateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.BankAccounts_Insert for bank account {BankAccountId}", bankAccount.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccounts_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = bankAccount.Id });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = bankAccount.TenantId });
        command.Parameters.Add(new SqlParameter("@Iban", SqlDbType.NVarChar, 34) { Value = bankAccount.Iban });
        command.Parameters.Add(new SqlParameter("@AccountName", SqlDbType.NVarChar, 200) { Value = bankAccount.AccountName });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = bankAccount.IsActive });

        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)bankAccount.SnelStartGrootboek?.Id ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNummer", SqlDbType.NVarChar, 50)
        {
            Value = (object?)bankAccount.SnelStartGrootboek?.Nummer ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNaam", SqlDbType.NVarChar, 200)
        {
            Value = (object?)bankAccount.SnelStartGrootboek?.Naam ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartDagboekId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)bankAccount.SnelStartDagboek?.Id ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartDagboekCode", SqlDbType.NVarChar, 50)
        {
            Value = (object?)bankAccount.SnelStartDagboek?.Code ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartDagboekNaam", SqlDbType.NVarChar, 200)
        {
            Value = (object?)bankAccount.SnelStartDagboek?.Naam ?? DBNull.Value
        });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Bank account {BankAccountId} inserted successfully", bankAccount.Id);
    }

    public async Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.BankAccounts_Update for bank account {BankAccountId}", bankAccount.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccounts_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = bankAccount.Id });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = bankAccount.TenantId });
        command.Parameters.Add(new SqlParameter("@Iban", SqlDbType.NVarChar, 34) { Value = bankAccount.Iban });
        command.Parameters.Add(new SqlParameter("@AccountName", SqlDbType.NVarChar, 200) { Value = bankAccount.AccountName });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = bankAccount.IsActive });

        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)bankAccount.SnelStartGrootboek?.Id ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNummer", SqlDbType.NVarChar, 50)
        {
            Value = (object?)bankAccount.SnelStartGrootboek?.Nummer ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartGrootboekNaam", SqlDbType.NVarChar, 200)
        {
            Value = (object?)bankAccount.SnelStartGrootboek?.Naam ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartDagboekId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)bankAccount.SnelStartDagboek?.Id ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartDagboekCode", SqlDbType.NVarChar, 50)
        {
            Value = (object?)bankAccount.SnelStartDagboek?.Code ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@SnelStartDagboekNaam", SqlDbType.NVarChar, 200)
        {
            Value = (object?)bankAccount.SnelStartDagboek?.Naam ?? DBNull.Value
        });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Bank account {BankAccountId} updated successfully", bankAccount.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.BankAccounts_Delete for bank account {BankAccountId}", id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.BankAccounts_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Bank account {BankAccountId} deleted successfully", id);
    }

    private static BankAccount MapBankAccount(SqlDataReader reader)
    {
        return new BankAccount
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            TenantId = reader.GetGuid(reader.GetOrdinal("TenantId")),
            Iban = reader.GetString(reader.GetOrdinal("Iban")),
            AccountName = reader.GetString(reader.GetOrdinal("AccountName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),

            SnelStartGrootboek = reader.IsDBNull(reader.GetOrdinal("SnelStartGrootboekId"))
                ? null
                : new SnelStartGrootboekRef(
                    reader.GetGuid(reader.GetOrdinal("SnelStartGrootboekId")),
                    reader.GetString(reader.GetOrdinal("SnelStartGrootboekNummer")),
                    reader.GetString(reader.GetOrdinal("SnelStartGrootboekNaam"))),

            SnelStartDagboek = reader.IsDBNull(reader.GetOrdinal("SnelStartDagboekId"))
                ? null
                : new SnelStartDagboekRef(
                    reader.GetGuid(reader.GetOrdinal("SnelStartDagboekId")),
                    reader.GetString(reader.GetOrdinal("SnelStartDagboekCode")),
                    reader.GetString(reader.GetOrdinal("SnelStartDagboekNaam"))),

            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}