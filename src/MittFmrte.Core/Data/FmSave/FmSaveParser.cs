using System.Buffers.Binary;
using System.Text;
using MittFmrte.Core.Models;

namespace MittFmrte.Core.Data.FmSave;

/// <summary>
/// Reads players and clubs out of decompressed FM24 save data. The layout was worked out
/// by comparing a real save against FMRTE; see docs/save-format.md. Read-only for now.
/// </summary>
public sealed class FmSaveParser(byte[] data)
{
    private readonly byte[] _d = data;
    private static readonly UTF8Encoding StrictUtf8 = new(false, throwOnInvalidBytes: true);

    // ---- Offsets inside a player block, relative to the CA field ----
    private const int RepHome = -6, RepCurrent = -4, RepWorld = -2;
    private const int PaOffset = 2;
    private const int ClubOffset = 15;
    private const int PositionsOffset = 23, PositionCount = 15;
    private const int AttributesOffset = 38, AttributeCount = 54;
    private const int ValueOffset = 92;
    private const int BlockEnd = AttributesOffset + AttributeCount + 8;

    /// <summary>A player's name block is normally 100-1600 bytes after his CA.</summary>
    private const int MaxNameDistance = 4000;

    public sealed record NameTables(
        IReadOnlyDictionary<uint, string> First,
        IReadOnlyDictionary<uint, string> Last,
        IReadOnlyDictionary<uint, string> Common,
        int End);

    public sealed record PersonName(int Offset, string First, string Last, string? Common, string? Full, DateOnly Born);

    public sealed record ClubRecord(int Offset, int Id, string Name, string ShortName);

    public GameWorld Parse(string source, string? humanClubHint = null, IProgress<string>? progress = null)
    {
        progress?.Report("Läser namn");
        var names = FindNameTables();
        progress?.Report("Läser personer");
        var people = FindPeople(names);
        progress?.Report("Läser spelare");
        var blocks = FindPlayerBlocks(names.End);
        var gameDate = GuessGameDate(blocks);
        var players = new List<Player>(blocks.Count);
        var starts = people.Select(p => p.Offset).ToArray();

        for (var b = 0; b < blocks.Count; b++)
        {
            var c = blocks[b];
            var i = Array.BinarySearch(starts, c);
            if (i < 0) i = ~i;
            if (i >= starts.Length) continue;
            var person = people[i];
            // The name must come before the next player's block, otherwise it belongs to someone else.
            var nextBlock = b + 1 < blocks.Count ? blocks[b + 1] : int.MaxValue;
            if (person.Offset - c > MaxNameDistance || person.Offset > nextBlock) continue;
            players.Add(ReadPlayer(c, person));
        }

        progress?.Report("Läser klubbar");
        var clubRecords = FindClubs(names.End, players.Select(p => p.ClubId).ToHashSet());

        // Clubs that at least one player belongs to.
        var clubById = new Dictionary<int, ClubRecord>();
        foreach (var r in clubRecords) clubById.TryAdd(r.Id, r);
        var clubs = players
            .Where(p => p.ClubId >= 0)
            .GroupBy(p => p.ClubId)
            .Select(g => MakeClub(g.Key, g.ToList(), clubById))
            .OrderByDescending(c => c.Reputation)
            .ToList();
        var freeAgents = new Club
        {
            Id = -1, Name = "Klubblös", Country = "", League = "", Reputation = 0, ReluctantSeller = false,
        };
        clubs.Add(freeAgents);

        var human = GuessHumanClub(clubs, humanClubHint);
        return new GameWorld
        {
            Source = source,
            GameDate = gameDate,
            Clubs = clubs,
            Players = players,
            HumanClubId = human.Id,
        };
    }

    // ------------------------------------------------------------------ names

    /// <summary>
    /// Finds the first-name, last-name and common-name tables: three lists of
    /// [u32 id][u32 length][UTF-8 text] where ids start at 0.
    /// </summary>
    public NameTables FindNameTables()
    {
        var tableStarts = new List<int>();
        for (var p = 0; p < _d.Length - 64 && tableStarts.Count < 3; p++)
        {
            if (_d[p] != 0 || U32(p) != 0) continue;
            if (IsTableStart(p))
            {
                tableStarts.Add(p);
                p += 1000;
            }
        }
        if (tableStarts.Count < 3)
            throw new SaveFileException("Hittade inte namnlistorna i sparfilen. Är det en FM24-sparfil?");

        var first = ReadTable(tableStarts[0], tableStarts[1], out _);
        var last = ReadTable(tableStarts[1], tableStarts[2], out _);
        var common = ReadTable(tableStarts[2], _d.Length, out var end);
        return new NameTables(first, last, common, end);
    }

