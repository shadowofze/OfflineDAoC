namespace OfflineDaoc.Launcher;

internal static class BotCharacterGenerator
{
    internal sealed record Identity(string Name, int Realm, int Gender, int ClassId, string ClassName, int RaceId, string RaceName);
    private sealed record Choice(int ClassId, string ClassName, params (int Id, string Name)[] Races);

    private static readonly IReadOnlyDictionary<int, Choice[]> Choices = new Dictionary<int, Choice[]>
    {
        [1] =
        [
            new(1, "Paladin", (2, "Avalonian"), (1, "Briton"), (3, "Highlander"), (4, "Saracen")),
            new(2, "Armsman", (2, "Avalonian"), (1, "Briton"), (3, "Highlander"), (13, "Inconnu"), (4, "Saracen")),
            new(3, "Scout", (1, "Briton"), (3, "Highlander"), (13, "Inconnu"), (4, "Saracen")),
            new(4, "Minstrel", (1, "Briton"), (3, "Highlander"), (4, "Saracen")),
            new(5, "Theurgist", (2, "Avalonian"), (1, "Briton")),
            new(6, "Cleric", (2, "Avalonian"), (1, "Briton"), (3, "Highlander")),
            new(7, "Wizard", (2, "Avalonian"), (1, "Briton")),
            new(8, "Sorcerer", (2, "Avalonian"), (1, "Briton"), (13, "Inconnu"), (4, "Saracen")),
            new(9, "Infiltrator", (1, "Briton"), (13, "Inconnu"), (4, "Saracen")),
            new(10, "Friar", (1, "Briton")),
            new(11, "Mercenary", (2, "Avalonian"), (1, "Briton"), (3, "Highlander"), (13, "Inconnu"), (4, "Saracen")),
            new(12, "Necromancer", (1, "Briton"), (13, "Inconnu"), (4, "Saracen")),
            new(13, "Cabalist", (2, "Avalonian"), (1, "Briton"), (13, "Inconnu"), (4, "Saracen")),
            new(19, "Reaver", (1, "Briton"), (13, "Inconnu"), (4, "Saracen")),
        ],
        [2] =
        [
            new(21, "Thane", (7, "Dwarf"), (5, "Norseman"), (6, "Troll")),
            new(22, "Warrior", (7, "Dwarf"), (8, "Kobold"), (5, "Norseman"), (6, "Troll"), (14, "Valkyn")),
            new(23, "Shadowblade", (8, "Kobold"), (5, "Norseman"), (14, "Valkyn")),
            new(24, "Skald", (7, "Dwarf"), (8, "Kobold"), (5, "Norseman"), (6, "Troll")),
            new(25, "Hunter", (7, "Dwarf"), (8, "Kobold"), (5, "Norseman"), (14, "Valkyn")),
            new(26, "Healer", (7, "Dwarf"), (5, "Norseman")),
            new(27, "Spiritmaster", (8, "Kobold"), (5, "Norseman")),
            new(28, "Shaman", (8, "Kobold"), (6, "Troll")),
            new(29, "Runemaster", (7, "Dwarf"), (8, "Kobold"), (5, "Norseman")),
            new(30, "Bonedancer", (8, "Kobold"), (6, "Troll"), (14, "Valkyn")),
            new(31, "Berserker", (7, "Dwarf"), (5, "Norseman"), (6, "Troll"), (14, "Valkyn")),
            new(32, "Savage", (7, "Dwarf"), (8, "Kobold"), (5, "Norseman"), (6, "Troll"), (14, "Valkyn")),
        ],
        [3] =
        [
            new(40, "Eldritch", (11, "Elf"), (12, "Lurikeen")),
            new(41, "Enchanter", (11, "Elf"), (12, "Lurikeen")),
            new(42, "Mentalist", (9, "Celt"), (11, "Elf"), (12, "Lurikeen")),
            new(43, "Blademaster", (9, "Celt"), (11, "Elf"), (10, "Firbolg")),
            new(44, "Hero", (9, "Celt"), (10, "Firbolg"), (12, "Lurikeen"), (15, "Sylvan")),
            new(45, "Champion", (9, "Celt"), (11, "Elf"), (12, "Lurikeen")),
            new(46, "Warden", (9, "Celt"), (10, "Firbolg"), (15, "Sylvan")),
            new(47, "Druid", (9, "Celt"), (10, "Firbolg"), (15, "Sylvan")),
            new(48, "Bard", (9, "Celt"), (10, "Firbolg")),
            new(49, "Nightshade", (11, "Elf"), (12, "Lurikeen")),
            new(50, "Ranger", (9, "Celt"), (11, "Elf"), (12, "Lurikeen")),
            new(55, "Animist", (9, "Celt"), (10, "Firbolg"), (15, "Sylvan")),
            new(56, "Valewalker", (9, "Celt"), (10, "Firbolg"), (15, "Sylvan")),
        ],
    };

