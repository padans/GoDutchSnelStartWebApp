using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class AppUserRepository : IAppUserRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<AppUserRepository> _logger;

    public AppUserRepository(ISqlConnectionFactory sqlConnectionFactory, ILogger<AppUserRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<AppUser>();
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.AppUsers_GetAll", connection) { CommandType = CommandType.StoredProcedure };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            users.Add(Map(reader));
        return users;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.AppUsers_GetById", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.AppUsers_GetByUsername", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 100) { Value = username });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task CreateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.AppUsers_Insert", connection) { CommandType = CommandType.StoredProcedure };
        AddParameters(command, user);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("AppUser {UserId} inserted", user.Id);
    }

    public async Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.AppUsers_Update", connection) { CommandType = CommandType.StoredProcedure };
        AddParameters(command, user);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("AppUser {UserId} updated", user.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.AppUsers_Delete", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("AppUser {UserId} deleted", id);
    }

    private static void AddParameters(SqlCommand command, AppUser user)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = user.Id });
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 100) { Value = user.Username });
        command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = user.PasswordHash });
        command.Parameters.Add(new SqlParameter("@Module", SqlDbType.NVarChar, 50) { Value = user.Module.ToString() });
        command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = user.CreatedUtc });
    }

    private static AppUser Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
        Module = Enum.Parse<AppModule>(reader.GetString(reader.GetOrdinal("Module")), ignoreCase: true),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc"))
    };
}
