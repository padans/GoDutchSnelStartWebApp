using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GoDutchSnelStartWebApp.Infrastructure.Persistence;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public SqlConnection CreateConnection(CancellationToken cancellationToken = default)
    {
        return new SqlConnection(_connectionString);
    }
}