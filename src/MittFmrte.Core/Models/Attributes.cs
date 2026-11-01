namespace MittFmrte.Core.Models;

/// <summary>The attribute names used by Football Manager, grouped like in the game.</summary>
public static class Attributes
{
    public const string Corners = "Corners";
    public const string Crossing = "Crossing";
    public const string Dribbling = "Dribbling";
    public const string Finishing = "Finishing";
    public const string FirstTouch = "First Touch";
    public const string FreeKick = "Free Kick Taking";
    public const string Heading = "Heading";
    public const string LongShots = "Long Shots";
    public const string LongThrows = "Long Throws";
    public const string Marking = "Marking";
    public const string Passing = "Passing";
    public const string PenaltyTaking = "Penalty Taking";
    public const string Tackling = "Tackling";
    public const string Technique = "Technique";

    public const string Aggression = "Aggression";
    public const string Anticipation = "Anticipation";
    public const string Bravery = "Bravery";
    public const string Composure = "Composure";
    public const string Concentration = "Concentration";
    public const string Decisions = "Decisions";
    public const string Determination = "Determination";
    public const string Flair = "Flair";
    public const string Leadership = "Leadership";
    public const string OffTheBall = "Off the Ball";
    public const string Positioning = "Positioning";
    public const string Teamwork = "Teamwork";
    public const string Vision = "Vision";
    public const string WorkRate = "Work Rate";

    public const string Acceleration = "Acceleration";
    public const string Agility = "Agility";
    public const string Balance = "Balance";
    public const string JumpingReach = "Jumping Reach";
    public const string NaturalFitness = "Natural Fitness";
    public const string Pace = "Pace";
    public const string Stamina = "Stamina";
    public const string Strength = "Strength";

    public const string AerialReach = "Aerial Reach";
    public const string CommandOfArea = "Command of Area";
    public const string Communication = "Communication";
    public const string Handling = "Handling";
    public const string Kicking = "Kicking";
    public const string OneOnOnes = "One on Ones";
    public const string Reflexes = "Reflexes";
    public const string Throwing = "Throwing";
    public const string Eccentricity = "Eccentricity";
    public const string Punching = "Tendency to Punch";
    public const string RushingOut = "Rushing Out";

    public const string Consistency = "Consistency";
    public const string Dirtiness = "Dirtiness";
    public const string ImportantMatches = "Important Matches";
    public const string InjuryProneness = "Injury Proneness";
    public const string Versatility = "Versatility";

    public const string LeftFoot = "Left Foot";
    public const string RightFoot = "Right Foot";

    public static readonly IReadOnlyList<string> Technical =
    [
        Corners, Crossing, Dribbling, Finishing, FirstTouch, FreeKick, Heading,
        LongShots, LongThrows, Marking, Passing, PenaltyTaking, Tackling, Technique,
    ];

    public static readonly IReadOnlyList<string> Mental =
    [
        Aggression, Anticipation, Bravery, Composure, Concentration, Decisions, Determination,
        Flair, Leadership, OffTheBall, Positioning, Teamwork, Vision, WorkRate,
    ];

    public static readonly IReadOnlyList<string> Physical =
    [
        Acceleration, Agility, Balance, JumpingReach, NaturalFitness, Pace, Stamina, Strength,
    ];

    public static readonly IReadOnlyList<string> Goalkeeping =
    [
        AerialReach, CommandOfArea, Communication, Eccentricity, Handling, Kicking, OneOnOnes,
        Punching, Reflexes, RushingOut, Throwing,
    ];

    public static readonly IReadOnlyList<string> Hidden =
    [
        Consistency, Dirtiness, ImportantMatches, InjuryProneness, Versatility,
    ];

    public static readonly IReadOnlyList<string> Feet = [LeftFoot, RightFoot];

    /// <summary>
    /// The order of the 54 player attributes as stored in an FM24 save, verified against
    /// Kimpembe, Hakimi and Navas in a real save.
    /// </summary>
    public static readonly IReadOnlyList<string> SaveFileOrder =
    [
        Crossing, Dribbling, Finishing, Heading, LongShots, Marking, OffTheBall, Passing, PenaltyTaking,
        Tackling, Vision, Handling, AerialReach, CommandOfArea, Communication, Kicking, Throwing,
        Anticipation, Decisions, OneOnOnes, Positioning, Reflexes, FirstTouch, Technique, LeftFoot,
        RightFoot, Flair, Corners, Teamwork, WorkRate, LongThrows, Eccentricity, RushingOut, Punching,
        Acceleration, FreeKick, Strength, Stamina, Pace, JumpingReach, Leadership, Dirtiness, Balance,
        Bravery, Consistency, Aggression, Agility, ImportantMatches, InjuryProneness, Versatility,
        NaturalFitness, Determination, Composure, Concentration,
    ];

    public static AttributeGroup GroupOf(string name) =>
        Technical.Contains(name) ? AttributeGroup.Technical
        : Mental.Contains(name) ? AttributeGroup.Mental
        : Physical.Contains(name) ? AttributeGroup.Physical
        : Goalkeeping.Contains(name) ? AttributeGroup.Goalkeeping
        : Feet.Contains(name) ? AttributeGroup.Feet
        : AttributeGroup.Hidden;
}
