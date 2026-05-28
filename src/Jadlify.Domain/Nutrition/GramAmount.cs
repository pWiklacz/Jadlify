namespace Jadlify.Domain.Nutrition;

public sealed record GramAmount
{
    public GramAmount(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

        Value = value;
    }

    public decimal Value { get; }

    public override string ToString() => $"{Value} g";
}