    private static readonly IReadOnlyDictionary<(int Realm, int Gender), (string[] Starts, string[] Middles, string[] Ends)> Names =
        new Dictionary<(int, int), (string[], string[], string[])>
        {
            [(1, 1)] = (["Ald", "Ber", "Ced", "Ed", "Gar", "God", "Har", "Leof", "Os", "Ran", "Ren", "Thed", "Wil"], ["", "a", "e", "en", "er", "i", "o"], ["bert", "ric", "win", "mund", "ward", "frey", "ren", "well", "red", "helm"]),
            [(1, 2)] = (["Ada", "Ael", "Ela", "Eve", "Gis", "Isa", "Lina", "Mara", "Ros", "Ser", "Thea", "Ys"], ["", "a", "e", "el", "en", "i", "o"], ["bel", "beth", "lyn", "wen", "ette", "ine", "ora", "elle", "ith", "anne"]),
            [(2, 1)] = (["Arn", "Bjorn", "Dag", "Egil", "Eirik", "Finn", "Gorm", "Hald", "Ivar", "Knut", "Rag", "Sig", "Tor", "Ulf"], ["", "a", "e", "ge", "i", "un"], ["ar", "bjorn", "brand", "grim", "mund", "rik", "sten", "ulf", "vald", "var"]),
            [(2, 2)] = (["Astr", "Bry", "Dag", "Eir", "Fre", "Gud", "Hild", "Ing", "Kara", "Liv", "Rag", "Sig", "Siv", "Yr"], ["", "a", "e", "i", "un"], ["a", "dis", "frid", "gerd", "hild", "run", "rid", "veig", "borg", "ny"]),
            [(3, 1)] = (["Aed", "Bran", "Cael", "Ciar", "Con", "Dair", "Eog", "Ferg", "Lorc", "Nial", "Ois", "Rian", "Tad"], ["", "a", "e", "el", "in", "o"], ["an", "dan", "gan", "gus", "lan", "nan", "ren", "ric", "ron", "van"]),
            [(3, 2)] = (["Aine", "Bria", "Cao", "Deir", "Eil", "Fia", "Mae", "Muir", "Nia", "Orla", "Rois", "Sao", "Una"], ["", "a", "e", "el", "in", "ri"], ["a", "bhe", "dra", "la", "lin", "na", "ra", "rin", "se", "wen"]),
        };

    private static readonly IReadOnlyDictionary<int, string[]> NameBridges = new Dictionary<int, string[]>
    {
        [1] = ["", "al", "en", "is", "or"],
        [2] = ["", "ar", "en", "ild", "un"],
        [3] = ["", "ae", "el", "in", "or"],
    };

    public static Identity Generate(int realm, ISet<string> reservedNames)
    {
        int gender = Random.Shared.Next(1, 3);
        Choice choice = Choices[realm][Random.Shared.Next(Choices[realm].Length)];
        var race = choice.Races[Random.Shared.Next(choice.Races.Length)];
        var parts = Names[(realm, gender)];
        string[] bridges = NameBridges[realm];
        for (int attempt = 0; attempt < 1024; attempt++)
        {
            string name = Normalize(parts.Starts[Random.Shared.Next(parts.Starts.Length)] +
                                    parts.Middles[Random.Shared.Next(parts.Middles.Length)] +
                                    bridges[Random.Shared.Next(bridges.Length)] +
                                    parts.Ends[Random.Shared.Next(parts.Ends.Length)]);
            if (reservedNames.Add(name))
                return new(name, realm, gender, choice.ClassId, choice.ClassName, race.Id, race.Name);
        }

        foreach (string start in parts.Starts)
        foreach (string middle in parts.Middles)
        foreach (string bridge in bridges)
        foreach (string end in parts.Ends)
        {
            string name = Normalize(start + middle + bridge + end);
            if (reservedNames.Add(name))
                return new(name, realm, gender, choice.ClassId, choice.ClassName, race.Id, race.Name);
        }
        throw new InvalidOperationException("The name generator could not find another unique realm-appropriate name.");
    }

    private static string Normalize(string raw)
    {
        string collapsed = raw.Replace("aa", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ee", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("ii", "i", StringComparison.OrdinalIgnoreCase);
        return char.ToUpperInvariant(collapsed[0]) + collapsed[1..].ToLowerInvariant();
    }
}
