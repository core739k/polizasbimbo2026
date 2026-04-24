namespace PolizasBimbo.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
