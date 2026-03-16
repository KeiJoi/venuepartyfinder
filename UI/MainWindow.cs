using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VenuePartyFinder.Models;
using VenuePartyFinder.Services;

namespace VenuePartyFinder.UI;

public sealed class MainWindow : Window
{
    private readonly record struct GlyphPalette(string Name, string[] Glyphs, int Columns);

    private static readonly GlyphPalette[] BlockLetterPalettes =
    [
        new("FFXIV Block A-Z", ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""], 7),
        new("Fullwidth Upper", CreateCodepointRange(0xFF21, 26), 7),
        new("Fullwidth Lower", CreateCodepointRange(0xFF41, 26), 7),
        new("Fullwidth Numbers", CreateCodepointRange(0xFF10, 10), 10),
        new("Circled Numbers", CreateCircledNumbers(), 10),
        new("Shapes", ["■", "□", "▣", "▢", "◆", "◇", "◈", "▲", "△", "▼", "▽", "●", "○", "◎", "◉", "★", "☆", "✦", "✧", "✩"], 5),
        new("Hearts And Notes", ["♡", "♥", "❥", "❤", "♪", "♫", "♬", "♩", "♭", "♯", "☾", "☽", "✿", "❀", "✾", "✼", "❁", "☀", "☼", "☁"], 5),
        new("Brackets And Marks", ["【", "】", "「", "」", "『", "』", "〈", "〉", "《", "》", "〔", "〕", "〖", "〗", "〘", "〙", "・", "•", "｜", "┃", "→", "←", "↑", "↓"], 6),
    ];

    private readonly PluginConfiguration configuration;
    private readonly PartyFinderAutomation automation;
    private string dutySearch = string.Empty;
    private string blockLetterBuilder = string.Empty;
    private bool openBlockLetterGenerator;

    public MainWindow(PluginConfiguration configuration, PartyFinderAutomation automation)
        : base("Venue Party Finder###VenuePartyFinder")
    {
        this.configuration = configuration;
        this.automation = automation;
        this.Size = new Vector2(980, 820);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var preset = this.configuration.Preset;

        ImGui.TextWrapped("This window mirrors the native recruitment criteria as closely as API level 14 exposes it, then applies those values through the in-game Party Finder UI.");
        ImGui.Spacing();

        if (ImGui.Button(this.automation.HasOwnListing ? "Edit / Apply Changes" : "Recruit Members"))
        {
            this.automation.QueueCreateOrUpdate("window button");
        }

        ImGui.SameLine();
        if (ImGui.Button("Refresh Active Listing"))
        {
            this.automation.QueueRefresh("window button");
        }

        ImGui.SameLine();
        if (ImGui.Button("Abort Queue"))
        {
            this.automation.Abort();
        }

        ImGui.Separator();
        ImGui.TextWrapped($"Status: {this.automation.Status}");
        ImGui.TextWrapped($"Own listing detected: {(this.automation.HasOwnListing ? "yes" : "no")}");

        if (ImGui.CollapsingHeader("Automation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var autoRefresh = this.configuration.AutoRefreshEnabled;
            if (ImGui.Checkbox("Auto-refresh on 5 minute warning", ref autoRefresh))
            {
                this.configuration.AutoRefreshEnabled = autoRefresh;
                this.configuration.Save();
            }

            var autoOpen = this.configuration.AutoOpenWindowOnLoad;
            if (ImGui.Checkbox("Open this window on plugin load", ref autoOpen))
            {
                this.configuration.AutoOpenWindowOnLoad = autoOpen;
                this.configuration.Save();
            }

            var warningOverride = this.configuration.WarningMessageOverride;
            if (ImGui.InputText("Warning text override", ref warningOverride, 256))
            {
                this.configuration.WarningMessageOverride = warningOverride;
                this.configuration.Save();
            }
        }

        ImGui.Separator();

        if (ImGui.BeginTable("criteria_layout", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextColumn();
            this.DrawLeftColumn(preset);

            ImGui.TableNextColumn();
            this.DrawRightColumn(preset);

            ImGui.EndTable();
        }

        ImGui.Separator();
        this.DrawRolesSection(preset);
    }

    private void DrawLeftColumn(PartyFinderPreset preset)
    {
        ImGui.TextUnformatted("Details");
        ImGui.Separator();

        this.DrawCategorySelector(preset);
        this.DrawDutySelector(preset);
        this.DrawObjectiveSelector(preset);

        var beginner = preset.BeginnerFriendly;
        if (ImGui.Checkbox("Beginner friendly", ref beginner))
        {
            preset.BeginnerFriendly = beginner;
            this.configuration.Save();
        }

        var comment = preset.Comment;
        if (ImGui.InputTextMultiline("Comment", ref comment, 512, new Vector2(-1, 120)))
        {
            preset.Comment = comment;
            this.configuration.Save();
        }

        if (ImGui.Button("Block Letter Generator"))
        {
            this.openBlockLetterGenerator = true;
            ImGui.OpenPopup("Block Letter Generator");
        }

        ImGui.TextDisabled($"{System.Text.Encoding.UTF8.GetByteCount(preset.Comment)}/192 bytes");
        this.DrawBlockLetterGenerator(preset);
    }

    private void DrawRightColumn(PartyFinderPreset preset)
    {
        ImGui.TextUnformatted("Search Area");
        ImGui.Separator();

        var worldOnly = preset.LimitRecruitingToWorld;
        if (ImGui.Checkbox("Limit recruiting to world server", ref worldOnly))
        {
            preset.LimitRecruitingToWorld = worldOnly;
            this.configuration.Save();
        }

        var privateParty = preset.PrivateParty;
        if (ImGui.Checkbox("Form a private party", ref privateParty))
        {
            preset.PrivateParty = privateParty;
            if (!privateParty)
            {
                preset.Password = 0;
            }

            this.configuration.Save();
        }

        ImGui.BeginDisabled(!preset.PrivateParty);
        var password = (int)preset.Password;
        if (ImGui.InputInt("Party password", ref password))
        {
            preset.Password = (ushort)Math.Clamp(password, 0, 9999);
            this.configuration.Save();
        }
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.TextUnformatted("Conditions");
        ImGui.Separator();

        this.DrawCompletionStatusSection(preset);
        this.DrawAverageItemLevelSection(preset);

        ImGui.Spacing();
        ImGui.TextUnformatted("Duty Finder Settings");
        ImGui.Separator();

        preset.DutyFinderSettings = this.DrawFlagCheckbox("Unrestricted party", AgentLookingForGroup.DutyFinderSetting.UnrestrictedParty, preset.DutyFinderSettings);
        preset.DutyFinderSettings = this.DrawFlagCheckbox("Minimum item level", AgentLookingForGroup.DutyFinderSetting.MinimumIL, preset.DutyFinderSettings);
        preset.DutyFinderSettings = this.DrawFlagCheckbox("Silence echo", AgentLookingForGroup.DutyFinderSetting.SilenceEcho, preset.DutyFinderSettings);

        ImGui.Spacing();
        ImGui.TextUnformatted("Loot Rules");
        ImGui.Separator();
        this.DrawLootRuleSelector(preset);

        ImGui.Spacing();
        ImGui.TextUnformatted("Language");
        ImGui.Separator();
        this.DrawLanguageSelector(preset);
    }

    private void DrawRolesSection(PartyFinderPreset preset)
    {
        if (!ImGui.CollapsingHeader("Roles", ImGuiTreeNodeFlags.DefaultOpen))
        {
            return;
        }

        var mainSlots = (int)preset.NumberOfSlotsInMainParty;
        if (ImGui.SliderInt("Slots in main party", ref mainSlots, 1, 8))
        {
            preset.NumberOfSlotsInMainParty = (byte)mainSlots;
            this.configuration.Save();
        }

        var groups = (int)preset.NumberOfGroups;
        if (ImGui.SliderInt("Number of groups", ref groups, 1, 6))
        {
            preset.NumberOfGroups = (byte)groups;
            this.configuration.Save();
        }

        var onePerJob = preset.OnePlayerPerJob;
        if (ImGui.Checkbox("One player per job", ref onePerJob))
        {
            preset.OnePlayerPerJob = onePerJob;
            this.configuration.Save();
        }

        ImGui.TextWrapped("The native screen stores role restrictions as per-slot job masks. Expand a slot below to match the role icons you want.");
        ImGui.TextWrapped($"Effective slots: {preset.EffectiveSlotCount}");

        if (ImGui.Button("Set all slots to any job"))
        {
            for (var i = 0; i < preset.SlotJobMasks.Length; i++)
            {
                preset.SetSlotMask(i, JobCatalog.AllJobsMask);
            }

            this.configuration.Save();
        }

        for (var i = 0; i < preset.EffectiveSlotCount; i++)
        {
            var mask = preset.GetSlotMask(i);
            var summary = this.automation.DescribeJobMask(mask);
            if (!ImGui.TreeNode($"Slot {i + 1}: {summary}###slot_{i}"))
            {
                continue;
            }

            this.DrawSlotQuickMasks(preset, i);
            ImGui.Separator();

            foreach (var job in JobCatalog.Jobs)
            {
                var enabled = (mask & (ulong)job.Flag) != 0;
                if (ImGui.Checkbox($"{job.Abbreviation} - {job.Name}###slot_{i}_{job.Abbreviation}", ref enabled))
                {
                    mask = enabled ? mask | (ulong)job.Flag : mask & ~(ulong)job.Flag;
                    preset.SetSlotMask(i, mask == 0 ? JobCatalog.AllJobsMask : mask);
                    this.configuration.Save();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawObjectiveSelector(PartyFinderPreset preset)
    {
        var preview = this.GetObjectiveName(preset.Objective);
        if (!ImGui.BeginCombo("Objective", preview))
        {
            return;
        }

        foreach (var objective in new[]
                 {
                     AgentLookingForGroup.Objective.None,
                     AgentLookingForGroup.Objective.DutyCompletion,
                     AgentLookingForGroup.Objective.Practice,
                     AgentLookingForGroup.Objective.Loot,
                 })
        {
            var selected = preset.Objective == objective;
            if (ImGui.Selectable(this.GetObjectiveName(objective), selected))
            {
                preset.Objective = objective;
                this.configuration.Save();
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private void DrawCompletionStatusSection(PartyFinderPreset preset)
    {
        var enabled = preset.CompletionStatus != AgentLookingForGroup.CompletionStatus.None;
        if (ImGui.Checkbox("Completion status", ref enabled))
        {
            preset.CompletionStatus = enabled
                ? AgentLookingForGroup.CompletionStatus.DutyComplete
                : AgentLookingForGroup.CompletionStatus.None;
            this.configuration.Save();
        }

        ImGui.BeginDisabled(!enabled);
        var preview = this.GetCompletionStatusName(preset.CompletionStatus == AgentLookingForGroup.CompletionStatus.None
            ? AgentLookingForGroup.CompletionStatus.DutyComplete
            : preset.CompletionStatus);
        if (ImGui.BeginCombo("Completion status value", preview))
        {
            foreach (var status in new[]
                     {
                         AgentLookingForGroup.CompletionStatus.DutyComplete,
                         AgentLookingForGroup.CompletionStatus.DutyIncomplete,
                         AgentLookingForGroup.CompletionStatus.DutyCompleteWeeklyUnclaimed,
                     })
            {
                var selected = preset.CompletionStatus == status;
                if (ImGui.Selectable(this.GetCompletionStatusName(status), selected))
                {
                    preset.CompletionStatus = status;
                    this.configuration.Save();
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
        ImGui.EndDisabled();
    }

    private void DrawAverageItemLevelSection(PartyFinderPreset preset)
    {
        var enabled = preset.AverageItemLevelEnabled;
        if (ImGui.Checkbox("Avg. item level", ref enabled))
        {
            preset.AverageItemLevelEnabled = enabled;
            if (enabled && preset.AverageItemLevel == 0)
            {
                preset.AverageItemLevel = 1;
            }

            this.configuration.Save();
        }

        ImGui.BeginDisabled(!preset.AverageItemLevelEnabled);
        var avgItemLevel = Math.Clamp((int)preset.AverageItemLevel, 1, 999);
        if (ImGui.InputInt("Avg. item level value", ref avgItemLevel))
        {
            preset.AverageItemLevel = (ushort)Math.Clamp(avgItemLevel, 1, 999);
            this.configuration.Save();
        }
        ImGui.EndDisabled();
    }

    private void DrawLanguageSelector(PartyFinderPreset preset)
    {
        preset.Languages = this.DrawFlagCheckbox("JP", AgentLookingForGroup.Language.Japanese, preset.Languages);
        ImGui.SameLine();
        preset.Languages = this.DrawFlagCheckbox("EN", AgentLookingForGroup.Language.English, preset.Languages);
        ImGui.SameLine();
        preset.Languages = this.DrawFlagCheckbox("DE", AgentLookingForGroup.Language.German, preset.Languages);
        ImGui.SameLine();
        preset.Languages = this.DrawFlagCheckbox("FR", AgentLookingForGroup.Language.French, preset.Languages);
    }

    private void DrawSlotQuickMasks(PartyFinderPreset preset, int index)
    {
        if (ImGui.SmallButton($"All###all_{index}"))
        {
            preset.SetSlotMask(index, JobCatalog.AllJobsMask);
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"Tank###tank_{index}"))
        {
            preset.SetSlotMask(index, JobCatalog.TankMask);
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"Heal###heal_{index}"))
        {
            preset.SetSlotMask(index, JobCatalog.HealerMask);
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"Melee###melee_{index}"))
        {
            preset.SetSlotMask(index, JobCatalog.MeleeMask);
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"Phys Ranged###pranged_{index}"))
        {
            preset.SetSlotMask(index, JobCatalog.PhysicalRangedMask);
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"Caster###caster_{index}"))
        {
            preset.SetSlotMask(index, JobCatalog.MagicalRangedMask);
            this.configuration.Save();
        }
    }

    private void DrawCategorySelector(PartyFinderPreset preset)
    {
        var preview = this.automation.GetCategoryName(preset.Category);
        if (!ImGui.BeginCombo("Category", preview))
        {
            return;
        }

        foreach (var category in this.automation.Categories)
        {
            var selected = category == preset.Category;
            if (ImGui.Selectable(this.automation.GetCategoryName(category), selected))
            {
                preset.Category = category;
                this.configuration.Save();
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private void DrawDutySelector(PartyFinderPreset preset)
    {
        ImGui.InputText("Duty filter", ref this.dutySearch, 128);

        var selectedDuty = this.automation.GetDutyName(preset.DutyId);
        if (!ImGui.BeginCombo("Duty", selectedDuty))
        {
            return;
        }

        foreach (var duty in this.automation.GetFilteredDuties(this.dutySearch))
        {
            var selected = duty.Id == preset.DutyId;
            if (ImGui.Selectable($"{duty.Name}##{duty.Id}", selected))
            {
                preset.DutyId = duty.Id;
                this.configuration.Save();
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private void DrawLootRuleSelector(PartyFinderPreset preset)
    {
        var preview = this.GetLootRuleName(preset.LootRule);
        if (!ImGui.BeginCombo("Loot rule", preview))
        {
            return;
        }

        foreach (var rule in new[]
                 {
                     AgentLookingForGroup.LootRule.Normal,
                     AgentLookingForGroup.LootRule.GreedOnly,
                     AgentLookingForGroup.LootRule.Lootmaster,
                 })
        {
            var selected = preset.LootRule == rule;
            if (ImGui.Selectable(this.GetLootRuleName(rule), selected))
            {
                preset.LootRule = rule;
                this.configuration.Save();
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private string GetObjectiveName(AgentLookingForGroup.Objective objective)
        => objective switch
        {
            AgentLookingForGroup.Objective.None => "None",
            AgentLookingForGroup.Objective.DutyCompletion => "Duty completion",
            AgentLookingForGroup.Objective.Practice => "Practice",
            AgentLookingForGroup.Objective.Loot => "Loot",
            _ => objective.ToString(),
        };

    private string GetCompletionStatusName(AgentLookingForGroup.CompletionStatus status)
        => status switch
        {
            AgentLookingForGroup.CompletionStatus.None => "None",
            AgentLookingForGroup.CompletionStatus.DutyComplete => "Duty complete",
            AgentLookingForGroup.CompletionStatus.DutyIncomplete => "Duty incomplete",
            AgentLookingForGroup.CompletionStatus.DutyCompleteWeeklyUnclaimed => "Weekly reward unclaimed",
            _ => status.ToString(),
        };

    private string GetLootRuleName(AgentLookingForGroup.LootRule rule)
        => rule switch
        {
            AgentLookingForGroup.LootRule.Normal => "Normal",
            AgentLookingForGroup.LootRule.GreedOnly => "Greed only",
            AgentLookingForGroup.LootRule.Lootmaster => "Lootmaster",
            _ => rule.ToString(),
        };

    private AgentLookingForGroup.DutyFinderSetting DrawFlagCheckbox(string label, AgentLookingForGroup.DutyFinderSetting flag, AgentLookingForGroup.DutyFinderSetting value)
    {
        var enabled = (value & flag) == flag;
        if (ImGui.Checkbox(label, ref enabled))
        {
            value = enabled ? value | flag : value & ~flag;
            this.configuration.Save();
        }

        return value;
    }

    private AgentLookingForGroup.Language DrawFlagCheckbox(string label, AgentLookingForGroup.Language flag, AgentLookingForGroup.Language value)
    {
        var enabled = (value & flag) == flag;
        if (ImGui.Checkbox(label, ref enabled))
        {
            value = enabled ? value | flag : value & ~flag;
            this.configuration.Save();
        }

        return value;
    }

    private void DrawBlockLetterGenerator(PartyFinderPreset preset)
    {
        if (this.openBlockLetterGenerator)
        {
            ImGui.SetNextWindowSize(new Vector2(860, 620), ImGuiCond.Appearing);
        }

        if (!ImGui.BeginPopupModal("Block Letter Generator", ref this.openBlockLetterGenerator, ImGuiWindowFlags.NoResize))
        {
            return;
        }

        ImGui.TextUnformatted("Build a string from block letters, numbers, and symbols, then copy or append it to your PF comment.");

        if (ImGui.InputTextMultiline("Generator Text", ref this.blockLetterBuilder, 2048, new Vector2(-1, 90)))
        {
        }

        if (ImGui.Button("Copy To Clipboard"))
        {
            ImGui.SetClipboardText(this.blockLetterBuilder);
        }

        ImGui.SameLine();
        if (ImGui.Button("Append To Comment"))
        {
            preset.Comment += this.blockLetterBuilder;
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Replace Comment"))
        {
            preset.Comment = this.blockLetterBuilder;
            this.configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Backspace") && this.blockLetterBuilder.Length > 0)
        {
            this.blockLetterBuilder = this.blockLetterBuilder[..^1];
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            this.blockLetterBuilder = string.Empty;
        }

        ImGui.Separator();

        if (ImGui.BeginTabBar("block_letter_tabs"))
        {
            foreach (var palette in BlockLetterPalettes)
            {
                if (!ImGui.BeginTabItem(palette.Name))
                {
                    continue;
                }

                this.DrawGlyphPalette(palette);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        if (ImGui.Button("Close"))
        {
            this.openBlockLetterGenerator = false;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawGlyphPalette(GlyphPalette palette)
    {
        for (var i = 0; i < palette.Glyphs.Length; i++)
        {
            if (i > 0 && i % palette.Columns != 0)
            {
                ImGui.SameLine();
            }

            if (ImGui.Button($"{palette.Glyphs[i]}##{palette.Name}_{i}", new Vector2(44, 32)))
            {
                this.blockLetterBuilder += palette.Glyphs[i];
            }
        }
    }

    private static string[] CreateCodepointRange(int start, int count)
    {
        var glyphs = new string[count];
        for (var i = 0; i < count; i++)
        {
            glyphs[i] = char.ConvertFromUtf32(start + i);
        }

        return glyphs;
    }

    private static string[] CreateCircledNumbers()
    {
        var glyphs = new string[10];
        glyphs[0] = char.ConvertFromUtf32(0x24EA);
        for (var i = 1; i <= 9; i++)
        {
            glyphs[i] = char.ConvertFromUtf32(0x2460 + i - 1);
        }

        return glyphs;
    }
}

