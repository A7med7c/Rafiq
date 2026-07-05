using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.LabReports.Commands.UploadLabReport;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.LabReports;

public sealed class UploadLabReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoTestsAreExtracted_StillPersistsTheReportWithEmptyResults()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var bedrockService = new Mock<IBedrockService>();
        bedrockService
            .Setup(x => x.AnalyzeAsync<BedrockLabReportDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BedrockLabReportDto
            {
                LabName = "Sample Lab",
                DoctorName = "Dr. Example",
                ReportDate = "2026-07-05",
                Summary = "No tests extracted",
                Tests = new List<BedrockLabResultDto>()
            });

        LabReport? savedReport = null;
        var labReportRepository = new Mock<ILabReportRepository>();
        labReportRepository
            .Setup(x => x.GetOrCreateDocumentTypeIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        labReportRepository
            .Setup(x => x.AddAsync(It.IsAny<LabReport>(), It.IsAny<CancellationToken>()))
            .Callback<LabReport, CancellationToken>((report, _) => savedReport = report)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UploadLabReportCommandHandler(
            currentUserService.Object,
            bedrockService.Object,
            labReportRepository.Object,
            unitOfWork.Object);

        var imageContent = new byte[] { 1, 2, 3, 4 };
        using var stream = new MemoryStream(imageContent);
        var command = new UploadLabReportCommand(new FormFile(stream, 0, imageContent.Length, "image", "lab.png"));

        var response = await handler.Handle(command, CancellationToken.None);

        response.Success.Should().BeTrue();
        savedReport.Should().NotBeNull();
        savedReport!.Results.Should().BeEmpty();
    }
}
