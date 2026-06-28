using Microsoft.Data.SqlClient;

namespace GoDutchSnelStartWebApp.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection(CancellationToken cancellationToken = default);
}