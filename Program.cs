namespace AggrWin;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Avoid starting a second copy if Windows login startup and a manual launch overlap.
        using var mutex = new Mutex(true, @"Local\AggrWin.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        Application.Run(new MainForm());
    }
}
