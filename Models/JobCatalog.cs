using Dalamud.Game.Gui.PartyFinder.Types;

namespace VenuePartyFinder.Models;

public sealed record JobOption(string Abbreviation, string Name, JobFlags Flag, string Role);

public static class JobCatalog
{
    public static readonly IReadOnlyList<JobOption> Jobs =
    [
        new("PLD", "Paladin", JobFlags.Paladin, "Tank"),
        new("WAR", "Warrior", JobFlags.Warrior, "Tank"),
        new("DRK", "Dark Knight", JobFlags.DarkKnight, "Tank"),
        new("GNB", "Gunbreaker", JobFlags.Gunbreaker, "Tank"),
        new("WHM", "White Mage", JobFlags.WhiteMage, "Healer"),
        new("SCH", "Scholar", JobFlags.Scholar, "Healer"),
        new("AST", "Astrologian", JobFlags.Astrologian, "Healer"),
        new("SGE", "Sage", JobFlags.Sage, "Healer"),
        new("MNK", "Monk", JobFlags.Monk, "Melee"),
        new("DRG", "Dragoon", JobFlags.Dragoon, "Melee"),
        new("NIN", "Ninja", JobFlags.Ninja, "Melee"),
        new("SAM", "Samurai", JobFlags.Samurai, "Melee"),
        new("RPR", "Reaper", JobFlags.Reaper, "Melee"),
        new("VPR", "Viper", JobFlags.Viper, "Melee"),
        new("BRD", "Bard", JobFlags.Bard, "Physical Ranged"),
        new("MCH", "Machinist", JobFlags.Machinist, "Physical Ranged"),
        new("DNC", "Dancer", JobFlags.Dancer, "Physical Ranged"),
        new("BLM", "Black Mage", JobFlags.BlackMage, "Magical Ranged"),
        new("SMN", "Summoner", JobFlags.Summoner, "Magical Ranged"),
        new("RDM", "Red Mage", JobFlags.RedMage, "Magical Ranged"),
        new("PCT", "Pictomancer", JobFlags.Pictomancer, "Magical Ranged"),
        new("BLU", "Blue Mage", JobFlags.BlueMage, "Limited"),
    ];

    public static readonly ulong AllJobsMask = Jobs.Aggregate(0UL, static (mask, job) => mask | (ulong)job.Flag);

    public static readonly ulong TankMask = BuildRoleMask("Tank");
    public static readonly ulong HealerMask = BuildRoleMask("Healer");
    public static readonly ulong MeleeMask = BuildRoleMask("Melee");
    public static readonly ulong PhysicalRangedMask = BuildRoleMask("Physical Ranged");
    public static readonly ulong MagicalRangedMask = BuildRoleMask("Magical Ranged");

    private static ulong BuildRoleMask(string role)
        => Jobs.Where(job => job.Role == role).Aggregate(0UL, static (mask, job) => mask | (ulong)job.Flag);
}
