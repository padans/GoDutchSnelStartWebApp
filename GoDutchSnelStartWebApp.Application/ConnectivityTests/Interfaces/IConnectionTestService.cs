using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;

namespace GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;

public interface IConnectionTestService
{
    Task<ConnectionTestResultDto> TestSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);
}
