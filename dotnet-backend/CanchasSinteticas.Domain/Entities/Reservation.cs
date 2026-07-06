namespace CanchasSinteticas.Domain.Entities;

public class Reservation(
    string id,
    string fieldId,
    string userId,
    DateOnly date,
    TimeOnly startTime,
    TimeOnly endTime,
    string status)
{
    public string Id { get; } = id;
    public string FieldId { get; } = fieldId;
    public string UserId { get; } = userId;
    public DateOnly Date { get; } = date;
    public TimeOnly StartTime { get; } = startTime;
    public TimeOnly EndTime { get; } = endTime;
    public string Status { get; } = status;

    public DateTime StartDateTime =>
        Date.ToDateTime(StartTime);
}
