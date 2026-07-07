using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface IGoDutchLeadRepository
{
    Task InsertAsync(GoDutchLead lead, CancellationToken cancellationToken = default);
}
