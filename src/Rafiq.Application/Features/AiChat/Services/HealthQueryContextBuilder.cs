using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;
using System.Text;

namespace Rafiq.Application.Features.AiChat.Services;

/// <summary>
/// Renders a validated <see cref="ParsedHealthQueryIntent"/> into a compact, plain-text
/// health context for the AI prompt. Only queries the categories present in the intent,
/// applies the intent's searchTerm/timeframe as in-memory filters over data the caller has
/// already been authorized to read (via IHealthProfileAuthorizationService, checked once
/// by the caller before this class is invoked), and caps list sizes so only the minimum
/// data needed for the question is ever sent to the model.
/// </summary>
public sealed class HealthQueryContextBuilder : IHealthQueryContextBuilder
{
    private const int MaxItemsPerCategory = 10;

    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUserMedicineRepository _userMedicineRepository;
    private readonly ILabReportRepository _labReportRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IImagingReportRepository _imagingReportRepository;

    public HealthQueryContextBuilder(
        IPatientProfileRepository patientProfileRepository,
        IAppointmentRepository appointmentRepository,
        IUserMedicineRepository userMedicineRepository,
        ILabReportRepository labReportRepository,
        IPrescriptionRepository prescriptionRepository,
        IImagingReportRepository imagingReportRepository)
    {
        _patientProfileRepository = patientProfileRepository;
        _appointmentRepository = appointmentRepository;
        _userMedicineRepository = userMedicineRepository;
        _labReportRepository = labReportRepository;
        _prescriptionRepository = prescriptionRepository;
        _imagingReportRepository = imagingReportRepository;
    }

    public async Task<string> BuildAsync(
        ParsedHealthQueryIntent intent,
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
    {
        if (intent.HasNoCategories)
            return string.Empty;

        var sections = new List<string>();

        foreach (var category in intent.Categories)
        {
            var section = category switch
            {
                HealthQueryCategory.Profile => await BuildProfileSectionAsync(userHealthProfileId, cancellationToken),
                HealthQueryCategory.Allergies => await BuildAllergiesSectionAsync(userHealthProfileId, intent, cancellationToken),
                HealthQueryCategory.ChronicDiseases => await BuildChronicDiseasesSectionAsync(userHealthProfileId, intent, cancellationToken),
                HealthQueryCategory.Medicines => await BuildMedicinesSectionAsync(userHealthProfileId, intent, cancellationToken),
                HealthQueryCategory.Appointments => await BuildAppointmentsSectionAsync(userHealthProfileId, intent, cancellationToken),
                HealthQueryCategory.LabReports => await BuildLabReportsSectionAsync(userHealthProfileId, intent, cancellationToken),
                HealthQueryCategory.Prescriptions => await BuildPrescriptionsSectionAsync(userHealthProfileId, intent, cancellationToken),
                HealthQueryCategory.ImagingReports => await BuildImagingReportsSectionAsync(userHealthProfileId, intent, cancellationToken),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(section))
                sections.Add(section!);
        }

        return string.Join("\n\n", sections);
    }

    private async Task<string?> BuildProfileSectionAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
            return null;

        var medicines = await _userMedicineRepository.GetAllByProfileIdAsync(profileId, cancellationToken);
        var appointments = await _appointmentRepository.GetUpcomingByUserHealthProfileIdAsync(profileId, cancellationToken);
        var labReports = await _labReportRepository.GetAllByProfileIdAsync(profileId, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Profile summary:");
        sb.AppendLine($"- Name: {profile.FirstName} {profile.LastName}");
        sb.AppendLine($"- Gender: {profile.Gender}, date of birth: {profile.DateOfBirth:yyyy-MM-dd}");
        if (profile.Height.HasValue)
            sb.AppendLine($"- Height: {profile.Height} cm");
        if (profile.Weight.HasValue)
            sb.AppendLine($"- Weight: {profile.Weight} kg");
        if (profile.BloodType.HasValue)
            sb.AppendLine($"- Blood type: {profile.BloodType}");
        sb.AppendLine($"- Registered allergies: {profile.Allergies.Count}");
        sb.AppendLine($"- Registered chronic diseases: {profile.ChronicDiseases.Count}");
        sb.AppendLine($"- Registered medicines: {medicines.Count}");
        sb.AppendLine($"- Upcoming appointments: {appointments.Count}");
        sb.AppendLine($"- Lab reports on file: {labReports.Count}");

        return sb.ToString().TrimEnd();
    }

    private async Task<string?> BuildAllergiesSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
            return null;

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        var items = profile.Allergies.AsEnumerable();
        if (terms is not null)
            items = items.Where(a => ContainsAny(a.Name, terms));

        var list = items.ToList();

        return intent.Operation switch
        {
            HealthQueryOperation.Count => DescribeCount("Allergies", intent.SearchTerm, list.Count),
            HealthQueryOperation.Exists => DescribeExists(
                "Allergies", intent.SearchTerm, list.Count,
                list.Count > 0 ? $"{list[0].Name} (severity: {list[0].Severity})" : null),
            _ => list.Count == 0
                ? "Allergies: no matching allergy records are currently registered in Rafiq."
                : "Allergies:\n" + string.Join("\n", list.Take(MaxItemsPerCategory).Select(a => $"- {a.Name} (severity: {a.Severity})"))
        };
    }

    private async Task<string?> BuildChronicDiseasesSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
            return null;

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        var items = profile.ChronicDiseases.AsEnumerable();
        if (terms is not null)
            items = items.Where(d => ContainsAny(d.Name, terms));

        var list = items.ToList();
        string Describe(Rafiq.Domain.Entities.User.ChronicDisease d) =>
            $"{d.Name} (status: {d.Status}{(d.DiagnosedAt is null ? "" : $", diagnosed {d.DiagnosedAt:yyyy-MM-dd}")})";

        return intent.Operation switch
        {
            HealthQueryOperation.Count => DescribeCount("Chronic diseases", intent.SearchTerm, list.Count),
            HealthQueryOperation.Exists => DescribeExists(
                "Chronic diseases", intent.SearchTerm, list.Count,
                list.Count > 0 ? Describe(list[0]) : null),
            HealthQueryOperation.GetLatest => FormatSingleOrEmpty(
                "Chronic diseases", list.OrderByDescending(d => d.DiagnosedAt ?? DateOnly.MinValue).FirstOrDefault(), Describe),
            HealthQueryOperation.GetOldest => FormatSingleOrEmpty(
                "Chronic diseases", list.OrderBy(d => d.DiagnosedAt ?? DateOnly.MaxValue).FirstOrDefault(), Describe),
            _ => list.Count == 0
                ? "Chronic diseases: no matching records are currently registered in Rafiq."
                : "Chronic diseases:\n" + string.Join("\n", list.Take(MaxItemsPerCategory).Select(d => $"- {Describe(d)}"))
        };
    }

    private async Task<string?> BuildMedicinesSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var medicines = await _userMedicineRepository.GetAllByProfileIdAsync(profileId, cancellationToken);

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        IEnumerable<UserMedicine> items = medicines;
        if (terms is not null)
            items = items.Where(m => ContainsAny(m.MedicineName, terms));
        if (intent.Timeframe != HealthQueryTimeframe.None)
            items = items.Where(m => IsWithinTimeframe(DateOnly.FromDateTime(m.CreatedAt), intent.Timeframe));

        var list = items.OrderByDescending(m => m.CreatedAt).ToList();
        string Describe(UserMedicine m) => $"{m.MedicineName} (dose: {m.Dosage}, frequency: {m.Frequency}, registered {m.CreatedAt:yyyy-MM-dd})";

        return intent.Operation switch
        {
            HealthQueryOperation.Count => DescribeCount("Medicines", intent.SearchTerm, list.Count),
            HealthQueryOperation.Exists => DescribeExists(
                "Medicines", intent.SearchTerm, list.Count,
                list.Count > 0 ? $"{list[0].MedicineName} ({list[0].Dosage})" : null),
            HealthQueryOperation.GetLatest => FormatSingleOrEmpty("Medicines", list.FirstOrDefault(), Describe),
            HealthQueryOperation.GetOldest => FormatSingleOrEmpty("Medicines", list.OrderBy(m => m.CreatedAt).FirstOrDefault(), Describe),
            _ => list.Count == 0
                ? "Medicines: no matching medicines are currently registered in Rafiq."
                : "Medicines:\n" + string.Join("\n", list.Take(MaxItemsPerCategory).Select(m => $"- {m.MedicineName} (dose: {m.Dosage}, frequency: {m.Frequency})"))
        };
    }

    private async Task<string?> BuildAppointmentsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetAllByUserHealthProfileIdAsync(profileId, cancellationToken);

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        IEnumerable<Appointment> items = appointments;
        if (terms is not null)
            items = items.Where(a => ContainsAny(a.Title, terms) || ContainsAny(a.Provider, terms) || ContainsAny(a.CustomType, terms));
        if (intent.Timeframe != HealthQueryTimeframe.None)
            items = items.Where(a => IsWithinTimeframe(DateOnly.FromDateTime(a.AppointmentDateTime), intent.Timeframe));

        var materialized = items.ToList();
        var now = DateTime.UtcNow;
        string Describe(Appointment a) => $"{a.Title} with {a.Provider} on {a.AppointmentDateTime:yyyy-MM-dd HH:mm} (status: {a.Status})";

        switch (intent.Operation)
        {
            case HealthQueryOperation.Count:
                return DescribeCount("Appointments", intent.SearchTerm, materialized.Count);

            case HealthQueryOperation.Exists:
                var firstMatch = materialized.FirstOrDefault();
                return DescribeExists("Appointments", intent.SearchTerm, materialized.Count, firstMatch is null ? null : Describe(firstMatch));

            case HealthQueryOperation.GetNext:
                var next = materialized.Where(a => a.AppointmentDateTime >= now).OrderBy(a => a.AppointmentDateTime).FirstOrDefault();
                return FormatSingleOrEmpty("Appointments", next, Describe);

            case HealthQueryOperation.GetPrevious:
            case HealthQueryOperation.GetLatest:
                var previous = materialized.Where(a => a.AppointmentDateTime < now).OrderByDescending(a => a.AppointmentDateTime).FirstOrDefault();
                return FormatSingleOrEmpty("Appointments", previous, Describe);

            case HealthQueryOperation.GetOldest:
                var oldest = materialized.OrderBy(a => a.AppointmentDateTime).FirstOrDefault();
                return FormatSingleOrEmpty("Appointments", oldest, Describe);

            default:
                var list = materialized.OrderByDescending(a => a.AppointmentDateTime).Take(MaxItemsPerCategory).ToList();
                return list.Count == 0
                    ? "Appointments: no matching appointments are currently registered in Rafiq."
                    : "Appointments:\n" + string.Join("\n", list.Select(a => $"- {Describe(a)}"));
        }
    }

    private async Task<string?> BuildLabReportsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var reports = await _labReportRepository.GetAllByProfileIdAsync(profileId, cancellationToken);

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        IEnumerable<LabReport> items = reports;
        if (terms is not null)
            items = items.Where(r => ContainsAny(r.LabName, terms) || r.Results.Any(res => ContainsAny(res.TestName, terms)));
        if (intent.Timeframe != HealthQueryTimeframe.None)
            items = items.Where(r => IsWithinTimeframe(r.ReportDate, intent.Timeframe));

        var materialized = items.ToList();

        string Describe(LabReport r)
        {
            var resultLines = r.Results
                .Where(res => terms is null || ContainsAny(res.TestName, terms))
                .Select(res => $"    - {res.TestName}: {res.Value} {res.Unit} (normal range: {res.NormalRange}{(string.IsNullOrEmpty(res.Status) ? "" : $", status: {res.Status}")})")
                .ToList();

            var header = $"- {r.LabName} on {r.ReportDate:yyyy-MM-dd} (doctor: {r.DoctorName})";
            return resultLines.Count > 0 ? header + "\n" + string.Join("\n", resultLines) : header;
        }

        switch (intent.Operation)
        {
            case HealthQueryOperation.Count:
                return DescribeCount("Lab reports", intent.SearchTerm, materialized.Count);

            case HealthQueryOperation.Exists:
                var firstMatch = materialized.FirstOrDefault();
                return DescribeExists("Lab reports", intent.SearchTerm, materialized.Count,
                    firstMatch is null ? null : $"{firstMatch.LabName} on {firstMatch.ReportDate:yyyy-MM-dd}");

            case HealthQueryOperation.GetLatest:
                return FormatSingleOrEmpty("Lab reports", materialized.OrderByDescending(r => r.ReportDate).FirstOrDefault(), Describe);

            case HealthQueryOperation.GetOldest:
                return FormatSingleOrEmpty("Lab reports", materialized.OrderBy(r => r.ReportDate).FirstOrDefault(), Describe);

            default:
                var list = materialized.OrderByDescending(r => r.ReportDate).Take(MaxItemsPerCategory).ToList();
                return list.Count == 0
                    ? "Lab reports: no matching lab reports are currently registered in Rafiq."
                    : "Lab reports:\n" + string.Join("\n", list.Select(Describe));
        }
    }

    private async Task<string?> BuildPrescriptionsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var prescriptions = await _prescriptionRepository.GetAllByProfileIdAsync(profileId, cancellationToken);

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        IEnumerable<Prescription> items = prescriptions;
        if (terms is not null)
            items = items.Where(p => ContainsAny(p.DoctorName, terms) || p.Medicines.Any(m => ContainsAny(m.MedicineName, terms)));
        if (intent.Timeframe != HealthQueryTimeframe.None)
            items = items.Where(p => IsWithinTimeframe(p.PrescriptionDate, intent.Timeframe));

        var materialized = items.ToList();

        string Describe(Prescription p)
        {
            var meds = p.Medicines
                .Where(m => terms is null || ContainsAny(m.MedicineName, terms))
                .Select(m => $"    - {m.MedicineName} ({m.Dosage}, {m.Frequency})")
                .ToList();

            var header = $"- Prescribed by {p.DoctorName} on {p.PrescriptionDate:yyyy-MM-dd}";
            return meds.Count > 0 ? header + "\n" + string.Join("\n", meds) : header;
        }

        switch (intent.Operation)
        {
            case HealthQueryOperation.Count:
                return DescribeCount("Prescriptions", intent.SearchTerm, materialized.Count);

            case HealthQueryOperation.Exists:
                var firstMatch = materialized.FirstOrDefault();
                return DescribeExists("Prescriptions", intent.SearchTerm, materialized.Count,
                    firstMatch is null ? null : $"prescribed by {firstMatch.DoctorName} on {firstMatch.PrescriptionDate:yyyy-MM-dd}");

            case HealthQueryOperation.GetLatest:
                return FormatSingleOrEmpty("Prescriptions", materialized.OrderByDescending(p => p.PrescriptionDate).FirstOrDefault(), Describe);

            case HealthQueryOperation.GetOldest:
                return FormatSingleOrEmpty("Prescriptions", materialized.OrderBy(p => p.PrescriptionDate).FirstOrDefault(), Describe);

            default:
                var list = materialized.OrderByDescending(p => p.PrescriptionDate).Take(MaxItemsPerCategory).ToList();
                return list.Count == 0
                    ? "Prescriptions: no matching prescriptions are currently registered in Rafiq."
                    : "Prescriptions:\n" + string.Join("\n", list.Select(Describe));
        }
    }

    private async Task<string?> BuildImagingReportsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken cancellationToken)
    {
        var reports = await _imagingReportRepository.GetAllByProfileIdAsync(profileId, cancellationToken);

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        IEnumerable<ImagingReport> items = reports;
        if (terms is not null)
            items = items.Where(r => ContainsAny(r.ImagingType, terms) || ContainsAny(r.BodyPart, terms) || ContainsAny(r.DoctorName, terms));
        if (intent.Timeframe != HealthQueryTimeframe.None)
            items = items.Where(r => IsWithinTimeframe(r.ReportDate, intent.Timeframe));

        var materialized = items.ToList();
        string Describe(ImagingReport r) => $"{r.ImagingType} of {r.BodyPart} on {r.ReportDate:yyyy-MM-dd}: {r.Impression}";

        switch (intent.Operation)
        {
            case HealthQueryOperation.Count:
                return DescribeCount("Imaging reports", intent.SearchTerm, materialized.Count);

            case HealthQueryOperation.Exists:
                var firstMatch = materialized.FirstOrDefault();
                return DescribeExists("Imaging reports", intent.SearchTerm, materialized.Count,
                    firstMatch is null ? null : $"{firstMatch.ImagingType} of {firstMatch.BodyPart} on {firstMatch.ReportDate:yyyy-MM-dd}");

            case HealthQueryOperation.GetLatest:
                return FormatSingleOrEmpty("Imaging reports", materialized.OrderByDescending(r => r.ReportDate).FirstOrDefault(), Describe);

            case HealthQueryOperation.GetOldest:
                return FormatSingleOrEmpty("Imaging reports", materialized.OrderBy(r => r.ReportDate).FirstOrDefault(), Describe);

            default:
                var list = materialized.OrderByDescending(r => r.ReportDate).Take(MaxItemsPerCategory).ToList();
                return list.Count == 0
                    ? "Imaging reports: no matching imaging reports are currently registered in Rafiq."
                    : "Imaging reports:\n" + string.Join("\n", list.Select(Describe));
        }
    }

    private static bool ContainsAny(string? candidate, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrEmpty(candidate))
            return false;

        foreach (var term in terms)
        {
            if (candidate.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsWithinTimeframe(DateOnly date, HealthQueryTimeframe timeframe)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return timeframe switch
        {
            HealthQueryTimeframe.Today => date == today,
            HealthQueryTimeframe.ThisWeek => date >= StartOfWeek(today) && date <= today,
            HealthQueryTimeframe.ThisMonth => date.Year == today.Year && date.Month == today.Month,
            _ => true
        };
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static string DescribeCount(string categoryLabel, string? searchTerm, int count) =>
        searchTerm is null
            ? $"{categoryLabel}: {count} record(s) registered."
            : $"{categoryLabel}: {count} record(s) matching \"{searchTerm}\".";

    private static string DescribeExists(string categoryLabel, string? searchTerm, int matchCount, string? firstMatchDescription)
    {
        if (searchTerm is null)
        {
            return matchCount > 0
                ? $"{categoryLabel}: yes, {matchCount} record(s) are registered."
                : $"{categoryLabel}: no records are currently registered in Rafiq.";
        }

        return matchCount > 0
            ? $"{categoryLabel}: yes, a matching record was found for \"{searchTerm}\"{(firstMatchDescription is null ? "" : $" - {firstMatchDescription}")}."
            : $"{categoryLabel}: no record matching \"{searchTerm}\" is currently registered in Rafiq.";
    }

    private static string FormatSingleOrEmpty<T>(string categoryLabel, T? item, Func<T, string> format) where T : class =>
        item is null
            ? $"{categoryLabel}: no matching records are currently registered in Rafiq."
            : $"{categoryLabel}: {format(item)}";
}
