using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.AI.Resolution;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;
using System.Text;

namespace Rafiq.Application.Features.AiChat.Services;

/// <summary>
/// Renders a validated <see cref="ParsedHealthQueryIntent"/> into a compact, plain-text
/// health context for the AI prompt. Supports both single-profile and family-wide scopes.
/// Only queries the categories present in the intent, applies searchTerm/timeframe filters
/// in-memory, and caps list sizes so only the minimum data needed is ever sent to the model.
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
    private readonly IMedicineReminderRepository _medicineReminderRepository;
    private readonly IGeneralDocumentRepository _generalDocumentRepository;
    private readonly IEmergencyContactRepository _emergencyContactRepository;
    private readonly ICurrentUserService _currentUserService;

    public HealthQueryContextBuilder(
        IPatientProfileRepository patientProfileRepository,
        IAppointmentRepository appointmentRepository,
        IUserMedicineRepository userMedicineRepository,
        ILabReportRepository labReportRepository,
        IPrescriptionRepository prescriptionRepository,
        IImagingReportRepository imagingReportRepository,
        IMedicineReminderRepository medicineReminderRepository,
        IGeneralDocumentRepository generalDocumentRepository,
        IEmergencyContactRepository emergencyContactRepository,
        ICurrentUserService currentUserService)
    {
        _patientProfileRepository = patientProfileRepository;
        _appointmentRepository = appointmentRepository;
        _userMedicineRepository = userMedicineRepository;
        _labReportRepository = labReportRepository;
        _prescriptionRepository = prescriptionRepository;
        _imagingReportRepository = imagingReportRepository;
        _medicineReminderRepository = medicineReminderRepository;
        _generalDocumentRepository = generalDocumentRepository;
        _emergencyContactRepository = emergencyContactRepository;
        _currentUserService = currentUserService;
    }

    public async Task<string> BuildAsync(
        ParsedHealthQueryIntent intent,
        QueryScope scope,
        CancellationToken cancellationToken = default)
    {
        if (intent.HasNoCategories)
            return string.Empty;

        return scope switch
        {
            SingleProfileScope single => await BuildSingleProfileAsync(intent, single, cancellationToken),
            FamilyWideScope family    => await BuildFamilyWideAsync(intent, family, cancellationToken),
            _                         => string.Empty
        };
    }

    // ── Single-profile path ──────────────────────────────────────────────────

    private async Task<string> BuildSingleProfileAsync(
        ParsedHealthQueryIntent intent,
        SingleProfileScope scope,
        CancellationToken ct)
    {
        var sections = new List<string>();

        foreach (var category in intent.Categories)
        {
            var section = category switch
            {
                HealthQueryCategory.Profile            => await BuildProfileSectionAsync(scope.ProfileId, ct),
                HealthQueryCategory.Allergies          => await BuildAllergiesSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.ChronicDiseases    => await BuildChronicDiseasesSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.Medicines          => await BuildMedicinesSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.Appointments       => await BuildAppointmentsSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.LabReports         => await BuildLabReportsSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.Prescriptions      => await BuildPrescriptionsSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.ImagingReports     => await BuildImagingReportsSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.MedicationReminders=> await BuildMedicationRemindersSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.GeneralDocuments   => await BuildGeneralDocumentsSectionAsync(scope.ProfileId, intent, ct),
                HealthQueryCategory.EmergencyContacts  => await BuildEmergencyContactsSectionAsync(ct),
                HealthQueryCategory.FamilyOverview     => null, // no data when viewing single profile
                _                                      => null
            };

            if (!string.IsNullOrWhiteSpace(section))
                sections.Add(section!);
        }

        // Prefix with profile name when viewing a family member (not self)
        var result = string.Join("\n\n", sections);
        if (!string.IsNullOrEmpty(scope.DisplayName) && !string.IsNullOrWhiteSpace(result))
            result = $"[Data for: {scope.DisplayName}]\n\n{result}";

        return result;
    }

    // ── Family-wide path ─────────────────────────────────────────────────────

    private async Task<string> BuildFamilyWideAsync(
        ParsedHealthQueryIntent intent,
        FamilyWideScope scope,
        CancellationToken ct)
    {
        if (scope.Profiles.Count == 0)
            return "Family overview: no accessible family members found.";

        var sections = new List<string>();

        foreach (var category in intent.Categories)
        {
            string? section = category switch
            {
                HealthQueryCategory.FamilyOverview => await BuildFamilyOverviewSectionAsync(scope, ct),
                _ => await BuildFamilyWideCategorySectionAsync(category, scope, intent, ct)
            };

            if (!string.IsNullOrWhiteSpace(section))
                sections.Add(section!);
        }

        return string.Join("\n\n", sections);
    }

    private async Task<string> BuildFamilyOverviewSectionAsync(FamilyWideScope scope, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Family overview ({scope.Profiles.Count} accessible member(s)):");

        foreach (var profile in scope.Profiles)
        {
            var relationship = profile.Relationship?.ToString() ?? "Member";
            var label = $"{profile.FirstName} {profile.LastName} ({relationship})";

            var patientProfile = await _patientProfileRepository.GetByIdAsync(profile.ProfileId, ct);
            var medicines = await _userMedicineRepository.GetAllByProfileIdAsync(profile.ProfileId, ct);
            var appointments = await _appointmentRepository.GetUpcomingByUserHealthProfileIdAsync(profile.ProfileId, ct);
            var labReports = await _labReportRepository.GetAllByProfileIdAsync(profile.ProfileId, ct);

            sb.AppendLine();
            sb.AppendLine(label + ":");
            if (patientProfile is not null)
            {
                sb.AppendLine($"  - Allergies: {patientProfile.Allergies.Count}" +
                    (patientProfile.Allergies.Count > 0
                        ? $" ({string.Join(", ", patientProfile.Allergies.Take(3).Select(a => $"{a.Name} - {a.Severity}"))})"
                        : string.Empty));
                sb.AppendLine($"  - Chronic diseases: {patientProfile.ChronicDiseases.Count}" +
                    (patientProfile.ChronicDiseases.Count > 0
                        ? $" ({string.Join(", ", patientProfile.ChronicDiseases.Take(3).Select(d => d.Name))})"
                        : string.Empty));
            }
            sb.AppendLine($"  - Medicines registered: {medicines.Count}");
            sb.AppendLine($"  - Upcoming appointments: {appointments.Count}");
            sb.AppendLine($"  - Lab reports: {labReports.Count}");
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string?> BuildFamilyWideCategorySectionAsync(
        HealthQueryCategory category,
        FamilyWideScope scope,
        ParsedHealthQueryIntent intent,
        CancellationToken ct)
    {
        var header = $"{CategoryLabel(category)} (family - {scope.Profiles.Count} member(s)):";
        var perProfile = new List<string>();

        foreach (var profile in scope.Profiles)
        {
            var relationship = profile.Relationship?.ToString() ?? "Member";
            var label = $"{profile.FirstName} {profile.LastName} ({relationship})";

            var section = category switch
            {
                HealthQueryCategory.Profile            => await BuildProfileSectionAsync(profile.ProfileId, ct),
                HealthQueryCategory.Allergies          => await BuildAllergiesSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.ChronicDiseases    => await BuildChronicDiseasesSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.Medicines          => await BuildMedicinesSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.Appointments       => await BuildAppointmentsSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.LabReports         => await BuildLabReportsSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.Prescriptions      => await BuildPrescriptionsSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.ImagingReports     => await BuildImagingReportsSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.MedicationReminders=> await BuildMedicationRemindersSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.GeneralDocuments   => await BuildGeneralDocumentsSectionAsync(profile.ProfileId, intent, ct),
                HealthQueryCategory.EmergencyContacts  => await BuildEmergencyContactsSectionAsync(ct),
                _                                      => null
            };

            if (!string.IsNullOrWhiteSpace(section))
                perProfile.Add($"{label}:\n{section}");
        }

        return perProfile.Count == 0
            ? null
            : header + "\n\n" + string.Join("\n\n", perProfile);
    }

    // ── Single-profile section builders ──────────────────────────────────────

    private async Task<string?> BuildProfileSectionAsync(Guid profileId, CancellationToken ct)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId, ct);
        if (profile is null)
            return null;

        var medicines = await _userMedicineRepository.GetAllByProfileIdAsync(profileId, ct);
        var appointments = await _appointmentRepository.GetUpcomingByUserHealthProfileIdAsync(profileId, ct);
        var labReports = await _labReportRepository.GetAllByProfileIdAsync(profileId, ct);

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

    private async Task<string?> BuildAllergiesSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId, ct);
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

    private async Task<string?> BuildChronicDiseasesSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId, ct);
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

    private async Task<string?> BuildMedicinesSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var medicines = await _userMedicineRepository.GetAllByProfileIdAsync(profileId, ct);

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

    private async Task<string?> BuildAppointmentsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var appointments = await _appointmentRepository.GetAllByUserHealthProfileIdAsync(profileId, ct);

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

            case HealthQueryOperation.GetUpcoming:
                var upcoming = materialized
                    .Where(a => a.AppointmentDateTime >= now && a.AppointmentDateTime <= now.AddDays(7))
                    .OrderBy(a => a.AppointmentDateTime)
                    .Take(MaxItemsPerCategory)
                    .ToList();
                return upcoming.Count == 0
                    ? "Appointments: no upcoming appointments in the next 7 days."
                    : "Upcoming appointments (next 7 days):\n" + string.Join("\n", upcoming.Select(a => $"- {Describe(a)}"));

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

    private async Task<string?> BuildLabReportsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var reports = await _labReportRepository.GetAllByProfileIdAsync(profileId, ct);

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

    private async Task<string?> BuildPrescriptionsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var prescriptions = await _prescriptionRepository.GetAllByProfileIdAsync(profileId, ct);

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

    private async Task<string?> BuildImagingReportsSectionAsync(Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var reports = await _imagingReportRepository.GetAllByProfileIdAsync(profileId, ct);

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

    private async Task<string?> BuildMedicationRemindersSectionAsync(
        Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var reminders = await _medicineReminderRepository.GetAllWithMedicineByProfileIdAsync(profileId, ct);

        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var now      = DateTime.UtcNow;

        // Apply timeframe filter
        IEnumerable<MedicineReminder> items = reminders;
        if (intent.Timeframe == HealthQueryTimeframe.Today)
            items = items.Where(r => r.IsEnabled && r.StartDate <= today && r.EndDate >= today);
        else if (intent.Timeframe == HealthQueryTimeframe.Tomorrow)
            items = items.Where(r => r.IsEnabled && r.StartDate <= tomorrow && r.EndDate >= tomorrow);

        // Apply searchTerm against medicine name
        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        if (terms is not null)
            items = items.Where(r => ContainsAny(r.UserMedicine.MedicineName, terms));

        string DescribeReminder(MedicineReminder r)
        {
            var time = $"{r.ReminderTime:hh\\:mm}";
            var lastTrigger = r.LastTriggeredAt.HasValue
                ? $", last taken: {r.LastTriggeredAt.Value:yyyy-MM-dd HH:mm}"
                : ", never taken";
            return $"- {r.UserMedicine.MedicineName}: {time} ({r.RepeatType}, until {r.EndDate:yyyy-MM-dd}{lastTrigger})";
        }

        switch (intent.Operation)
        {
            case HealthQueryOperation.GetNext:
            {
                // Next upcoming active reminder (today, time not yet passed)
                var activeToday = items
                    .Where(r => r.IsEnabled && r.StartDate <= today && r.EndDate >= today)
                    .ToList();
                var nextToday = activeToday
                    .Where(r => r.ReminderTime > now.TimeOfDay)
                    .OrderBy(r => r.ReminderTime)
                    .FirstOrDefault();
                if (nextToday is not null)
                    return $"Medication reminders - next dose:\n{DescribeReminder(nextToday)}";

                // If all of today's passed, find earliest active tomorrow
                var nextTomorrow = items
                    .Where(r => r.IsEnabled && r.StartDate <= tomorrow && r.EndDate >= tomorrow)
                    .OrderBy(r => r.ReminderTime)
                    .FirstOrDefault();
                return nextTomorrow is not null
                    ? $"Medication reminders - next dose (tomorrow):\n{DescribeReminder(nextTomorrow)}"
                    : "Medication reminders: no upcoming reminders scheduled.";
            }

            case HealthQueryOperation.GetLatest:
            {
                var latest = items
                    .Where(r => r.LastTriggeredAt.HasValue)
                    .OrderByDescending(r => r.LastTriggeredAt)
                    .FirstOrDefault();
                return latest is not null
                    ? $"Medication reminders - last dose taken:\n{DescribeReminder(latest)}"
                    : "Medication reminders: no doses recorded as taken yet.";
            }

            case HealthQueryOperation.GetUpcoming:
            {
                var upcoming = reminders
                    .Where(r => r.IsEnabled && r.StartDate <= today.AddDays(7) && r.EndDate >= today)
                    .OrderBy(r => r.ReminderTime)
                    .Take(MaxItemsPerCategory)
                    .ToList();
                return upcoming.Count == 0
                    ? "Medication reminders: no active reminders in the next 7 days."
                    : "Upcoming medication reminders (next 7 days):\n" + string.Join("\n", upcoming.Select(DescribeReminder));
            }

            case HealthQueryOperation.GetMissed:
            {
                // Active today, time has passed, not triggered today
                var missed = reminders
                    .Where(r => r.IsEnabled
                        && r.StartDate <= today
                        && r.EndDate >= today
                        && r.ReminderTime <= now.TimeOfDay
                        && (r.LastTriggeredAt is null
                            || DateOnly.FromDateTime(r.LastTriggeredAt.Value) < today))
                    .OrderBy(r => r.ReminderTime)
                    .Take(MaxItemsPerCategory)
                    .ToList();
                return missed.Count == 0
                    ? "Medication reminders: no missed doses today."
                    : $"Missed medication reminders today ({missed.Count}):\n" +
                      string.Join("\n", missed.Select(DescribeReminder));
            }

            case HealthQueryOperation.Count:
            {
                var activeCount = items.Count(r => r.IsEnabled && r.StartDate <= today && r.EndDate >= today);
                return DescribeCount("Medication reminders (active today)", intent.SearchTerm, activeCount);
            }

            case HealthQueryOperation.Exists:
            {
                var any = items.Any(r => r.IsEnabled && r.StartDate <= today && r.EndDate >= today);
                return any
                    ? "Medication reminders: yes, active reminders are set up."
                    : "Medication reminders: no active reminders are currently set up.";
            }

            default:
            {
                var list = items
                    .Where(r => r.IsEnabled && r.StartDate <= today && r.EndDate >= today)
                    .OrderBy(r => r.ReminderTime)
                    .Take(MaxItemsPerCategory)
                    .ToList();
                return list.Count == 0
                    ? "Medication reminders: no active reminders are currently set up in Rafiq."
                    : "Medication reminders (active today):\n" + string.Join("\n", list.Select(DescribeReminder));
            }
        }
    }

    private async Task<string?> BuildGeneralDocumentsSectionAsync(
        Guid profileId, ParsedHealthQueryIntent intent, CancellationToken ct)
    {
        var documents = await _generalDocumentRepository.GetAllByUserIdAsync(profileId, ct);

        var terms = intent.SearchTerm is null ? null : MedicalTermSynonyms.Expand(intent.SearchTerm);
        IEnumerable<GeneralDocument> items = documents;
        if (terms is not null)
            items = items.Where(d =>
                ContainsAny(d.Title, terms) ||
                ContainsAny(d.DocumentType, terms) ||
                ContainsAny(d.DoctorName, terms) ||
                ContainsAny(d.HospitalOrClinic, terms));

        var list = items.OrderByDescending(d => d.CreatedAt).ToList();

        string Describe(GeneralDocument d)
        {
            var parts = new List<string> { d.Title };
            if (!string.IsNullOrEmpty(d.DocumentType)) parts.Add($"type: {d.DocumentType}");
            if (!string.IsNullOrEmpty(d.DoctorName))   parts.Add($"doctor: {d.DoctorName}");
            if (!string.IsNullOrEmpty(d.DocumentDate)) parts.Add($"date: {d.DocumentDate}");
            if (!string.IsNullOrEmpty(d.AiSummary))    parts.Add($"summary: {d.AiSummary}");
            return string.Join(", ", parts);
        }

        return intent.Operation switch
        {
            HealthQueryOperation.Count  => DescribeCount("General documents", intent.SearchTerm, list.Count),
            HealthQueryOperation.Exists => DescribeExists("General documents", intent.SearchTerm, list.Count,
                list.Count > 0 ? list[0].Title : null),
            HealthQueryOperation.GetLatest => FormatSingleOrEmpty("General documents", list.FirstOrDefault(), Describe),
            HealthQueryOperation.GetOldest => FormatSingleOrEmpty("General documents",
                list.OrderBy(d => d.CreatedAt).FirstOrDefault(), Describe),
            _ => list.Count == 0
                ? "General documents: no documents are currently uploaded in Rafiq."
                : "General documents:\n" + string.Join("\n", list.Take(MaxItemsPerCategory).Select(d => $"- {Describe(d)}"))
        };
    }

    private async Task<string?> BuildEmergencyContactsSectionAsync(CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return "Emergency contacts: unable to retrieve (not authenticated).";

        var contacts = await _emergencyContactRepository.GetAllByUserIdAsync(userId.Value, ct);

        if (contacts.Count == 0)
            return "Emergency contacts: no emergency contacts are currently registered in Rafiq.";

        return "Emergency contacts:\n" +
               string.Join("\n", contacts.Select(c =>
                   $"- {c.Name} ({c.Relation}): {c.PhoneNumber}"));
    }

    // ── Utility helpers ───────────────────────────────────────────────────────

    private static string CategoryLabel(HealthQueryCategory category) => category switch
    {
        HealthQueryCategory.Profile             => "Profile",
        HealthQueryCategory.Allergies           => "Allergies",
        HealthQueryCategory.ChronicDiseases     => "Chronic diseases",
        HealthQueryCategory.Medicines           => "Medicines",
        HealthQueryCategory.Appointments        => "Appointments",
        HealthQueryCategory.LabReports          => "Lab reports",
        HealthQueryCategory.Prescriptions       => "Prescriptions",
        HealthQueryCategory.ImagingReports      => "Imaging reports",
        HealthQueryCategory.MedicationReminders => "Medication reminders",
        HealthQueryCategory.FamilyOverview      => "Family overview",
        HealthQueryCategory.GeneralDocuments    => "General documents",
        HealthQueryCategory.EmergencyContacts   => "Emergency contacts",
        _                                       => category.ToString()
    };

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
            HealthQueryTimeframe.Today     => date == today,
            HealthQueryTimeframe.Tomorrow  => date == today.AddDays(1),
            HealthQueryTimeframe.ThisWeek  => date >= StartOfWeek(today) && date <= today,
            HealthQueryTimeframe.ThisMonth => date.Year == today.Year && date.Month == today.Month,
            _                              => true
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
