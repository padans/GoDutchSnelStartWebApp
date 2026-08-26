using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public PasswordResetTokenRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.PasswordResetTokens_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = token.Id });
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = token.UserId });
        command.Parameters.Add(new SqlParameter("@Token", SqlDbType.NVarChar, 128) { Value = token.Token });
        command.Parameters.Add(new SqlParameter("@ExpiresUtc", SqlDbType.DateTime2) { Value = token.ExpiresUtc });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = token.CreatedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.PasswordResetTokens_GetByToken", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Token", SqlDbType.NVarChar, 128) { Value = token });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new PasswordResetToken
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                Token = reader.GetString(reader.GetOrdinal("Token")),
                ExpiresUtc = reader.GetDateTime(reader.GetOrdinal("ExpiresUtc")),
                UsedUtc = reader.IsDBNull(reader.GetOrdinal("UsedUtc")) ? null : reader.GetDateTime(reader.GetOrdinal("UsedUtc")),
                CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc"))
            };
        }

        return null;
    }

    public async Task MarkUsedAsync(Guid id, DateTime usedUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.PasswordResetTokens_MarkUsed", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@UsedUtc", SqlDbType.DateTime2) { Value = usedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
