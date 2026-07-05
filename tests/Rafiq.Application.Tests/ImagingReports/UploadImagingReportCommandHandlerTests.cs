using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Moq;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Tests.ImagingReports;

public sealed class UploadImagingReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUploadIsValid_SavesFileToWebRootAndStoresRelativePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "rafiq-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

            var bedrockService = new Mock<IBedrockService>();
            bedrockService
                .Setup(x => x.AnalyzeAsync<BedrockImagingReportDto>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BedrockImagingReportDto
                {
                    ImagingType = "MRI",
                    BodyPart = "Brain",
                    Findings = "No findings",
                    Impression = "Normal",
                    DoctorName = "Dr. Smith",
                    ReportDate = "2026-07-05",
                    AiSummary = "All good"
                });

            ImagingReport? savedReport = null;
            var imagingReportRepository = new Mock<IImagingReportRepository>();
            imagingReportRepository
                .Setup(x => x.AddAsync(It.IsAny<ImagingReport>(), It.IsAny<CancellationToken>()))
                .Callback<ImagingReport, CancellationToken>((report, _) => savedReport = report)
                .Returns(Task.CompletedTask);

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new UploadImagingReportCommandHandler(
                currentUserService.Object,
                bedrockService.Object,
                imagingReportRepository.Object,
                unitOfWork.Object,
                new FakeWebHostEnvironment(tempRoot));

            var imageContent = new byte[] { 1, 2, 3, 4 };
            using var stream = new MemoryStream(imageContent);
            var command = new UploadImagingReportCommand(new FormFile(stream, 0, imageContent.Length, "image", "scan.png"));

            await handler.Handle(command, CancellationToken.None);

            savedReport.Should().NotBeNull();
            savedReport!.ReportImagePath.Should().NotBeNullOrWhiteSpace();
            savedReport.ReportImagePath.Should().StartWith("/imaging-reports/");

            var expectedFilePath = Path.Combine(tempRoot, "imaging-reports", Path.GetFileName(savedReport.ReportImagePath));
            File.Exists(expectedFilePath).Should().BeTrue();
            File.ReadAllBytes(expectedFilePath).Should().Equal(imageContent);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class FakeWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Rafiq.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
