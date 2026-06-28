using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Services;

public sealed class BankAccountSnelStartLinkService : IBankAccountSnelStartLinkService
{
    private const int DefaultSyncIntervalMinutes = 15;
    private const int MinimumSyncIntervalMinutes = 15;
    private const int MaximumSyncIntervalMinutes = 24 * 60;

    private readonly IBankAccountSnelStartLinkRepository _linkRepository;
    private readonly ISnelStartAdministrationRepository _administrationRepository;
    private readonly ILogger<BankAccountSnelStartLinkService> _logger;

    public BankAccountSnelStartLinkService(
        IBankAccountSnelStartLinkRepository linkRepository,
        ISnelStartAdministrationRepository administrationRepository,
        ILogger<BankAccountSnelStartLinkService> logger)
    {
        _linkRepository = linkRepository;
        _administrationRepository = administrationRepository;
        _logger = logger;
    }

    public async Task<BankAccountSnelStartLinkDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByIdAsync(id, cancellationToken);

        if (link is null)
        {
            return null;
        }

        return MapToDto(link);
    }

    public async Task<BankAccountSnelStartLinkDto?> GetByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);

        if (link is null)
        {
            return null;
        }

        return MapToDto(link);
    }

    public async Task<Guid> CreateAsync(
        CreateBankAccountSnelStartLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.BankAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("BankAccountId is required.");
        }

        if (request.SnelStartAdministrationId == Guid.Empty)
        {
            throw new InvalidOperationException("SnelStartAdministrationId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ExportFormat))
        {
            throw new InvalidOperationException("ExportFormat is required.");
        }

        var exportFormat = ParseExportFormat(request.ExportFormat);

        var administration = await _administrationRepository.GetByIdAsync(
            request.SnelStartAdministrationId,
            cancellationToken);

        if (administration is null)
        {
            throw new InvalidOperationException("SnelStartAdministration not found.");
        }

        var existingLink = await _linkRepository.GetByBankAccountIdAsync(
            request.BankAccountId,
            cancellationToken);

        if (existingLink is not null)
        {
            throw new InvalidOperationException("A SnelStart link already exists for this bank account.");
        }

        var nowUtc = DateTime.UtcNow;
        var autoSyncEnabled = request.AutoSyncEnabled;
        var syncIntervalMinutes = NormalizeSyncIntervalMinutes(request.SyncIntervalMinutes);

        var link = new BankAccountSnelStartLink
        {
            Id = Guid.NewGuid(),
            BankAccountId = request.BankAccountId,
            SnelStartAdministrationId = request.SnelStartAdministrationId,
            ExportFormat = exportFormat,
            IsActive = request.IsActive,
            AutoSyncEnabled = autoSyncEnabled,
            SyncIntervalMinutes = syncIntervalMinutes,
            LastRunUtc = null,
            NextRunUtc = autoSyncEnabled ? nowUtc : null
        };

        await _linkRepository.CreateAsync(link, cancellationToken);

        _logger.LogInformation(
            "BankAccountSnelStartLink created. LinkId: {LinkId}, BankAccountId: {BankAccountId}, SnelStartAdministrationId: {AdministrationId}, ExportFormat: {ExportFormat}, AutoSyncEnabled: {AutoSyncEnabled}, SyncIntervalMinutes: {SyncIntervalMinutes}.",
            link.Id,
            link.BankAccountId,
            link.SnelStartAdministrationId,
            link.ExportFormat,
            link.AutoSyncEnabled,
            link.SyncIntervalMinutes);

        return link.Id;
    }

    public async Task UpdateAsync(
        UpdateBankAccountSnelStartLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Id is required.");
        }

        if (request.BankAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("BankAccountId is required.");
        }

        if (request.SnelStartAdministrationId == Guid.Empty)
        {
            throw new InvalidOperationException("SnelStartAdministrationId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ExportFormat))
        {
            throw new InvalidOperationException("ExportFormat is required.");
        }

        var exportFormat = ParseExportFormat(request.ExportFormat);

        var existing = await _linkRepository.GetByIdAsync(request.Id, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException("BankAccountSnelStartLink not found.");
        }

        var administration = await _administrationRepository.GetByIdAsync(
            request.SnelStartAdministrationId,
            cancellationToken);

        if (administration is null)
        {
            throw new InvalidOperationException("SnelStartAdministration not found.");
        }

        var linkForBankAccount = await _linkRepository.GetByBankAccountIdAsync(
            request.BankAccountId,
            cancellationToken);

        if (linkForBankAccount is not null && linkForBankAccount.Id != request.Id)
        {
            throw new InvalidOperationException("Another SnelStart link already exists for this bank account.");
        }

        var wasDisabled = !existing.AutoSyncEnabled;
        var becomesEnabled = request.AutoSyncEnabled;
        var intervalChanged = existing.SyncIntervalMinutes != NormalizeSyncIntervalMinutes(request.SyncIntervalMinutes);

        existing.BankAccountId = request.BankAccountId;
        existing.SnelStartAdministrationId = request.SnelStartAdministrationId;
        existing.ExportFormat = exportFormat;
        existing.IsActive = request.IsActive;
        existing.AutoSyncEnabled = request.AutoSyncEnabled;
        existing.SyncIntervalMinutes = NormalizeSyncIntervalMinutes(request.SyncIntervalMinutes);

        if (!existing.AutoSyncEnabled)
        {
            existing.NextRunUtc = null;
        }
        else if (wasDisabled || intervalChanged || existing.NextRunUtc is null)
        {
            existing.NextRunUtc = DateTime.UtcNow;
        }

        await _linkRepository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation(
            "BankAccountSnelStartLink updated. LinkId: {LinkId}, BankAccountId: {BankAccountId}, SnelStartAdministrationId: {AdministrationId}, ExportFormat: {ExportFormat}, AutoSyncEnabled: {AutoSyncEnabled}, SyncIntervalMinutes: {SyncIntervalMinutes}.",
            existing.Id,
            existing.BankAccountId,
            existing.SnelStartAdministrationId,
            existing.ExportFormat,
            existing.AutoSyncEnabled,
            existing.SyncIntervalMinutes);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Id is required.");
        }

        await _linkRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation(
            "BankAccountSnelStartLink deleted. LinkId: {LinkId}.",
            id);
    }

    private static BankAccountSnelStartLinkDto MapToDto(BankAccountSnelStartLink link)
    {
        return new BankAccountSnelStartLinkDto
        {
            Id = link.Id,
            BankAccountId = link.BankAccountId,
            SnelStartAdministrationId = link.SnelStartAdministrationId,
            ExportFormat = link.ExportFormat.ToString(),
            IsActive = link.IsActive,
            AutoSyncEnabled = link.AutoSyncEnabled,
            SyncIntervalMinutes = link.SyncIntervalMinutes,
            LastRunUtc = link.LastRunUtc,
            NextRunUtc = link.NextRunUtc,
            CreatedUtc = link.CreatedUtc,
            ModifiedUtc = link.ModifiedUtc
        };
    }

    private static SnelStartExportFormat ParseExportFormat(string exportFormat)
    {
        if (!Enum.TryParse<SnelStartExportFormat>(exportFormat.Trim(), ignoreCase: true, out var format))
        {
            throw new InvalidOperationException(
                $"ExportFormat must be '{SnelStartExportFormat.Mt940}' or '{SnelStartExportFormat.Camt053}'.");
        }

        return format;
    }

    private static int NormalizeSyncIntervalMinutes(int syncIntervalMinutes)
    {
        if (syncIntervalMinutes <= 0)
        {
            return DefaultSyncIntervalMinutes;
        }

        return Math.Clamp(
            syncIntervalMinutes,
            MinimumSyncIntervalMinutes,
            MaximumSyncIntervalMinutes);
    }
}
