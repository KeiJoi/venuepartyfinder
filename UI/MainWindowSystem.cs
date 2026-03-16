using Dalamud.Interface.Windowing;

namespace VenuePartyFinder.UI;

public sealed class MainWindowSystem : IDisposable
{
    private readonly WindowSystem windowSystem = new("VenuePartyFinder");
    private readonly MainWindow mainWindow;

    public MainWindowSystem(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;
        this.windowSystem.AddWindow(mainWindow);
    }

    public void Draw() => this.windowSystem.Draw();

    public void Dispose() => this.windowSystem.RemoveAllWindows();
}
