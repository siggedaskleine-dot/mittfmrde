using System.Collections.ObjectModel;

namespace MittFmrte.Core.Models;

public sealed class Player : ObservableObject
{
    private string _firstName = "";
    private string _lastName = "";
    private int _squadNumber;
    private int _currentAbility;
    private int _potentialAbility;
    private int _reputation;
    private long _value;
    private long? _wage;
    private DateOnly? _contractExpires;
    private int _injuryDays;
    private int _banMatches;
    private int _morale;
    private int _clubId;

    public required int Id { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required string Nationality { get; init; }
    public required IReadOnlyList<Position> Positions { get; init; }

    /// <summary>Optional full name from the save (e.g. "Keylor Antonio Navas Gamboa").</summary>
    public string? FullLegalName { get; init; }

    /// <summary>FM's 15 position ratings (1-20), in <see cref="FmPositions.Order"/>. Empty for demo data.</summary>
    public IReadOnlyList<int> PositionRatings { get; init; } = [];

    /// <summary>Where this player's data lives in the decompressed save, for writing back later.</summary>
    public long? SaveOffset { get; init; }

    /// <summary>Attributes in display order. Keyed lookup via <see cref="Attr"/>.</summary>
    public ObservableCollection<PlayerAttribute> AttributeList { get; } = [];

    public string FirstName { get => _firstName; set { if (Set(ref _firstName, value)) OnPropertyChanged(nameof(FullName)); } }
    public string LastName { get => _lastName; set { if (Set(ref _lastName, value)) OnPropertyChanged(nameof(FullName)); } }
    public string FullName => string.IsNullOrEmpty(CommonName) ? $"{FirstName} {LastName}".Trim() : CommonName!;

    private string? _commonName;
    /// <summary>FM common name such as "Marquinhos" or "Vitinha". Shown instead of first + last name.</summary>
    public string? CommonName { get => _commonName; set { if (Set(ref _commonName, value)) OnPropertyChanged(nameof(FullName)); } }

    public int ClubId { get => _clubId; set => Set(ref _clubId, value); }
    public int SquadNumber { get => _squadNumber; set => Set(ref _squadNumber, value); }

    /// <summary>Current ability, 1-200 like in FM.</summary>
    public int CurrentAbility { get => _currentAbility; set => Set(ref _currentAbility, Math.Clamp(value, 1, 200)); }

    /// <summary>Potential ability, 1-200. Never below CA.</summary>
    public int PotentialAbility { get => _potentialAbility; set => Set(ref _potentialAbility, Math.Clamp(value, 1, 200)); }

    /// <summary>World reputation, 0-10000 like in FM.</summary>
    public int Reputation { get => _reputation; set => Set(ref _reputation, Math.Clamp(value, 0, 10000)); }

    public long Value { get => _value; set => Set(ref _value, Math.Max(0, value)); }
    /// <summary>Weekly wage, or null when the save file reader can't read it yet.</summary>
    public long? WeeklyWage { get => _wage; set => Set(ref _wage, value is null ? null : Math.Max(0, value.Value)); }
    /// <summary>Contract end date, or null when unknown.</summary>
    public DateOnly? ContractExpires { get => _contractExpires; set => Set(ref _contractExpires, value); }

    public int InjuryDays { get => _injuryDays; set { if (Set(ref _injuryDays, Math.Max(0, value))) OnPropertyChanged(nameof(Status)); } }
    public int BanMatches { get => _banMatches; set { if (Set(ref _banMatches, Math.Max(0, value))) OnPropertyChanged(nameof(Status)); } }

    /// <summary>Morale 0-100.</summary>
    public int Morale { get => _morale; set => Set(ref _morale, Math.Clamp(value, 0, 100)); }

    public Position MainPosition => Positions[0];
    public string PositionText => PositionRatings.Count == 15
        ? FmPositions.Describe(PositionRatings)
        : string.Join(", ", Positions.Select(PositionNames.Short));

    public string Status => (InjuryDays > 0, BanMatches > 0) switch
    {
        (true, true) => "Skadad, avstängd",
        (true, false) => "Skadad",
        (false, true) => "Avstängd",
        _ => "",
    };

    public int AgeOn(DateOnly date)
    {
        var age = date.Year - BirthDate.Year;
        if (date < BirthDate.AddYears(age)) age--;
        return age;
    }

    public PlayerAttribute? Attr(string name) => AttributeList.FirstOrDefault(a => a.Name == name);

    public int AttrValue(string name) => Attr(name)?.Value ?? PlayerAttribute.Min;
}
