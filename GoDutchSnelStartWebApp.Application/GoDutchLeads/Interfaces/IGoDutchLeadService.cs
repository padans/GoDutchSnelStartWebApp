using GoDutchSnelStartWebApp.Application.GoDutchLeads.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchLeads.Interfaces;

public interface IGoDutchLeadService
{
    Task CreateAsync(CreateGoDutchLeadRequest request, CancellationToken cancellationToken = default);
}
