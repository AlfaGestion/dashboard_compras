namespace AlfaCoreShell;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var options = LauncherOptions.Load(args, AppContext.BaseDirectory);
        using var form = new MainForm(options);
        Application.Run(form);
    }
}
