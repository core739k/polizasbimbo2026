using PolizasBimbo.Application.Abstractions;

namespace PolizasBimbo.Application.Tests.Helpers;

public sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; }
    public FixedClock(DateTime utcNow) => UtcNow = utcNow;
}
