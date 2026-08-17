using monitor_controller.Configuration;
using monitor_controller.Display;
using monitor_controller.Scheduling;
using monitor_controller.UI;

namespace monitor_controller;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var displayController = new DdcController();
        using var scheduler = new DisplayScheduler(displayController);
        var configService = new ConfigService();

        Application.Run(new TrayApplicationContext(displayController, scheduler, configService));
    }
}