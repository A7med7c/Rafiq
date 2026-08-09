using Microsoft.AspNetCore.Hosting;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.MedicalReport.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Services.MedicalReport;

public sealed class MedicalReportPdfGenerator : IMedicalReportPdfGenerator
{
    private static readonly string Blue = "#0EAFD7";
    private static readonly string LightBlue = "#E8F8FC";
    private static readonly string DarkText = "#111827";
    private static readonly string MutedText = "#6B7280";
    private static readonly string BorderColor = "#E5E7EB";
    private static readonly string White = "#FFFFFF";

    private readonly string _webRootPath;
    private readonly bool _fontsRegistered;

    public MedicalReportPdfGenerator(IWebHostEnvironment env)
    {
        _webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        QuestPDF.Settings.License = LicenseType.Community;
        if (!_fontsRegistered)
        {
            var fontsDir = Path.Combine(_webRootPath, "fonts");
            var regular = Path.Combine(fontsDir, "Amiri-Regular.ttf");
            var bold    = Path.Combine(fontsDir, "Amiri-Bold.ttf");

            if (File.Exists(regular)) FontManager.RegisterFont(File.OpenRead(regular));
            if (File.Exists(bold))    FontManager.RegisterFont(File.OpenRead(bold));

            _fontsRegistered = true;
        }
    }

    public byte[] Generate(MedicalReportDataDto data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontFamily("Amiri").FontColor(DarkText).FontSize(10));