    private bool IsTableStart(int p)
    {
        for (uint expected = 0; expected < 50; expected++)
        {
            if (!TryEntry(p, out var id, out _, out var next) || id != expected) return false;
            p = next;
        }
        return true;
    }

    private Dictionary<uint, string> ReadTable(int start, int limit, out int end)
    {
        var table = new Dictionary<uint, string>();
        var p = start;
        var misses = 0;
        while (p < limit)
        {
            if (TryEntry(p, out var id, out var text, out var next))
            {
                table[id] = text;
                p = next;
                misses = 0;
            }
            else
            {
                // Tables occasionally have a short gap; give up after a long run of non-entries.
                if (++misses > 64) break;
                p++;
            }
        }
        end = p - misses;
        return table;
    }

    private bool TryEntry(int p, out uint id, out string text, out int next)
    {
        id = 0; text = ""; next = p;
        if (p + 8 > _d.Length) return false;
        id = U32(p);
        var len = U32(p + 4);
        if (len is 0 or >= 300 || p + 8 + len > _d.Length) return false;
        if (!TryUtf8(p + 8, (int)len, out text) || text.Any(char.IsControl)) return false;
        next = p + 8 + (int)len;
        return true;
    }

    /// <summary>
    /// Person name blocks: [u32 first][00][u32 last][00][u32 common|ffffffff][00][u32 len][full name][date of birth].
    /// </summary>
    public List<PersonName> FindPeople(NameTables names)
    {
        var people = new List<PersonName>();
        for (var p = names.End; p < _d.Length - 32; p++)
        {
            if (_d[p + 4] != 0 || _d[p + 9] != 0 || _d[p + 14] != 0) continue;
            if (U32(p) == 0 && U32(p + 5) == 0) continue;
            if (!names.First.TryGetValue(U32(p), out var first)) continue;
            if (!names.Last.TryGetValue(U32(p + 5), out var last)) continue;
            var commonId = U32(p + 10);
            string? common = null;
            // Id 0 is used both for "no common name" and for the first entry; treat it as none.
            if (commonId != uint.MaxValue && commonId != 0 && !names.Common.TryGetValue(commonId, out common)) continue;
            var len = U32(p + 15);
            if (len > 120) continue;
            string? full = null;
            if (len > 0 && !TryUtf8(p + 19, (int)len, out full!)) continue;
            var dob = ReadDate(p + 19 + (int)len);
            if (dob is not { Year: >= 1900 and <= 2030 } born) continue;
            people.Add(new PersonName(p, first, last, common, full, born));
        }
        return people;
    }

    // ---------------------------------------------------------------- players

    /// <summary>Finds player blocks by their shape: reputation, CA/PA, 15 position ratings and 54 attributes.</summary>
    public List<int> FindPlayerBlocks(int from)
    {
        var blocks = new List<int>();
        for (var c = Math.Max(from, 8); c < _d.Length - BlockEnd; c++)
        {
            var ca = U16(c);
            if (ca is < 1 or > 200) continue;
            var pa = I16(c + PaOffset);
            if (!(pa >= ca && pa <= 200) && !(pa is >= -200 and <= -5)) continue;

            var max = 0;
            var ok = true;
            for (var i = 0; i < PositionCount; i++)
            {
                var r = _d[c + PositionsOffset + i];
                if (r is < 1 or > 20) { ok = false; break; }
                if (r > max) max = r;
            }
            if (!ok || max != 20) continue;

            for (var i = 0; i < AttributeCount; i++)
            {
                var a = _d[c + AttributesOffset + i];
                if (a is < 1 or > 100) { ok = false; break; }
            }
            if (!ok) continue;

            if (I16(c + RepHome) is < 0 or > 10000 || I16(c + RepCurrent) is < 0 or > 10000 || I16(c + RepWorld) is < 0 or > 10000)
                continue;

            blocks.Add(c);
            c += BlockEnd - 1;
        }
        return blocks;
    }

