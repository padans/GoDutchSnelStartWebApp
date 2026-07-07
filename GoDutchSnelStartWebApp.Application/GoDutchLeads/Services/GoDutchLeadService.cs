using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.GoDutchLeads.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchLeads.Interfaces;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.GoDutchLeads.Services;

public sealed class GoDutchLeadService : IGoDutchLeadService
{
    private readonly IGoDutchLeadRepository _repository;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<GoDutchLeadService> _logger;

    public GoDutchLeadService(
        IGoDutchLeadRepository repository,
        IEmailNotificationService emailService,
        ILogger<GoDutchLeadService> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task CreateAsync(CreateGoDutchLeadRequest request, CancellationToken cancellationToken = default)
    {
        var lead = new GoDutchLead
        {
            Id             = Guid.NewGuid(),
            BedrijfsNaam   = request.BedrijfsNaam.Trim(),
            ContactPersoon = request.ContactPersoon.Trim(),
            Email          = request.Email.Trim(),
            Telefoon       = string.IsNullOrWhiteSpace(request.Telefoon) ? null : request.Telefoon.Trim(),
            AantalBankrekeningen = request.AantalBankrekeningen,
            Status         = "Nieuw",
            CreatedUtc     = DateTime.UtcNow
        };

        await _repository.InsertAsync(lead, cancellationToken);

        _logger.LogInformation("Nieuwe GoDutch lead aangemeld: {BedrijfsNaam} ({Email})", lead.BedrijfsNaam, lead.Email);

        var body = $"""
            Nieuwe aanmelding via de GoDutch landingspagina:

            Bedrijfsnaam:        {lead.BedrijfsNaam}
            Contactpersoon:      {lead.ContactPersoon}
            E-mail:              {lead.Email}
            Telefoon:            {lead.Telefoon ?? "-"}
            Aantal bankrekeningen: {lead.AantalBankrekeningen?.ToString() ?? "niet opgegeven"}

            Ontvangen:           {lead.CreatedUtc:dd-MM-yyyy HH:mm} UTC
            Lead ID:             {lead.Id}
            """;

        await _emailService.SendAsync(
            $"Nieuwe GoDutch aanmelding: {lead.BedrijfsNaam}",
            body,
            cancellationToken);
    }
}
