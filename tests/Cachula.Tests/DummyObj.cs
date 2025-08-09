namespace Cachula.Tests;

public class DummyObj
{
    public int Id { get; init; }
    public string? Name { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is not DummyObj other)
        {
            return false;
        }

        return Id == other.Id && Name == other.Name;
    }

    public override int GetHashCode() => HashCode.Combine(Id, Name);
}
