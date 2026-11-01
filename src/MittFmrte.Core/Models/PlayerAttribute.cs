namespace MittFmrte.Core.Models;

public enum AttributeGroup { Technical, Mental, Physical, Goalkeeping, Hidden, Feet }

/// <summary>One 1-20 attribute such as "Passing". Observable so edits show up immediately.</summary>
public sealed class PlayerAttribute : ObservableObject
{
    public const int Min = 1;
    public const int Max = 20;

    private int _value;

    public PlayerAttribute(string name, AttributeGroup group, int value)
    {
        Name = name;
        Group = group;
        _value = Clamp(value);
    }

    public string Name { get; }
    public AttributeGroup Group { get; }

    public int Value
    {
        get => _value;
        set => Set(ref _value, Clamp(value));
    }

    public static int Clamp(int v) => Math.Clamp(v, Min, Max);
}
