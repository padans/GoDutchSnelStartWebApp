using System.Data;
using GoDutchSnelStartWebApp.Application.Notifications.Dtos;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<NotificationRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task InsertAsync(NotificationDto notification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.Notifications_Insert");

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Notifications_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = notification.Id });
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier)
        {
            Value = notification.TenantId.HasValue ? (object)notification.TenantId.Value : DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@Severity", SqlDbType.NVarChar, 20) { Value = notification.Severity });
        command.Parameters.Add(new SqlParameter("@Title", SqlDbType.NVarChar, 200) { Value = notification.Title });
        command.Parameters.Add(new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Value = notification.Message });
        command.Parameters.Add(new SqlParameter("@CreatedUtc", SqlDbType.DateTime2) { Value = notification.CreatedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Notifications_GetUnreadCount", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int count ? count : Convert.ToInt32(result);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Notifications_GetUnread", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<NotificationDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new NotificationDto
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("TenantId")),
                Severity = reader.GetString(reader.GetOrdinal("Severity")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Message = reader.GetString(reader.GetOrdinal("Message")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
                ReadUtc = reader.IsDBNull(reader.GetOrdinal("ReadUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ReadUtc"))
            });
        }

        return results;
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Notifications_MarkAsRead", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@ReadUtc", SqlDbType.DateTime2) { Value = DateTime.UtcNow });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.Notifications_MarkAllAsRead", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@ReadUtc", SqlDbType.DateTime2) { Value = DateTime.UtcNow });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
