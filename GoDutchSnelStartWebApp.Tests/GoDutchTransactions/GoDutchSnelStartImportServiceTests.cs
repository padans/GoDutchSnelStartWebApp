using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Timers;
using Xunit;

namespace GoDutchSnelStartWebApp.Tests.GoDutchTransactions;

public class GoDutchSnelStartImportServiceTests
{
    [Fact]
    public async Task ImportAsync_WhenFirstUploadFailsAndSecondSucceeds_RetriesAndReturnsSuccess()
    {
        // Arrange
        var exportServiceMock = new Mock<IGoDutchSnelStartExportService>();
        exportServiceMock
            .Setup(x => x.GenerateForSnelStartAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<SnelStartExportFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("dummy-content");

        var importerMock = new Mock<ISnelStartBankStatementImporter>();

        importerMock
            .SetupSequence(x => x.ImportAsync(
                It.IsAny<SnelStartImportRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnelStartImportResult
            {
                Success = false,
                UploadSucceeded = false,
                TransactionCount = 2,
                Message = "Tijdelijke fout"
            })
            .ReturnsAsync(new SnelStartImportResult
            {
                Success = true,
                UploadSucceeded = true,
                TransactionCount = 2,
                Message = "Gelukt"
            });

        var loggerMock = new Mock<ILogger<GoDutchSnelStartImportService>>();

        var retryOptions = Options.Create(new SnelStartImportRetryOptions
        {
            Enabled = true,
            MaxAttempts = 3,
            InitialDelayMilliseconds = 1
        });

        var service = new GoDutchSnelStartImportService(
            exportServiceMock.Object,
            importerMock.Object,
            loggerMock.Object,
            retryOptions);

        // Act
        var result = await service.ImportAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "NL91ABNA0417164300",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            SnelStartExportFormat.Mt940,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.UploadSucceeded);

        importerMock.Verify(
            x => x.ImportAsync(It.IsAny<SnelStartImportRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        exportServiceMock.Verify(
            x => x.GenerateForSnelStartAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<SnelStartExportFormat>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WhenDuplicateImport_DoesNotRetry()
    {
        // Arrange
        var exportServiceMock = new Mock<IGoDutchSnelStartExportService>();
        exportServiceMock
            .Setup(x => x.GenerateForSnelStartAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<SnelStartExportFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("dummy-content");

        var importerMock = new Mock<ISnelStartBankStatementImporter>();
        importerMock
            .Setup(x => x.ImportAsync(
                It.IsAny<SnelStartImportRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnelStartImportResult
            {
                Success = false,
                UploadSucceeded = false,
                IsDuplicateImport = true,
                TransactionCount = 2,
                Message = "Duplicate import"
            });

        var loggerMock = new Mock<ILogger<GoDutchSnelStartImportService>>();

        var retryOptions = Options.Create(new SnelStartImportRetryOptions
        {
            Enabled = true,
            MaxAttempts = 3,
            InitialDelayMilliseconds = 1
        });

        var service = new GoDutchSnelStartImportService(
            exportServiceMock.Object,
            importerMock.Object,
            loggerMock.Object,
            retryOptions);

        // Act
        var result = await service.ImportAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "NL91ABNA0417164300",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            SnelStartExportFormat.Mt940,
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.UploadSucceeded);
        Assert.True(result.IsDuplicateImport);

        importerMock.Verify(
            x => x.ImportAsync(It.IsAny<SnelStartImportRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WhenTransactionCountIsZero_DoesNotRetry()
    {
        // Arrange
        var exportServiceMock = new Mock<IGoDutchSnelStartExportService>();
        exportServiceMock
            .Setup(x => x.GenerateForSnelStartAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<SnelStartExportFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("dummy-content");

        var importerMock = new Mock<ISnelStartBankStatementImporter>();
        importerMock
            .Setup(x => x.ImportAsync(
                It.IsAny<SnelStartImportRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnelStartImportResult
            {
                Success = false,
                UploadSucceeded = false,
                TransactionCount = 0,
                Message = "Geen transacties"
            });

        var loggerMock = new Mock<ILogger<GoDutchSnelStartImportService>>();

        var retryOptions = Options.Create(new SnelStartImportRetryOptions
        {
            Enabled = true,
            MaxAttempts = 3,
            InitialDelayMilliseconds = 1
        });

        var service = new GoDutchSnelStartImportService(
            exportServiceMock.Object,
            importerMock.Object,
            loggerMock.Object,
            retryOptions);

        // Act
        var result = await service.ImportAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "NL91ABNA0417164300",
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31),
            SnelStartExportFormat.Mt940,
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.UploadSucceeded);
        Assert.Equal(0, result.TransactionCount);

        importerMock.Verify(
            x => x.ImportAsync(It.IsAny<SnelStartImportRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}