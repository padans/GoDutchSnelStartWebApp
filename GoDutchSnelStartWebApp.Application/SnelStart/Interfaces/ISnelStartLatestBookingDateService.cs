namespace GoDutchSnelStartWebApp.Application.SnelStart.Interfaces;

public interface ISnelStartLatestBookingDateService
{
    Task<DateTime?> GetLatestBookingDateAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);
}