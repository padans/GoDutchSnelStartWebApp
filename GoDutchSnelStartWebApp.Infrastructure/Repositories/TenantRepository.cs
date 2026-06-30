using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<TenantRepository> _logger;

    public TenantRepository(ISqlConnectionFactory sqlConnectionFactory, ILogger<TenantRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.Tenants_GetAll");

        var tenants = new List<Tenant>();

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Tenants_GetAll", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            tenants.Add(MapTenant(reader));
        }

        _logger.LogInformation("Stored procedure dbo.Tenants_GetAll returned {TenantCount} tenants", tenants.Count);

        return tenants;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.Tenants_GetById for tenant {TenantId}", id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Tenants_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            _logger.LogInformation("Tenant {TenantId} found", id);
            return MapTenant(reader);
        }

        _logger.LogWarning("Tenant {TenantId} not found in repository", id);
        return null;
    }

    public async Task CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.Tenants_Insert for tenant {TenantId}", tenant.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Tenants_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = tenant.Id });
        command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = tenant.Name });
        command.Parameters.Add(new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 100) { Value = (object?)tenant.CustomerCode ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@CompanyName", SqlDbType.NVarChar, 200) { Value = (object?)tenant.CompanyName ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ContactName", SqlDbType.NVarChar, 200) { Value = (object?)tenant.ContactName ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = (object?)tenant.Email ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Phone", SqlDbType.NVarChar, 50) { Value = (object?)tenant.Phone ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)tenant.Address ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@PostalCode", SqlDbType.NVarChar, 20) { Value = (object?)tenant.PostalCode ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = (object?)tenant.City ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@KvkNumber", SqlDbType.NVarChar, 20) { Value = (object?)tenant.KvkNumber ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@DefaultIban", SqlDbType.NVarChar, 34) { Value = (object?)tenant.DefaultIban ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@GoDutchEnabled", SqlDbType.Bit) { Value = tenant.GoDutchEnabled });
        command.Parameters.Add(new SqlParameter("@MyPosEnabled", SqlDbType.Bit) { Value = tenant.MyPosEnabled });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = tenant.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = tenant.IsActive });
        command.Parameters.Add(new SqlParameter("@TrialStartsUtc", SqlDbType.DateTime2) { Value = tenant.TrialStartsUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@TrialEndsUtc", SqlDbType.DateTime2) { Value = tenant.TrialEndsUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OnboardingCompletedUtc", SqlDbType.DateTime2) { Value = tenant.OnboardingCompletedUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = tenant.CreatedUtc });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = tenant.ModifiedUtc ?? (object)DBNull.Value });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} inserted successfully", tenant.Id);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.Tenants_Update for tenant {TenantId}", tenant.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Tenants_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = tenant.Id });
        command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = tenant.Name });
        command.Parameters.Add(new SqlParameter("@CustomerCode", SqlDbType.NVarChar, 100) { Value = (object?)tenant.CustomerCode ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@CompanyName", SqlDbType.NVarChar, 200) { Value = (object?)tenant.CompanyName ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ContactName", SqlDbType.NVarChar, 200) { Value = (object?)tenant.ContactName ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = (object?)tenant.Email ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Phone", SqlDbType.NVarChar, 50) { Value = (object?)tenant.Phone ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 200) { Value = (object?)tenant.Address ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@PostalCode", SqlDbType.NVarChar, 20) { Value = (object?)tenant.PostalCode ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = (object?)tenant.City ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@KvkNumber", SqlDbType.NVarChar, 20) { Value = (object?)tenant.KvkNumber ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@DefaultIban", SqlDbType.NVarChar, 34) { Value = (object?)tenant.DefaultIban ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@GoDutchEnabled", SqlDbType.Bit) { Value = tenant.GoDutchEnabled });
        command.Parameters.Add(new SqlParameter("@MyPosEnabled", SqlDbType.Bit) { Value = tenant.MyPosEnabled });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = tenant.Status.ToString() });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = tenant.IsActive });
        command.Parameters.Add(new SqlParameter("@TrialStartsUtc", SqlDbType.DateTime2) { Value = tenant.TrialStartsUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@TrialEndsUtc", SqlDbType.DateTime2) { Value = tenant.TrialEndsUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OnboardingCompletedUtc", SqlDbType.DateTime2) { Value = tenant.OnboardingCompletedUtc ?? (object)DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = tenant.ModifiedUtc ?? (object)DBNull.Value });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} updated successfully", tenant.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing stored procedure dbo.Tenants_Delete for tenant {TenantId}", id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Tenants_Delete", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} deleted successfully", id);
    }

    private static Tenant MapTenant(SqlDataReader reader)
    {
        return new Tenant
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            CustomerCode = reader.IsDBNull(reader.GetOrdinal("CustomerCode"))
                ? null
                : reader.GetString(reader.GetOrdinal("CustomerCode")),
            CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName"))
                ? null
                : reader.GetString(reader.GetOrdinal("CompanyName")),
            ContactName = reader.IsDBNull(reader.GetOrdinal("ContactName"))
                ? null
                : reader.GetString(reader.GetOrdinal("ContactName")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                ? null
                : reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone"))
                ? null
                : reader.GetString(reader.GetOrdinal("Phone")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                ? null
                : reader.GetString(reader.GetOrdinal("Address")),
            PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode"))
                ? null
                : reader.GetString(reader.GetOrdinal("PostalCode")),
            City = reader.IsDBNull(reader.GetOrdinal("City"))
                ? null
                : reader.GetString(reader.GetOrdinal("City")),
            KvkNumber = reader.IsDBNull(reader.GetOrdinal("KvkNumber"))
                ? null
                : reader.GetString(reader.GetOrdinal("KvkNumber")),
            DefaultIban = reader.IsDBNull(reader.GetOrdinal("DefaultIban"))
                ? null
                : reader.GetString(reader.GetOrdinal("DefaultIban")),
            GoDutchEnabled = reader.GetBoolean(reader.GetOrdinal("GoDutchEnabled")),
            MyPosEnabled = reader.GetBoolean(reader.GetOrdinal("MyPosEnabled")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                ? TenantStatus.Draft
                : Enum.Parse<TenantStatus>(reader.GetString(reader.GetOrdinal("Status")), ignoreCase: true),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            TrialStartsUtc = reader.IsDBNull(reader.GetOrdinal("TrialStartsUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("TrialStartsUtc")),
            TrialEndsUtc = reader.IsDBNull(reader.GetOrdinal("TrialEndsUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("TrialEndsUtc")),
            OnboardingCompletedUtc = reader.IsDBNull(reader.GetOrdinal("OnboardingCompletedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("OnboardingCompletedUtc")),
            CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
            ModifiedUtc = reader.IsDBNull(reader.GetOrdinal("ModifiedUtc"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ModifiedUtc"))
        };
    }
}