    private Player ReadPlayer(int c, PersonName name)
    {
        var ratings = new int[PositionCount];
        for (var i = 0; i < PositionCount; i++) ratings[i] = _d[c + PositionsOffset + i];

        var pa = I16(c + PaOffset);
        var player = new Player
        {
            Id = c,
            SaveOffset = c,
            BirthDate = name.Born,
            Nationality = "",
            Positions = FmPositions.ToSimplified(ratings),
            PositionRatings = ratings,
            FullLegalName = name.Full,
            FirstName = name.First,
            LastName = name.Last,
            CommonName = name.Common,
            ClubId = (int)U32(c + ClubOffset),
            CurrentAbility = U16(c),
            // Negative PA is FM's "range" potential (-95 = 170-200 and so on). Show its midpoint for now.
            PotentialAbility = pa > 0 ? pa : PotentialFromRange(pa),
            Reputation = I16(c + RepCurrent),
            Value = U32(c + ValueOffset),
            Morale = 50,
        };

        for (var i = 0; i < AttributeCount; i++)
        {
            var attrName = Attributes.SaveFileOrder[i];
            player.AttributeList.Add(new PlayerAttribute(attrName, Attributes.GroupOf(attrName), ToDisplay(_d[c + AttributesOffset + i])));
        }
        return player;
    }

    /// <summary>FM stores attributes as 1-100; the game shows them as 1-20.</summary>
    public static int ToDisplay(int raw) => Math.Clamp((int)Math.Round(raw / 5.0, MidpointRounding.AwayFromZero), 1, 20);

    /// <summary>FM's PA ranges: -10 means 40-90 ... -95 means 170-200 (roughly 1/10 of the value times 20).</summary>
    public static int PotentialFromRange(int negative) => Math.Clamp(-negative * 2, 1, 200);

    // ------------------------------------------------------------------ clubs

    /// <summary>
    /// Club records: [u32 index][u32 uid][u32 uid][00][u32 nation][ffffffff][nation][nation] ... name, short name.
    /// The id players refer to is stored at the end of each record, 18 bytes before the next record.
    /// </summary>
    public List<ClubRecord> FindClubs(int limit, IReadOnlySet<int>? usedIds = null)
    {
        var found = new List<(int Offset, int Index, string Name, string Short)>();
        for (var p = 3; p < limit - 64; p++)
        {
            // Every club record follows "... ff 00 00" and repeats its uid twice.
            if (_d[p - 3] != 0xFF || _d[p - 2] != 0 || _d[p - 1] != 0 || _d[p + 12] != 0) continue;
            var uid = U32(p + 4);
            if (uid == 0 || uid == uint.MaxValue || uid != U32(p + 8)) continue;
            var index = U32(p);
            if (index > 200_000) continue;

            for (var k = p + 29; k < p + 70; k++)
            {
                var len = U32(k);
                if (len is < 3 or > 60) continue;
                if (!TryUtf8(k + 4, (int)len, out var name) || name.Length == 0 || !char.IsUpper(name[0]) || name.Any(char.IsControl))
                    continue;
                var shortLen = U32(k + 4 + (int)len);
                var shortName = shortLen < 40 && TryUtf8(k + 8 + (int)len, (int)shortLen, out var s) ? s : name;
                found.Add((p, (int)index, name, shortName));
                break;
            }
        }

        var result = new List<ClubRecord>(found.Count);
        for (var i = 0; i + 1 < found.Count; i++)
        {
            var next = found[i + 1].Offset;
            // The id sits just before a short trailer at the end of the record (usually 18 bytes
            // before the next record). Some records are followed by other data, so as a fallback
            // take the player-referenced value that occurs most often inside the record.
            int? id = null;
            var start = found[i].Offset;
            var atEnd = (int)U32(next - 18);
            if (usedIds is null)
            {
                id = atEnd;
            }
            else
            {
                // Inside the record the id follows the bytes "ff 00 0N"; it is repeated at the end of
                // the record, 18 bytes before the next one.
                var end = Math.Min(next, start + 4000) - 4;
                for (var q = start + 12; q <= end && id is null; q++)
                {
                    if (_d[q - 3] != 0xFF || _d[q - 2] != 0 || _d[q - 1] is < 1 or > 9) continue;
                    var v = (int)U32(q);
                    if (v > 0 && usedIds.Contains(v)) id = v;
                }
                if (id is null && usedIds.Contains(atEnd) && next - start < 4000) id = atEnd;
            }
            if (id is { } value) result.Add(new ClubRecord(found[i].Offset, value, found[i].Name, found[i].Short));
        }

        // Two records can claim the same id; keep the one whose id fits its neighbours' numbering.
        var byId = result.GroupBy(r => r.Id).Where(g => g.Count() > 1);
        var drop = new HashSet<ClubRecord>();
        foreach (var g in byId)
        {
            var best = g.OrderByDescending(r => NeighbourFit(result, r)).First();
            foreach (var r in g) if (r != best) drop.Add(r);
        }
        return result.Where(r => !drop.Contains(r)).ToList();
    }

