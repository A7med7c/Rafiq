using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class HealthcareProvider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
