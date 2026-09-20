namespace SpriteForge.App.Diagnostics;

internal static class StartupLog
{
    private static readonly object Gate = new();

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SpriteForge",
                "logs");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "startup.log");
            var line = $"{DateTimeOffset.Now:O} {message}";
            if (exception is not null)
            {
                line += $"{Environment.NewLine}{exception}";
            }

            lock (Gate)
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // Startup logging must never become a new startup failure.
        }
    }
}