    private static int NeighbourFit(List<ClubRecord> all, ClubRecord r)
    {
        var i = all.IndexOf(r);
        var fit = 0;
        if (i > 0 && all[i - 1].Id == r.Id - 1) fit++;
        if (i + 1 < all.Count && all[i + 1].Id == r.Id + 1) fit++;
        return fit;
    }

    private static Club MakeClub(int id, List<Player> squad, Dictionary<int, ClubRecord> records)
    {
        var name = records.TryGetValue(id, out var r) ? r.Name : $"Klubb #{id}";
        var shortName = r?.ShortName ?? name;
        // The save's club reputation isn't decoded yet; estimate it from the best players' reputation.
        var rep = (int)squad.Select(p => p.Reputation).OrderDescending().Take(15).DefaultIfEmpty(0).Average();
        var topValue = squad.Select(p => p.Value).OrderDescending().Take(3).DefaultIfEmpty(0).Average();
        return new Club
        {
            Id = id,
            Name = name,
            ShortName = shortName,
            Country = "",
            League = "",
            Reputation = rep,
            // Budgets aren't decoded yet either. Start from a rough estimate the user can change.
            TransferBudget = (long)(topValue * 0.6),
            MaxWeeklyWage = 0,
        };
    }

    private static Club GuessHumanClub(List<Club> clubs, string? hint)
    {
        // FM names saves "Manager - Club", e.g. "Mattias Nordström - Paris SG".
        if (!string.IsNullOrWhiteSpace(hint))
        {
            var part = hint.Contains(" - ") ? hint[(hint.LastIndexOf(" - ", StringComparison.Ordinal) + 3)..] : hint;
            part = part.Trim();
            var match = clubs.FirstOrDefault(c => c.ShortName.Equals(part, StringComparison.OrdinalIgnoreCase))
                ?? clubs.FirstOrDefault(c => c.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                ?? clubs.FirstOrDefault(c => Abbrev(c.Name).Equals(Abbrev(part), StringComparison.OrdinalIgnoreCase))
                ?? clubs.FirstOrDefault(c => c.Name.StartsWith(part.Split(' ')[0], StringComparison.OrdinalIgnoreCase) && c.Id >= 0);
            if (match is not null) return match;
        }
        return clubs[0];
    }

    private static string Abbrev(string s) =>
        new(s.Split(' ', '-').Where(w => w.Length > 0).Select(w => w.Length <= 3 ? w : w[..1]).SelectMany(w => w).ToArray());

    private DateOnly GuessGameDate(List<int> blocks)
    {
        // Each player block carries the current game date 31 bytes before CA.
        var date = blocks
            .Select(c => ReadDate(c - 31))
            .Where(d => d is { Year: >= 2000 and <= 2100 })
            .GroupBy(d => d!.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
        return date == default ? new DateOnly(2023, 7, 1) : date;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>FM dates: u16 day of year (low 9 bits) and u16 year.</summary>
    public DateOnly? ReadDate(int p)
    {
        if (p < 0 || p + 4 > _d.Length) return null;
        var day = U16(p) & 0x1FF;
        var year = U16(p + 2);
        if (year is < 1 or > 9999 || day < 1 || day > (DateTime.IsLeapYear(year) ? 366 : 365)) return null;
        return new DateOnly(year, 1, 1).AddDays(day - 1);
    }

    private uint U32(int p) => BinaryPrimitives.ReadUInt32LittleEndian(_d.AsSpan(p));
    private int U16(int p) => BinaryPrimitives.ReadUInt16LittleEndian(_d.AsSpan(p));
    private int I16(int p) => BinaryPrimitives.ReadInt16LittleEndian(_d.AsSpan(p));

    private bool TryUtf8(int p, int len, out string text)
    {
        text = "";
        if (p < 0 || p + len > _d.Length) return false;
        try { text = StrictUtf8.GetString(_d, p, len); return true; }
        catch (DecoderFallbackException) { return false; }
    }
}
