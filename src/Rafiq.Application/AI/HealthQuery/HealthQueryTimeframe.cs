namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// An optional additional date-range filter layered on top of a category + operation.
/// </summary>
public enum HealthQueryTimeframe
{
    None,
    Today,
    ThisWeek,
    ThisMonth
}
