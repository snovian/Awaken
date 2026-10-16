namespace Awaken;

internal static class Program
{
    private const string MutexName = "Awaken.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Awaken 已在运行。", "Awaken", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        using var context = new AwakenContext();
        Application.Run(context);
        GC.KeepAlive(mutex);
    }
}
