using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories.MyPos;

public sealed class MyPosAutoSyncSettingsRepository : IMyPosAutoSyncSettingsRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<MyPosAutoSyncSettingsRepository> _logger;

    public MyPosAutoSyncSettingsRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<MyPosAutoSyncSettingsRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task<MyPosAutoSyncSettingsDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.MyPosAutoSyncSettings_Get");

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosAutoSyncSettings_Get", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MyPosAutoSyncSettingsDto
        {
            Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled")),
            IntervalMinutes = reader.GetInt32(reader.GetOrdinal("IntervalMinutes")),
            SyncMode = reader.IsDBNull(reader.GetOrdinal("SyncMode")) ? "Lookback" : reader.GetString(reader.GetOrdinal("SyncMode")),
            LookbackHours = reader.GetInt32(reader.GetOrdinal("LookbackHours")),
            PeriodType = reader.IsDBNull(reader.GetOrdinal("PeriodType")) ? "Day" : reader.GetString(reader.GetOrdinal("PeriodType"))
        };
    }

    public async Task UpsertAsync(MyPosAutoSyncSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing dbo.MyPosAutoSyncSettings_Upsert");

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.MyPosAutoSyncSettings_Upsert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Enabled", SqlDbType.Bit) { Value = settings.Enabled });
        command.Parameters.Add(new SqlParameter("@IntervalMinutes", SqlDbType.Int) { Value = settings.IntervalMinutes });
        command.Parameters.Add(new SqlParameter("@SyncMode", SqlDbType.NVarChar, 20) { Value = settings.SyncMode ?? "Lookback" });
        command.Parameters.Add(new SqlParameter("@LookbackHours", SqlDbType.Int) { Value = settings.LookbackHours });
        command.Parameters.Add(new SqlParameter("@PeriodType", SqlDbType.NVarChar, 20) { Value = settings.PeriodType ?? "Day" });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = DateTime.UtcNow });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
