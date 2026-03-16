using Dalamud.Game.Gui.PartyFinder.Types;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace VenuePartyFinder.Models;

public sealed class PartyFinderPreset
{
    public AgentLookingForGroup.DutyCategory Category { get; set; } = AgentLookingForGroup.DutyCategory.None;

    public ushort DutyId { get; set; }

    public AgentLookingForGroup.Objective Objective { get; set; } = AgentLookingForGroup.Objective.None;

    public bool BeginnerFriendly { get; set; }

    public AgentLookingForGroup.CompletionStatus CompletionStatus { get; set; } = AgentLookingForGroup.CompletionStatus.None;

    public bool AverageItemLevelEnabled { get; set; }

    public ushort AverageItemLevel { get; set; } = 1;

    public AgentLookingForGroup.DutyFinderSetting DutyFinderSettings { get; set; } = AgentLookingForGroup.DutyFinderSetting.None;

    public AgentLookingForGroup.LootRule LootRule { get; set; } = AgentLookingForGroup.LootRule.Normal;

    public bool PrivateParty { get; set; }

    public ushort Password { get; set; }

    public AgentLookingForGroup.Language Languages { get; set; } = AgentLookingForGroup.Language.English;

    public byte NumberOfSlotsInMainParty { get; set; } = 8;

    public bool LimitRecruitingToWorld { get; set; }

    public bool OnePlayerPerJob { get; set; }

    public byte NumberOfGroups { get; set; } = 1;

    public string Comment { get; set; } = "Venue open. Please read the description before joining.";

    public ulong[] SlotJobMasks { get; set; } = CreateDefaultSlotMasks();

    public int EffectiveSlotCount => Math.Clamp((int)this.NumberOfSlotsInMainParty * Math.Max(1, (int)this.NumberOfGroups), 1, 48);

    public static PartyFinderPreset CreateDefault()
    {
        return new PartyFinderPreset
        {
            Objective = AgentLookingForGroup.Objective.Practice,
            Languages = AgentLookingForGroup.Language.English,
            AverageItemLevel = 1,
            SlotJobMasks = CreateDefaultSlotMasks(),
        };
    }

    public ulong GetSlotMask(int index)
    {
        if (index < 0 || index >= this.SlotJobMasks.Length)
        {
            return JobCatalog.AllJobsMask;
        }

        return this.SlotJobMasks[index] == 0 ? JobCatalog.AllJobsMask : this.SlotJobMasks[index];
    }

    public void SetSlotMask(int index, ulong mask)
    {
        if (index < 0 || index >= this.SlotJobMasks.Length)
        {
            return;
        }

        this.SlotJobMasks[index] = mask == 0 ? JobCatalog.AllJobsMask : mask;
    }

    private static ulong[] CreateDefaultSlotMasks()
    {
        var slots = new ulong[48];
        for (var i = 0; i < slots.Length; i++)
        {
            slots[i] = JobCatalog.AllJobsMask;
        }

        return slots;
    }
}
