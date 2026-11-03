namespace StatusExpiryCallouts;

public sealed record TrackedStatus(string Job, string Label, uint[] StatusIds);

public static class StatusCatalog
{
    // IDs mirror the status groups shown in the requested tracker list.
    public static readonly IReadOnlyList<TrackedStatus> All =
    [
        new("Astrologian", "Combust/II/III", [0x346, 0x34B, 0x759]),
        new("Bard", "Bard DoTs", [0x4B0, 0x4B1, 0x7C, 0x81]),
        new("Black Mage", "Thunder/High Thunder", [0xA1, 0xA2, 0xA3, 0x4BA, 0xF1F, 0xF20]),
        new("Blue Mage", "Bleeding", [0x6B2]),
        new("Dragoon", "Chaos Thrust/Chaotic Spring", [0x76, 0xA9F]),
        new("Monk", "Demolish", [0xF6]),
        new("Monk", "Twin Snakes", [0xBB9]),
        new("Ninja", "Shadow Fang", [0x1FC]),
        new("Paladin", "Goring/Valor", [0x2D5, 0xAA1]),
        new("Reaper", "Death's Design", [2586]),
        new("Sage", "Dosis", [2614, 2615, 2616, 3897]),
        new("Samurai", "Higanbana", [0x4CC]),
        new("Samurai", "Fugetsu", [0x512]),
        new("Samurai", "Fuka", [0x513]),
        new("Scholar", "Bio/II/Biolysis", [0xB3, 0xBD, 0x767]),
        new("Warrior", "Storm's Eye", [0x5A, 0xA75]),
        new("White Mage", "Aero/II/III/Dia", [0x8F, 0x90, 0x31E, 0x74F]),
        new("Other", "Spirit Dart", [0xD1F]),
    ];
}
