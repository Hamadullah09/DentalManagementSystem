using System.Net;
using Xunit;

namespace DentalSurgery.IntegrationTests;

/// <summary>
/// The unauthenticated surface. Everything here was a real defect found by
/// exploiting it, so each test names the behaviour rather than the fix.
/// </summary>
[Collection(DentalAppCollection.Name)]
public class UnauthenticatedSurfaceTests(DentalAppFixture app)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/patients")]
    [InlineData("/patients/new")]
    [InlineData("/schedule")]
    [InlineData("/waiting-room")]
    [InlineData("/treatment-plans")]
    [InlineData("/surgery")]
    [InlineData("/prescriptions")]
    [InlineData("/billing/invoices")]
    [InlineData("/inventory")]
    [InlineData("/reports")]
    [InlineData("/admin")]
    [InlineData("/admin/users")]
    [InlineData("/admin/roles")]
    [InlineData("/admin/audit")]
    public async Task An_anonymous_visitor_is_sent_to_sign_in(string path)
    {
        var client = app.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("/api/patients")]
    [InlineData("/api/schedule/day")]
    [InlineData("/api/reports/dashboard")]
    [InlineData("/api/exports")]
    public async Task An_anonymous_api_call_is_refused_with_a_status_not_a_page(string path)
    {
        var client = app.CreateClient();

        var response = await client.GetAsync(path);

        // A redirect to an HTML sign-in page answers 200 to a client that
        // follows it, which is indistinguishable from success.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/Account/Register")]
    [InlineData("/Account/RegisterConfirmation")]
    [InlineData("/Account/ResendEmailConfirmation")]
    [InlineData("/Account/ExternalLogin")]
    [InlineData("/Account/Manage/ExternalLogins")]
    [InlineData("/Account/Manage/DeletePersonalData")]
    public async Task There_is_no_self_service_account_creation(string path)
    {
        var client = app.CreateClient();

        var response = await client.GetAsync(path);

        // Anonymous self-registration let anyone create an account and read
        // every patient record. The pages are gone, not merely hidden.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_sign_in_page_carries_none_of_the_application_shell()
    {
        var client = app.CreateClient();

        var html = await client.GetStringAsync("/Account/Login");

        // These are live practice figures. They were rendered to anyone who
        // could reach the sign-in page, signed in or not.
        Assert.DoesNotContain("nav-badge", html);
        Assert.DoesNotContain("app-sidebar", html);
        Assert.DoesNotContain("Search patients", html);

        // Nor the scaffolding that advertised the removed flows.
        Assert.DoesNotContain("Register as a new user", html);
        Assert.DoesNotContain("external authentication services", html);

        // What it should carry.
        Assert.Contains("auth-shell", html);
        Assert.Contains("Sign in", html);
    }

    [Theory]
    [InlineData("//evil.example")]
    [InlineData("/%5C%5Cevil.example")]
    [InlineData("https://evil.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/patients")]
    public async Task A_successful_sign_in_only_ever_redirects_within_this_application(string target)
    {
        // Asserted on the redirect a successful sign-in produces, because that
        // is where an open redirect would actually be exploited. The value
        // appearing in the form's own action is not the vulnerability — it is
        // url-encoded there and never followed; what mattered is where the
        // browser is sent afterwards.
        var client = app.CreateClient();
        var returnUrl = Uri.EscapeDataString(target);

        var page = await client.GetAsync($"/Account/Login?ReturnUrl={returnUrl}");
        page.EnsureSuccessStatusCode();

        var form = new Dictionary<string, string>
        {
            ["Input.Email"] = DentalAppFixture.Receptionist,
            ["Input.Password"] = DentalAppFixture.DemoPassword,
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] =
                DentalAppFixture.ExtractAntiforgeryToken(await page.Content.ReadAsStringAsync()),
            ["_handler"] = "login"
        };

        var response = await client.PostAsync(
            $"/Account/Login?ReturnUrl={returnUrl}", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var location = response.Headers.Location!;
        var destination = location.IsAbsoluteUri ? location : new Uri(app.BaseAddress, location);

        Assert.Equal(app.BaseAddress.Host, destination.Host);
        Assert.Equal(app.BaseAddress.Port, destination.Port);
        Assert.DoesNotContain("evil.example", destination.ToString());
    }

    [Fact]
    public async Task Health_probes_stay_open_so_an_orchestrator_can_reach_them()
    {
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }
}

/// <summary>Sign-in behaviour: what is refused, and what the refusal reveals.</summary>
[Collection(DentalAppCollection.Name)]
public class SignInTests(DentalAppFixture app)
{
    [Fact]
    public async Task A_valid_sign_in_is_accepted()
    {
        var client = app.CreateClient();

        var response = await app.TrySignInAsync(client, DentalAppFixture.Receptionist, DentalAppFixture.DemoPassword);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_address_and_a_wrong_password_give_the_same_answer()
    {
        var client = app.CreateClient();

        var unknown = await app.TrySignInAsync(client, "nobody@nowhere.example", "Whatever#2026!");
        var wrong = await app.TrySignInAsync(client, DentalAppFixture.Receptionist, "Wrong#Password1!");

        var unknownBody = await unknown.Content.ReadAsStringAsync();
        var wrongBody = await wrong.Content.ReadAsStringAsync();

        // Telling the two apart lets anyone confirm which addresses hold
        // accounts here, which is the first step of a credential-stuffing run.
        Assert.Contains("were not recognised", unknownBody);
        Assert.Contains("were not recognised", wrongBody);
    }

    [Fact]
    public async Task A_deactivated_account_cannot_sign_in()
    {
        const string email = DentalAppFixture.Nurse;

        app.SetAccountActive(email, active: false);

        try
        {
            var client = app.CreateClient();
            var response = await app.TrySignInAsync(client, email, DentalAppFixture.DemoPassword);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("cannot sign in", body);
        }
        finally
        {
            app.SetAccountActive(email, active: true);
        }
    }

    [Fact]
    public async Task Deactivating_an_account_ends_the_session_it_already_holds()
    {
        const string email = DentalAppFixture.Hygienist;

        var client = await app.SignInAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/patients")).StatusCode);

        app.SetAccountActive(email, active: false);

        try
        {
            // The cookie is still valid by its own lifetime; the account is not.
            var afterDeactivation = await client.GetAsync("/patients");
            Assert.Equal(HttpStatusCode.Found, afterDeactivation.StatusCode);
        }
        finally
        {
            app.SetAccountActive(email, active: true);
        }
    }

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account()
    {
        const string email = DentalAppFixture.Nurse;
        var client = app.CreateClient();

        // The policy is five attempts. Lockout was configured but inert,
        // because the sign-in call passed lockoutOnFailure: false.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await app.TrySignInAsync(client, email, $"Wrong#Password{attempt}!");
        }

        var afterLockout = await app.TrySignInAsync(client, email, DentalAppFixture.DemoPassword);

        try
        {
            // The correct password now fails too: the account, not the guess,
            // is what is refused.
            Assert.Equal(HttpStatusCode.Found, afterLockout.StatusCode);

            Assert.True(
                afterLockout.Headers.Location!.ToString().Contains("Lockout"),
                $"Expected the lockout page; went to {afterLockout.Headers.Location}." +
                Environment.NewLine + app.RecentOutput(15));
        }
        finally
        {
            app.ClearLockout(email);
        }
    }

    [Fact]
    public async Task The_bootstrap_administrator_must_replace_its_password_before_anything_opens()
    {
        // A fresh administrator is created owing a password change. Until it is
        // done the account can reach exactly one page.
        app.SetForcedPasswordChange(DentalAppFixture.Administrator);

        var client = await app.SignInAsync(DentalAppFixture.Administrator);

        var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Found, home.StatusCode);
        Assert.Contains("ChangePassword", home.Headers.Location!.ToString());

        var patients = await client.GetAsync("/patients");
        Assert.Contains("ChangePassword", patients.Headers.Location!.ToString());

        // An API caller gets a status rather than a redirect to a form.
        var api = await client.GetAsync("/api/patients");
        Assert.Equal(HttpStatusCode.Forbidden, api.StatusCode);

        app.ClearForcedPasswordChange(DentalAppFixture.Administrator);
    }

    [Fact]
    public async Task The_session_cookie_is_not_readable_by_script()
    {
        var client = app.CreateClient();

        var response = await app.TrySignInAsync(client, DentalAppFixture.Dentist, DentalAppFixture.DemoPassword);

        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToArray()
            : [];

        var session = cookies.FirstOrDefault(c => c.Contains("DentalSurgery.Session"));

        Assert.NotNull(session);
        Assert.Contains("httponly", session!.ToLowerInvariant());
        Assert.Contains("samesite=strict", session.ToLowerInvariant());
    }
}