                page.Header().Element(c => BuildHeader(c, data));
                page.Content().Element(c => BuildContent(c, data));
                page.Footer().Element(c => BuildFooter(c, data));
            });
        }).GeneratePdf();
    }

    // ── Header ────────────────────────────────────────────────────────────────
    private void BuildHeader(IContainer container, MedicalReportDataDto data)
    {
        container
            .Background(Blue)
            .Padding(24)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item()
                            .Text("Rafiq AI")
                            .FontFamily("Amiri").Bold().FontSize(22).FontColor(White);

                        inner.Item()
                            .Text("Complete Medical File")
                            .FontFamily("Amiri").FontSize(13).FontColor(White).Italic();
                    });

                    row.ConstantItem(120).AlignRight().Column(inner =>
                    {
                        inner.Item()
                            .Text($"{data.Profile.FirstName} {data.Profile.LastName}")
                            .FontFamily("Amiri").Bold().FontSize(12).FontColor(White).AlignRight();

                        inner.Item()
                            .Text(data.GeneratedAt.ToString("dd MMM yyyy"))
                            .FontFamily("Amiri").FontSize(10).FontColor(White).AlignRight();
                    });
                });
            });
    }

    // ── Footer ────────────────────────────────────────────────────────────────
    private void BuildFooter(IContainer container, MedicalReportDataDto data)
    {
        container
            .BorderTop(1).BorderColor(BorderColor)
            .PaddingHorizontal(24).PaddingVertical(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Rafiq AI  |  Generated: " + data.GeneratedAt.ToString("dd MMM yyyy HH:mm"))
                    .FontSize(8).FontColor(MutedText);

                row.ConstantItem(80).AlignRight()
                    .Text(txt =>
                    {
                        txt.Span("Page ").FontSize(8).FontColor(MutedText);
                        txt.CurrentPageNumber().FontSize(8).FontColor(MutedText);
                        txt.Span(" of ").FontSize(8).FontColor(MutedText);
                        txt.TotalPages().FontSize(8).FontColor(MutedText);
                    });
            });
    }

    // ── Content ───────────────────────────────────────────────────────────────
    private void BuildContent(IContainer container, MedicalReportDataDto data)
    {
        container.PaddingHorizontal(24).PaddingTop(16).PaddingBottom(8).Column(col =>
        {
            BuildPatientInfo(col, data);
            BuildAllergiesAndDiseases(col, data);
            BuildMedications(col, data);
            BuildPrescriptions(col, data);
            BuildLabReports(col, data);
            BuildImagingReports(col, data);
            BuildAppointments(col, data);
            BuildGeneralDocuments(col, data);
            BuildTimeline(col, data);

            if (data.AiClinicalSummary is not null)
                BuildAiSummary(col, data.AiClinicalSummary);
        });
    }

    // ── Section header ────────────────────────────────────────────────────────
    private static void SectionHeader(ColumnDescriptor col, string title, string icon = "")
    {
        col.Item().PaddingTop(14).PaddingBottom(4).Row(row =>
        {
            row.RelativeItem()
                .Background(LightBlue)
                .Padding(6)
                .Text($"{icon} {title}".Trim())
                .Bold().FontSize(11).FontColor(Blue);
        });
    }

    // ── Patient Information ───────────────────────────────────────────────────
    private void BuildPatientInfo(ColumnDescriptor col, MedicalReportDataDto data)
    {
        SectionHeader(col, "Patient Information");

        var p = data.Profile;
        var age = CalculateAge(p.DateOfBirth);

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            InfoCell(table, "Full Name",   $"{p.FirstName} {p.LastName}");
            InfoCell(table, "Date of Birth", p.DateOfBirth.ToString("dd MMM yyyy"));
            InfoCell(table, "Age",          $"{age} years");
            InfoCell(table, "Gender",       p.Gender.ToString());
            InfoCell(table, "Blood Type",   p.BloodType?.ToString() ?? "—");
            InfoCell(table, "Height",       p.Height.HasValue ? $"{p.Height} cm" : "—");
            InfoCell(table, "Weight",       p.Weight.HasValue ? $"{p.Weight} kg" : "—");
            InfoCell(table, "BMI",          (p.Height.HasValue && p.Weight.HasValue)
                ? CalculateBmi(p.Height.Value, p.Weight.Value)
                : "—");
        });

        if (data.EmergencyContacts.Any())
        {
            var ec = data.EmergencyContacts.First();
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("Emergency Contact: ").Bold().FontSize(10).FontColor(DarkText);
                row.RelativeItem().Text($"{ec.Name} ({ec.Relation}) — {ec.PhoneNumber}").FontSize(10);
            });
        }
    }

    // ── Allergies & Chronic Diseases ──────────────────────────────────────────
    private void BuildAllergiesAndDiseases(ColumnDescriptor col, MedicalReportDataDto data)
    {
        var allergies = data.Profile.Allergies.ToList();
        var diseases  = data.Profile.ChronicDiseases.ToList();

        if (!allergies.Any() && !diseases.Any()) return;

        SectionHeader(col, "Allergies & Chronic Conditions");

        col.Item().Row(row =>
        {
            if (allergies.Any())
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().PaddingBottom(3).Text("Allergies").Bold().FontSize(10);
                    foreach (var a in allergies)
                    {
                        inner.Item().Row(r =>
                        {
                            r.ConstantItem(8).Height(8).AlignMiddle()
                                .Background(SeverityColor(a.Severity.ToString()))
                                .Width(8);
                            r.RelativeItem().PaddingLeft(4)
                                .Text($"{a.Name} — {a.Severity}").FontSize(9);
                        });
                    }
                });
            }

            if (diseases.Any())
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().PaddingBottom(3).Text("Chronic Diseases").Bold().FontSize(10);
                    foreach (var d in diseases)
                    {
                        inner.Item().Text($"• {d.Name}" +
                            (d.DiagnosedAt.HasValue ? $"  (diagnosed {d.DiagnosedAt.Value:MMM yyyy})" : ""))
                            .FontSize(9);
                    }
                });
            }
        });
    }

    // ── Medications ───────────────────────────────────────────────────────────
    private void BuildMedications(ColumnDescriptor col, MedicalReportDataDto data)
    {
        if (!data.Medicines.Any()) return;

        SectionHeader(col, "Current Medications");

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            TableHeader(table, "Medicine", "Dosage", "Frequency", "Duration");

            foreach (var m in data.Medicines)
            {
                TableRow(table, m.MedicineName, m.Dosage, m.Frequency, m.Duration);
            }
        });
    }

    // ── Prescriptions ─────────────────────────────────────────────────────────
    private void BuildPrescriptions(ColumnDescriptor col, MedicalReportDataDto data)
    {
        if (!data.Prescriptions.Any()) return;

        SectionHeader(col, "Prescriptions");

        foreach (var p in data.Prescriptions.OrderByDescending(x => x.PrescriptionDate))
        {
            col.Item().PaddingTop(6).Column(inner =>
            {
                inner.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Dr. {p.DoctorName}  —  {p.PrescriptionDate:dd MMM yyyy}")
                        .Bold().FontSize(10);
                });

                if (p.Medicines.Any())
                {
                    inner.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        TableHeader(table, "Medicine", "Dosage", "Frequency", "Duration");
                        foreach (var m in p.Medicines)
                            TableRow(table, m.MedicineName, m.Dosage, m.Frequency, m.Duration);
                    });
                }

                var imgBytes = TryLoadImageBytes(p.ImagePath);
                if (imgBytes != null)
                    inner.Item().PaddingTop(4).MaxWidth(300).Image(imgBytes);
            });
        }
    }

    // ── Lab Reports ───────────────────────────────────────────────────────────
    private void BuildLabReports(ColumnDescriptor col, MedicalReportDataDto data)
    {
        if (!data.LabReports.Any()) return;

        SectionHeader(col, "Lab Reports");

        foreach (var lab in data.LabReports.OrderByDescending(l => l.ReportDate))
        {
            col.Item().PaddingTop(6).Column(inner =>
            {
                inner.Item().Text($"{lab.LabName}  —  Dr. {lab.DoctorName}  —  {lab.ReportDate:dd MMM yyyy}")
                    .Bold().FontSize(10);

                if (!string.IsNullOrWhiteSpace(lab.Description))
                    inner.Item().PaddingTop(2).Text(lab.Description).FontSize(9).FontColor(MutedText);

                if (lab.Results.Any())
                {
                    inner.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(60);
                        });

                        TableHeader(table, "Test", "Value", "Unit", "Range", "Status");

                        foreach (var r in lab.Results)
                        {
                            table.Cell().Padding(4).Text(r.TestName).FontSize(9);
                            table.Cell().Padding(4).Text(r.Value).FontSize(9);
                            table.Cell().Padding(4).Text(r.Unit).FontSize(9);
                            table.Cell().Padding(4).Text(r.NormalRange).FontSize(9);
                            table.Cell().Padding(3).Element(c =>
                                c.Background(StatusBadgeColor(r.Status))
                                 .Padding(2)
                                 .Text(r.Status ?? "—")
                                 .FontSize(8)
                                 .FontColor(White)
                                 .AlignCenter());
                        }
                    });
                }

                var imgBytes = TryLoadImageBytes(lab.ImageUrl);
                if (imgBytes != null)
                    inner.Item().PaddingTop(4).MaxWidth(300).Image(imgBytes);
            });
        }
    }

    // ── Imaging Reports ───────────────────────────────────────────────────────
    private void BuildImagingReports(ColumnDescriptor col, MedicalReportDataDto data)
    {
        if (!data.ImagingReports.Any()) return;

        SectionHeader(col, "Imaging Reports");

        foreach (var img in data.ImagingReports.OrderByDescending(i => i.ReportDate))
        {
            col.Item().PaddingTop(6).Column(inner =>
            {
                inner.Item().Text($"{img.ImagingType} — {img.BodyPart}  |  {img.ReportDate:dd MMM yyyy}")
                    .Bold().FontSize(10);

                if (!string.IsNullOrWhiteSpace(img.DoctorName))
                    inner.Item().Text($"Dr. {img.DoctorName}").FontSize(9).FontColor(MutedText);

                if (!string.IsNullOrWhiteSpace(img.Findings))
                    inner.Item().PaddingTop(2).Text("Findings: " + img.Findings).FontSize(9);

                if (!string.IsNullOrWhiteSpace(img.Impression))
                    inner.Item().PaddingTop(2).Text("Impression: " + img.Impression).FontSize(9);

                var imgBytes = TryLoadImageBytes(img.ImageUrl);
                if (imgBytes != null)
                    inner.Item().PaddingTop(4).MaxWidth(300).Image(imgBytes);
            });
        }
    }

    // ── Appointments ──────────────────────────────────────────────────────────
    private void BuildAppointments(ColumnDescriptor col, MedicalReportDataDto data)
    {
        if (!data.Appointments.Any()) return;

        SectionHeader(col, "Appointments");

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn();
                c.ConstantColumn(70);
            });

            TableHeader(table, "Title", "Provider", "Date", "Status");

            foreach (var a in data.Appointments.OrderBy(x => x.AppointmentDateTime))
            {
                table.Cell().Padding(4).Text(a.Title).FontSize(9);
                table.Cell().Padding(4).Text(a.Provider).FontSize(9);
                table.Cell().Padding(4).Text(a.AppointmentDateTime.ToString("dd MMM yyyy")).FontSize(9);
                table.Cell().Padding(3).Element(c =>
                    c.Background(AppointmentStatusColor(a.Status.ToString()))
                     .Padding(2)
                     .Text(a.Status.ToString())
                     .FontSize(8).FontColor(White).AlignCenter());
            }
        });
    }

    // ── General Documents ─────────────────────────────────────────────────────
    private void BuildGeneralDocuments(ColumnDescriptor col, MedicalReportDataDto data)
    {
        if (!data.GeneralDocuments.Any()) return;

        SectionHeader(col, "Other Medical Documents");

        foreach (var doc in data.GeneralDocuments.OrderByDescending(d => d.CreatedAt))
        {
            col.Item().PaddingTop(8).Column(inner =>
            {
                // Title + status badge
                inner.Item().Row(row =>
                {
                    row.RelativeItem().Text(doc.Title).Bold().FontSize(10);

                    var (statusLabel, statusColor) = doc.AnalysisStatus switch
                    {
                        GeneralDocumentStatus.Completed  => ("Analyzed",    "#10B981"),
                        GeneralDocumentStatus.Processing => ("Processing",  "#F59E0B"),
                        GeneralDocumentStatus.Pending    => ("Pending",     "#F59E0B"),
                        GeneralDocumentStatus.Failed     => ("Failed",      "#EF4444"),
                        _                                => ("Unknown",     MutedText)
                    };

                    row.ConstantItem(72).AlignRight().Element(c =>
                        c.Background(statusColor).Padding(3)
                         .Text(statusLabel).FontSize(8).FontColor(White).AlignCenter());
                });

                // Metadata
                if (!string.IsNullOrWhiteSpace(doc.DocumentType))
                    inner.Item().Text($"Type: {doc.DocumentType}").FontSize(9).FontColor(MutedText);
                if (!string.IsNullOrWhiteSpace(doc.DoctorName))
                    inner.Item().Text($"Doctor: {doc.DoctorName}").FontSize(9).FontColor(MutedText);
                if (!string.IsNullOrWhiteSpace(doc.HospitalOrClinic))
                    inner.Item().Text($"Hospital/Clinic: {doc.HospitalOrClinic}").FontSize(9).FontColor(MutedText);
                if (!string.IsNullOrWhiteSpace(doc.DocumentDate))
                    inner.Item().Text($"Date: {doc.DocumentDate}").FontSize(9).FontColor(MutedText);
                if (!string.IsNullOrWhiteSpace(doc.Description))
                    inner.Item().PaddingTop(2).Text(doc.Description).FontSize(9);

                // AI summary
                if (!string.IsNullOrWhiteSpace(doc.AiSummary))
                {
                    inner.Item().PaddingTop(4)
                        .Background(LightBlue)
                        .Border(1).BorderColor(Blue)
                        .Padding(6)
                        .Text("AI Summary: " + doc.AiSummary)
                        .FontSize(9).Italic();
                }
                else if (doc.AnalysisStatus == GeneralDocumentStatus.Failed
                         && !string.IsNullOrWhiteSpace(doc.FailureReason))
                {
                    inner.Item().PaddingTop(4)
                        .Background("#FEF2F2")
                        .Padding(4)
                        .Text("Analysis failed: " + doc.FailureReason)
                        .FontSize(9).FontColor("#EF4444");
                }

                // Original document image (always shown — the file is the record for non-analyzed docs)
                var imgBytes = TryLoadImageBytes(doc.ImagePath);
                if (imgBytes != null)
                {
                    inner.Item().PaddingTop(6)
                        .Border(1).BorderColor(BorderColor)
                        .MaxWidth(420)
                        .Image(imgBytes);
                }
                else if (!string.IsNullOrWhiteSpace(doc.ImagePath))
                {
                    var ext = Path.GetExtension(doc.ImagePath).ToLowerInvariant();
                    if (ext == ".pdf")
                        inner.Item().PaddingTop(4)
                            .Text("Original file: PDF document (not embeddable in report)")
                            .FontSize(9).FontColor(MutedText).Italic();
                }
            });

            col.Item().PaddingTop(6).BorderBottom(1).BorderColor(BorderColor);
        }
    }

    // ── Timeline ──────────────────────────────────────────────────────────────
    private void BuildTimeline(ColumnDescriptor col, MedicalReportDataDto data)
    {
        var events = BuildTimelineEvents(data);
        if (!events.Any()) return;

        SectionHeader(col, "Medical Timeline");

        var grouped = events.GroupBy(e => e.Date.Year).OrderByDescending(g => g.Key);

        foreach (var year in grouped)
        {
            col.Item().PaddingTop(8).Text(year.Key.ToString())
                .Bold().FontSize(11).FontColor(Blue);

            foreach (var ev in year.OrderByDescending(e => e.Date))
            {
                col.Item().PaddingLeft(12).PaddingTop(2).Row(row =>
                {
                    row.ConstantItem(6).Height(6).AlignMiddle().Background(Blue).Width(6);
                    row.RelativeItem().PaddingLeft(8)
                        .Text($"{ev.Date:dd MMM}  —  {ev.Description}")
                        .FontSize(9);
                });
            }
        }
    }

    private static List<(DateTime Date, string Description)> BuildTimelineEvents(MedicalReportDataDto data)
    {
        var events = new List<(DateTime, string)>();

        foreach (var d in data.Profile.ChronicDiseases.Where(x => x.DiagnosedAt.HasValue))
            events.Add((d.DiagnosedAt!.Value.ToDateTime(TimeOnly.MinValue), $"Diagnosed: {d.Name}"));

        foreach (var l in data.LabReports)
            events.Add((l.ReportDate.ToDateTime(TimeOnly.MinValue), $"Lab Report: {l.LabName}"));

        foreach (var i in data.ImagingReports)
            events.Add((i.ReportDate.ToDateTime(TimeOnly.MinValue), $"Imaging: {i.ImagingType} — {i.BodyPart}"));

        foreach (var p in data.Prescriptions)
            events.Add((p.PrescriptionDate.ToDateTime(TimeOnly.MinValue), $"Prescription by Dr. {p.DoctorName}"));

        foreach (var a in data.Appointments.Where(x => x.Status != Domain.Enums.AppointmentStatus.Upcoming))
            events.Add((a.AppointmentDateTime, $"Appointment: {a.Title} — {a.Provider} ({a.Status})"));

        foreach (var g in data.GeneralDocuments.Where(x => x.AnalysisStatus == GeneralDocumentStatus.Completed))
            events.Add((g.CreatedAt, $"Document: {g.Title}" + (string.IsNullOrWhiteSpace(g.DocumentType) ? "" : $" ({g.DocumentType})")));

        return events;
    }

    // ── AI Clinical Summary ───────────────────────────────────────────────────
    private void BuildAiSummary(ColumnDescriptor col, string summary)
    {
        SectionHeader(col, "AI Clinical Summary");

        col.Item()
            .Background(LightBlue)
            .Border(1).BorderColor(Blue)
            .Padding(10)
            .Text(summary)
            .FontSize(10).Italic();
    }

    // ── Image loader ──────────────────────────────────────────────────────────
    private byte[]? TryLoadImageBytes(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        try
        {
            var abs = Path.Combine(_webRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(abs)) return null;
            var ext = Path.GetExtension(abs).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return null;
            return File.ReadAllBytes(abs);
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void InfoCell(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(1).BorderColor(BorderColor).Padding(6).Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor(MutedText);
            col.Item().Text(value).FontSize(10).Bold();
        });
    }

    private static void TableHeader(TableDescriptor table, params string[] headers)
    {
        foreach (var h in headers)
        {
            table.Cell().Background(Blue).Padding(5)
                .Text(h).FontSize(9).Bold().FontColor(White);
        }
    }

    private static void TableRow(TableDescriptor table, params string[] cells)
    {
        foreach (var c in cells)
        {
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4)
                .Text(c ?? "—").FontSize(9);
        }
    }

    private static string StatusBadgeColor(string? status)
    {
        if (status is null) return "#9CA3AF";
        return status.ToLowerInvariant() switch
        {
            "normal"   => "#10B981",
            "high"     => "#EF4444",
            "low"      => "#F59E0B",
            "critical" => "#DC2626",
            _          => "#6B7280"
        };
    }

    private static string SeverityColor(string severity) =>
        severity.ToLowerInvariant() switch
        {
            "mild"     => "#10B981",
            "moderate" => "#F59E0B",
            "severe"   => "#EF4444",
            _          => "#6B7280"
        };

    private static string AppointmentStatusColor(string status) =>
        status.ToLowerInvariant() switch
        {
            "upcoming"  => "#0EAFD7",
            "completed" => "#10B981",
            "cancelled" => "#EF4444",
            "missed"    => "#F59E0B",
            _           => "#6B7280"
        };

    private static int CalculateAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dob.Year;
        if (today < dob.AddYears(age)) age--;
        return age;
    }

    private static string CalculateBmi(decimal height, decimal weight)
    {
        if (height <= 0) return "—";
        var heightM = height / 100m;
        var bmi = weight / (heightM * heightM);
        return $"{bmi:F1}";
    }
}
