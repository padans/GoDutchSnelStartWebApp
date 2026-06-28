using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GoDutchSnelStartWebApp.Tests.GoDutchTransactions;

public class GoDutchAutoSyncServiceTests
{
    [Fact]
    public async Task RunOnceAsync_FullFlow_WithRetry_Succeeds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();

        var linkRepository = new Mock<IBankAccountSnelStartLinkRepository>();
        linkRepository
            .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccountSnelStartLink>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    BankAccountId = bankAccountId,
                    IsActive = true
                }
            });

        var bankAccountRepository = new Mock<IBankAccountRepository>();
        bankAccountRepository
            .Setup(x => x.GetByIdAsync(bankAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankAccount
            {
                Id = bankAccountId,
                TenantId = tenantId,
                Iban = "NL91ABNA0417164300",
                IsActive = true,
                AccountName = "Test"
            });

        var importRunRepository = new Mock<IGoDutchImportRunRepository>();
        importRunRepository
            .Setup(x => x.GetLastSuccessfulByBankAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoDutchImportRun?)null);

        importRunRepository
            .Setup(x => x.CreateAsync(It.IsAny<GoDutchImportRun>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        importRunRepository
            .Setup(x => x.UpdateAsync(It.IsAny<GoDutchImportRun>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var autoImportService = new Mock<IGoDutchSnelStartAutoImportService>();
        autoImportService
            .Setup(x => x.ImportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnelStartImportResult
            {
                Success = true,
                UploadSucceeded = true,
                TransactionCount = 5,
                RetryCount = 1,
                Message = "OK"
            });

        var logger = new Mock<ILogger<GoDutchAutoSyncService>>();

        var options = Options.Create(new GoDutchAutoSyncOptions
        {
            OverlapSeconds = 120,
            IntervalMinutes = 15
        });



        var service = new GoDutchAutoSyncService(
             linkRepository.Object,
             bankAccountRepository.Object,
             importRunRepository.Object,
             autoImportService.Object,
             logger.Object,
             options);

        // Act
        await service.RunOnceAsync();

        // Assert
        autoImportService.Verify(x =>
            x.ImportAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        importRunRepository.Verify(x =>
            x.CreateAsync(It.IsAny<GoDutchImportRun>(), It.IsAny<CancellationToken>()),
            Times.Once);

        importRunRepository.Verify(x =>
            x.UpdateAsync(
                It.Is<GoDutchImportRun>(r =>
                    r.Status == ImportRunStatus.Succeeded &&
                    r.TransactionCount == 5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}