using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
