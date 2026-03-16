using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using ECommons;
using VenuePartyFinder.Services;
using VenuePartyFinder.UI;

namespace VenuePartyFinder;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Venue Party Finder";

    private const string CommandName = "/vpf";

    private readonly PluginConfiguration configuration;
    private readonly PartyFinderAutomation automation;
    private readonly MainWindow mainWindow;
    private readonly MainWindowSystem windowSystem;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        ECommonsMain.Init(pluginInterface, this);

        this.configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        this.automation = new PartyFinderAutomation(this.configuration);
        this.mainWindow = new MainWindow(this.configuration, this.automation);
        this.windowSystem = new MainWindowSystem(this.mainWindow);

        if (this.configuration.AutoOpenWindowOnLoad)
        {
            this.mainWindow.IsOpen = true;
        }

        Service.PluginInterface.UiBuilder.Draw += this.DrawUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi += this.OpenUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += this.OpenUi;
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open Venue Party Finder.",
            ShowInHelp = true,
        });
        Service.ChatGui.ChatMessage += this.OnChatMessage;
    }

    public void Dispose()
    {
        Service.ChatGui.ChatMessage -= this.OnChatMessage;
        Service.CommandManager.RemoveHandler(CommandName);
        Service.PluginInterface.UiBuilder.Draw -= this.DrawUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= this.OpenUi;
        this.windowSystem.Dispose();
        this.automation.Dispose();
        ECommonsMain.Dispose();
    }

    private void DrawUi() => this.windowSystem.Draw();

    private void OpenUi() => this.mainWindow.IsOpen = true;

    private void OnCommand(string command, string arguments)
    {
        if (arguments.Equals("refresh", StringComparison.OrdinalIgnoreCase))
        {
            this.automation.QueueRefresh("manual command");
            return;
        }

        if (arguments.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            this.automation.QueueCreateOrUpdate("manual command");
            return;
        }

        this.mainWindow.Toggle();
    }

    private void OnChatMessage(Dalamud.Game.Text.XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        this.automation.HandleChatMessage(type, message.TextValue);
    }
}
