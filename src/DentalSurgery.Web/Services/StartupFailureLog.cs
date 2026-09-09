namespace DentalSurgery.Web.Services;

/// <summary>
/// Writes the exception that stopped the application starting to a file beside
/// it, using nothing but the file system.
/// <para>
/// Deliberately primitive. Everything richer has already failed by the time
/// this runs: the logger is configured from the same configuration that may be
/// the problem, dependency injection may never have been built, and on IIS the
/// console the logger writes to does not exist. What the operator gets instead
/// is a bare 500 from IIS and no explanation anywhere on the machine.
/// </para>
/// <para>
/// The file goes under App_Data, which IIS refuses to serve, because a
/// start-up exception routinely quotes the connection string it failed to open.
/// </para>
/// </summary>
public static class StartupFailureLog
{
    private const string Folder = "App_Data/logs";
    private const string FileName = "startup-error.log";

    /// <summary>
    /// Appends a failure, and never throws: an error while recording an error
    /// must not replace the original one.
    /// </summary>
    public static void Write(string contentRootPath, Exception exception)
    {
        try
        {
            var directory = Path.Combine(contentRootPath, Folder);
            Directory.CreateDirectory(directory);

            var report =
                $"""
                 ================================================================
                 {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                 The application failed to start.
                 ================================================================
                 {exception}

                 """;

            // Append rather than replace: a restart loop is itself evidence,
            // and the first failure is usually the informative one.
            File.AppendAllText(Path.Combine(directory, FileName), report);
        }
        catch
        {
            // Nowhere left to report it, and the real exception is about to be
            // rethrown regardless.
        }
    }
}
