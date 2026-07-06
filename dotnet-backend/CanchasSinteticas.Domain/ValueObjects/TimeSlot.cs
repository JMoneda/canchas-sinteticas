using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.ValueObjects;

public class TimeSlot
{
    public DateOnly Date { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public TimeSlot(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime.Minute % 30 != 0 || endTime.Minute % 30 != 0)
            throw new InvalidBlockError();

        var duration = endTime - startTime;
        if (duration < TimeSpan.FromHours(1))
            throw new DurationError();

        if (startTime < new TimeOnly(6, 0) || endTime > new TimeOnly(23, 0))
            throw new OperatingHoursError();

        Date = date;
        StartTime = startTime;
        EndTime = endTime;
    }

    public DateTime StartDateTime => Date.ToDateTime(StartTime);

    public bool IsBookable(DateTime now) =>
        StartDateTime - now >= TimeSpan.FromHours(1);

    public bool OverlapsWith(TimeSlot other)
    {
        if (Date != other.Date) return false;
        return StartTime < other.EndTime && other.StartTime < EndTime;
    }
}
