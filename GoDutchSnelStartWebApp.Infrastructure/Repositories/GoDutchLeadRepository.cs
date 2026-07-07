using System.Data;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.Repositories;

public sealed class GoDutchLeadRepository : IGoDutchLeadRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<GoDutchLeadRepository> _logger;

    public GoDutchLeadRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        ILogger<GoDutchLeadRepository> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _logger = logger;
    }

    public async Task InsertAsync(GoDutchLead lead, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GoDutchLead opslaan: {Id}", lead.Id);

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.GoDutchLeads_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Id",                  SqlDbType.UniqueIdentifier) { Value = lead.Id });
        command.Parameters.Add(new SqlParameter("@BedrijfsNaam",        SqlDbType.NVarChar, 200)    { Value = lead.BedrijfsNaam });
        command.Parameters.Add(new SqlParameter("@ContactPersoon",      SqlDbType.NVarChar, 200)    { Value = lead.ContactPersoon });
        command.Parameters.Add(new SqlParameter("@Email",               SqlDbType.NVarChar, 200)    { Value = lead.Email });
        command.Parameters.Add(new SqlParameter("@Telefoon",            SqlDbType.NVarChar, 50)     { Value = (object?)lead.Telefoon ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@AantalBankrekeningen",SqlDbType.Int)              { Value = (object?)lead.AantalBankrekeningen ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Status",              SqlDbType.NVarChar, 50)     { Value = lead.Status });
        command.Parameters.Add(new SqlParameter("@CreatedUtc",          SqlDbType.DateTime2)        { Value = lead.CreatedUtc });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
