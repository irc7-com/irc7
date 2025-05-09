namespace Irc.Enumerations;

public sealed class EnumSubjectZone
{
    public static readonly EnumSubjectZone GT = new("GT");
    public static readonly EnumSubjectZone ST = new("ST");
    public static readonly EnumSubjectZone ET = new("ET");

    public string Value { get; }

    private EnumSubjectZone(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) =>
        obj is EnumSubjectZone other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
