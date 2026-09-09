using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Xunit;

namespace DentalSurgery.IntegrationTests;

/// <summary>
/// Starts the real application as its own process and drives it over HTTP.
/// <para>
/// The obvious choice would be <c>WebApplicationFactory</c>, and it was tried
/// first. It does not fit this application: the host resolver it depends on
/// stops <c>Program</c> at the point the host is built, which is before the
/// database initialiser between <c>Build()</c> and <c>Run()</c> has run. The
/// suite would then be exercising a host that had never seeded its roles — and
/// the role-to-permission map is precisely what these tests are about.
/// </para>
/// <para>
/// Running the published entry point instead means the test drives the same
/// start-up sequence a deployment does, initialiser included, through a real
/// socket. It is slower to start and worth it: a fault in start-up order shows
/// up here rather than in production.
/// </para>
/// </summary>
public class DentalAppFixture : IAsyncLifetime
{
    private Process? _process;
    private readonly List<string> _output = [];

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"dental-it-{Guid.NewGuid():N}.db");

    private readonly string _documentRoot =
        Path.Combine(Path.GetTempPath(), $"dental-it-docs-{Guid.NewGuid():N}");

    public int Port { get; private set; }

    public Uri BaseAddress { get; private set; } = new("http://127.0.0.1/");

    /// <summary>
    /// The password the suite provisions its own logins with.
    /// <para>
    /// The application no longer ships a password of any kind, so the fixture
    /// supplies the accounts it needs through <c>Seed:Accounts</c>, exactly as
    /// a deployment does. This value opens nothing but a throwaway SQLite file
    /// created for one test run.
    /// </para>
    /// </summary>
    public const string DemoPassword = "IntegrationTest#2026!";

    public const string Administrator = "admin@test.invalid";
    public const string Dentist = "dentist@test.invalid";
    public const string OralSurgeon = "surgeon@test.invalid";
    public const string Hygienist = "hygienist@test.invalid";
    public const string PracticeManager = "manager@test.invalid";
    public const string Receptionist = "reception@test.invalid";
    public const string Nurse = "nurse@test.invalid";

    /// <summary>
    /// The logins the suite signs in as, attached to the demonstration staff so
    /// a clinician's screens have something on them.
    /// </summary>
    private static readonly (string Email, string Role, string? Staff)[] TestAccounts =
    [
        (Administrator, "Administrator", null),
        (Dentist, "Dentist", "S-0001"),
        (OralSurgeon, "OralSurgeon", "S-0002"),
        (Hygienist, "Hygienist", "S-0005"),
        (Nurse, "Nurse", "S-0007"),
        (PracticeManager, "PracticeManager", "S-0009"),
        (Receptionist, "Receptionist", "S-0010")
    ];

    // ------------------------------------------------------------------ lifetime

    public async Task InitializeAsync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var webProject = Path.Combine(repositoryRoot, "src", "DentalSurgery.Web");
        var entryPoint = Path.Combine(webProject, "bin", "Debug", "net8.0", "DentalSurgery.Web.dll");

        if (!File.Exists(entryPoint))
        {
            throw new InvalidOperationException(
                $"The web application has not been built at {entryPoint}. " +
                "Run 'dotnet build' before the integration tests.");
        }

        Port = FreePort();
        BaseAddress = new Uri($"http://127.0.0.1:{Port}/");

        var start = new ProcessStartInfo("dotnet", $"\"{entryPoint}\"")
        {
            // The content root, so Razor and static assets resolve as they do
            // when the application is run normally.
            WorkingDirectory = webProject,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var environment = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ASPNETCORE_URLS"] = $"http://127.0.0.1:{Port}",

            ["Database__Provider"] = "Sqlite",
            ["ConnectionStrings__DefaultConnection"] =
                $"Data Source={_databasePath};Cache=Shared;Foreign Keys=True",

            // Demonstration staff exist only in Development, which is itself one
            // of the production-safety behaviours these tests rely on.
            ["Seed__DemoData"] = "true",
            ["Seed__MigrateOnStartup"] = "true",
            ["Seed__AdminEmail"] = Administrator,
            ["Seed__AdminPassword"] = DemoPassword,

            // The suite signs in far more often than a person would. The one
            // test that asserts lockout does not depend on the rate limiter.
            ["RateLimiting__Enabled"] = "false",

            ["Storage__DocumentRoot"] = _documentRoot,
            ["Notifications__SuppressOutbound"] = "true"
        };

        // The suite provisions its own logins the way a deployment does. Nothing
        // is assumed about accounts the application might create on its own.
        for (var i = 0; i < TestAccounts.Length; i++)
        {
            var (email, role, staff) = TestAccounts[i];
            environment[$"Seed__Accounts__{i}__Email"] = email;
            environment[$"Seed__Accounts__{i}__Password"] = DemoPassword;
            environment[$"Seed__Accounts__{i}__Role"] = role;
            environment[$"Seed__Accounts__{i}__MustChangePassword"] = "false";
            if (staff is not null) environment[$"Seed__Accounts__{i}__StaffNumber"] = staff;
        }

        foreach (var (key, value) in environment) start.Environment[key] = value;

        _process = Process.Start(start)
            ?? throw new InvalidOperationException("The application process could not be started.");

        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (_output) _output.Add(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (_output) _output.Add(e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitUntilReadyAsync();
    }

    /// <summary>
    /// Polls readiness until the application answers or the process dies. A
    /// start-up failure is reported with the application's own output, because
    /// "the tests timed out" says nothing a person can act on.
    /// </summary>
    private async Task WaitUntilReadyAsync()
    {
        using var client = new HttpClient { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(5) };

        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (DateTime.UtcNow < deadline)
        {
            if (_process!.HasExited)
            {
                throw new InvalidOperationException(
                    $"The application exited during start-up with code {_process.ExitCode}." +
                    Environment.NewLine + RecentOutput());
            }

            try
            {
                var response = await client.GetAsync("/health/ready");
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            catch (TaskCanceledException)
            {
                // Still starting.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            "The application did not become ready within three minutes." +
            Environment.NewLine + RecentOutput());
    }

    /// <summary>The application's own recent log output, for diagnosing a failure.</summary>
    public string RecentOutput(int lines = 40)
    {
        lock (_output) return string.Join(Environment.NewLine, _output.TakeLast(lines));
    }



    public Task DisposeAsync()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(10_000);
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }

        _process?.Dispose();

        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
        }

        try { if (Directory.Exists(_documentRoot)) Directory.Delete(_documentRoot, recursive: true); }
        catch (IOException) { }

        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ clients

    /// <summary>
    /// A client that keeps cookies and does not chase redirects, so a test can
    /// see the 302 an unauthorised request produces rather than the page it
    /// eventually lands on.
    /// </summary>
    public HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        return new HttpClient(handler) { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(60) };
    }

    /// <summary>Signs a client in, or throws with the reason it could not.</summary>
    public async Task<HttpClient> SignInAsync(string email, string password = DemoPassword)
    {
        var client = CreateClient();
        var response = await TrySignInAsync(client, email, password);

        if (response.StatusCode != HttpStatusCode.Found)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Sign-in for {email} returned {(int)response.StatusCode}, not a redirect. " +
                $"Body starts: {body[..Math.Min(400, body.Length)]}");
        }

        return client;
    }

    /// <summary>
    /// Posts the sign-in form the way a browser does, antiforgery token included.
    /// Returns the response so a test can assert on a refusal.
    /// </summary>
    public async Task<HttpResponseMessage> TrySignInAsync(
        HttpClient client, string email, string password)
    {
        var page = await client.GetAsync("/Account/Login");
        page.EnsureSuccessStatusCode();

        var form = new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(await page.Content.ReadAsStringAsync()),
            ["_handler"] = "login"
        };

        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
    }

    /// <summary>
    /// Pulls the antiforgery token out of a rendered form. A test that skipped
    /// it would be testing a request no browser makes, and would quietly stop
    /// exercising the antiforgery middleware at all.
    /// </summary>
    public static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html, """name="__RequestVerificationToken"[^>]*value="([^"]+)""", RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                html, """value="([^"]+)"[^>]*name="__RequestVerificationToken""", RegexOptions.IgnoreCase);
        }

        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException("The page carried no antiforgery token.");
    }

    // ------------------------------------------------------------------ state

    /// <summary>
    /// Reads or changes account state directly in the test's own database.
    /// <para>
    /// Used only to arrange a precondition the interface has no route to — an
    /// account that is already deactivated, or already locked out. The behaviour
    /// under test is still observed through HTTP.
    /// </para>
    /// </summary>
    public void ExecuteSql(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath};Cache=Shared");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);

        command.ExecuteNonQuery();
    }

    public T? QueryScalar<T>(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath};Cache=Shared");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);

        var result = command.ExecuteScalar();

        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public void SetAccountActive(string email, bool active) =>
        ExecuteSql("update Users set IsActive = $active where Email = $email",
            ("$active", active ? 1 : 0), ("$email", email));

    public void ClearForcedPasswordChange(string email) =>
        ExecuteSql("update Users set MustChangePassword = 0 where Email = $email", ("$email", email));

    public void SetForcedPasswordChange(string email) =>
        ExecuteSql("update Users set MustChangePassword = 1 where Email = $email", ("$email", email));

    public void ClearLockout(string email) =>
        ExecuteSql("update Users set LockoutEnd = null, AccessFailedCount = 0 where Email = $email",
            ("$email", email));

    // ------------------------------------------------------------------ helpers

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DentalSurgery.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find DentalSurgery.sln above the test assembly.");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

/// <summary>
/// One application instance shared by the whole suite. Starting the process and
/// seeding a demonstration practice takes tens of seconds; doing it per test
/// class would dominate the run for no added confidence.
/// </summary>
[CollectionDefinition(Name)]
public class DentalAppCollection : ICollectionFixture<DentalAppFixture>
{
    public const string Name = "dental-app";
}
