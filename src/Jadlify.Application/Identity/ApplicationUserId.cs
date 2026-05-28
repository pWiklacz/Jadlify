namespace Jadlify.Application.Identity;

public sealed record ApplicationUserId
{
    public ApplicationUserId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
