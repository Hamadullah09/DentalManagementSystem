using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DentalSurgery.IntegrationTests;

/// <summary>
/// Every page, opened as every role, against the real routing and the real
/// policies.
/// <para>
/// This is the test that would have caught the original defects. A page whose
/// <c>[Authorize]</c> attribute is missing shows up as a role reaching
/// something it should not; a policy name that resolves to nothing shows up as
/// everyone being refused. Both are invisible to a unit test of the permission
/// tables, because both are faults in the wiring rather than in the matrix.
/// </para>
/// </summary>
[Collection(DentalAppCollection.Name)]
public class RouteAuthorizationTests(DentalAppFixture app)
{
    private const bool Allowed = true;
    private const bool Refused = false;

    /// <summary>
    /// Asserts the outcome of opening <paramref name="path"/> as
    /// <paramref name="email"/>. "Allowed" means the page rendered; "refused"
    /// means the access-denied redirect, never a 500 and never a sign-in bounce
    /// for an account that is signed in.
    /// </summary>
    private async Task AssertAccessAsync(string email, string path, bool expected)
    {
        var client = await app.SignInAsync(email);
        var response = await client.GetAsync(path);

        if (expected == Allowed)
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"{email} should be able to open {path}, but got {(int)response.StatusCode} " +
                $"{response.Headers.Location}");
            return;
        }

        Assert.True(response.StatusCode == HttpStatusCode.Found,
            $"{email} should be refused {path}, but got {(int)response.StatusCode}.");

        Assert.True(
            response.Headers.Location!.ToString().Contains("AccessDenied", StringComparison.OrdinalIgnoreCase),
            $"{email} was redirected away from {path} to {response.Headers.Location}, " +
            "which is not the access-denied page.");
    }

    // ------------------------------------------------------------ reception

    [Theory]
    [InlineData("/", Allowed)]
    [InlineData("/patients", Allowed)]
    [InlineData("/patients/new", Allowed)]
    [InlineData("/schedule", Allowed)]
    [InlineData("/waiting-room", Allowed)]
    [InlineData("/recalls", Allowed)]
    [InlineData("/billing/invoices", Allowed)]
    [InlineData("/billing/claims", Allowed)]
    [InlineData("/surgery", Refused)]
    [InlineData("/implants", Refused)]
    [InlineData("/prescriptions", Refused)]
    [InlineData("/radiographs", Refused)]
    [InlineData("/inventory", Refused)]
    [InlineData("/sterilisation", Refused)]
    [InlineData("/reports", Refused)]
    [InlineData("/staff", Refused)]
    [InlineData("/admin", Refused)]
    [InlineData("/admin/users", Refused)]
    [InlineData("/admin/roles", Refused)]
    [InlineData("/admin/audit", Refused)]
    public Task Reception(string path, bool expected) =>
        AssertAccessAsync(DentalAppFixture.Receptionist, path, expected);

    // ------------------------------------------------------------ dentist

    [Theory]
    [InlineData("/patients", Allowed)]
    [InlineData("/surgery", Allowed)]
    [InlineData("/prescriptions", Allowed)]
    [InlineData("/radiographs", Allowed)]
    [InlineData("/treatment-plans", Allowed)]
    [InlineData("/reports", Allowed)]
    [InlineData("/purchase-orders", Refused)]
    [InlineData("/exports", Refused)]
    [InlineData("/staff", Refused)]
    [InlineData("/admin", Refused)]
    [InlineData("/admin/roles", Refused)]
    [InlineData("/admin/audit", Refused)]
    public Task A_dentist(string path, bool expected) =>
        AssertAccessAsync(DentalAppFixture.Dentist, path, expected);

    // ------------------------------------------------------------ surgeon

    [Theory]
    [InlineData("/surgery", Allowed)]
    [InlineData("/implants", Allowed)]
    [InlineData("/prescriptions", Allowed)]
    [InlineData("/admin/roles", Refused)]
    public Task An_oral_surgeon(string path, bool expected) =>
        AssertAccessAsync(DentalAppFixture.OralSurgeon, path, expected);

    // ------------------------------------------------------------ hygienist

    [Theory]
    [InlineData("/patients", Allowed)]
    [InlineData("/sterilisation", Allowed)]
    [InlineData("/radiographs", Allowed)]
    [InlineData("/surgery", Refused)]
    [InlineData("/implants", Refused)]
    [InlineData("/billing/claims", Refused)]
    [InlineData("/reports", Refused)]
    [InlineData("/admin/users", Refused)]
    public Task A_hygienist(string path, bool expected) =>
        AssertAccessAsync(DentalAppFixture.Hygienist, path, expected);

    // ------------------------------------------------------------ management

    [Theory]
    [InlineData("/reports", Allowed)]
    [InlineData("/exports", Allowed)]
    [InlineData("/inventory", Allowed)]
    [InlineData("/staff", Allowed)]
    [InlineData("/admin/users", Allowed)]
    [InlineData("/admin", Allowed)]
    [InlineData("/surgery", Refused)]
    [InlineData("/prescriptions", Refused)]
    [InlineData("/radiographs", Refused)]
    [InlineData("/admin/roles", Refused)]
    [InlineData("/admin/audit", Refused)]
    public Task A_practice_manager(string path, bool expected) =>
        AssertAccessAsync(DentalAppFixture.PracticeManager, path, expected);

    // ------------------------------------------------------------ administrator

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/users")]
    [InlineData("/admin/roles")]
    [InlineData("/admin/audit")]
    [InlineData("/admin/integrations")]
    [InlineData("/reports")]
    [InlineData("/surgery")]
    [InlineData("/patients")]
    public async Task An_administrator_reaches_everything(string path)
    {
        app.ClearForcedPasswordChange(DentalAppFixture.Administrator);
        await AssertAccessAsync(DentalAppFixture.Administrator, path, Allowed);
    }
}

/// <summary>The same question asked of the API, where the answer is a status code.</summary>
[Collection(DentalAppCollection.Name)]
public class ApiAuthorizationTests(DentalAppFixture app)
{
    private async Task<Guid> FirstPatientIdAsync(HttpClient client)
    {
        using var document = System.Text.Json.JsonDocument.Parse(
            await client.GetStringAsync("/api/patients?pageSize=1"));

        var root = document.RootElement;
        var items = root.TryGetProperty("items", out var list) ? list : root;

        return items[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Reception_reads_the_register_and_the_ledger_but_not_the_clinical_record()
    {
        var client = await app.SignInAsync(DentalAppFixture.Receptionist);
        var patientId = await FirstPatientIdAsync(client);

        // The front desk books, registers and takes payment.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/patients")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/patients/{patientId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/patients/{patientId}/account")).StatusCode);

        // It does not read the chart or the medical risk assessment. Both of
        // these answered 200 with real data before the permission model existed.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/patients/{patientId}/chart")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/patients/{patientId}/risk")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/reports/financial")).StatusCode);
    }

    [Fact]
    public async Task A_dentist_reads_the_chart_and_the_risk_assessment()
    {
        var client = await app.SignInAsync(DentalAppFixture.Dentist);
        var patientId = await FirstPatientIdAsync(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/patients/{patientId}/chart")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/patients/{patientId}/risk")).StatusCode);
    }

    [Fact]
    public async Task A_practice_manager_reads_the_money_and_not_the_medicine()
    {
        var client = await app.SignInAsync(DentalAppFixture.PracticeManager);
        var patientId = await FirstPatientIdAsync(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/reports/financial")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/patients/{patientId}/account")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/patients/{patientId}/chart")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/patients/{patientId}/risk")).StatusCode);
    }

    [Fact]
    public async Task A_hygienist_cannot_submit_an_insurance_claim()
    {
        var client = await app.SignInAsync(DentalAppFixture.Hygienist);

        var response = await client.PostAsync($"/api/claims/{Guid.NewGuid()}/submit", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_patient_needs_more_than_being_able_to_read_one()
    {
        var client = await app.SignInAsync(DentalAppFixture.Nurse);

        // A nurse holds Patients.View and not Patients.Create.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/patients")).StatusCode);

        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Should",
            lastName = "NotExist",
            dateOfBirth = "1990-01-01"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

/// <summary>
/// Authorisation inside a page the user is entitled to open.
/// <para>
/// Reception may reach a patient. That is not the same as being allowed to read
/// their clinical notes, and the difference is only visible by asking for the
/// tab directly — which is exactly what someone would do with the URL.
/// </para>
/// </summary>
[Collection(DentalAppCollection.Name)]
public class PatientRecordSectionTests(DentalAppFixture app)
{
    private async Task<Guid> FirstPatientIdAsync(HttpClient client)
    {
        using var document = System.Text.Json.JsonDocument.Parse(
            await client.GetStringAsync("/api/patients?pageSize=1"));

        var root = document.RootElement;
        var items = root.TryGetProperty("items", out var list) ? list : root;

        return items[0].GetProperty("id").GetGuid();
    }

    [Theory]
    [InlineData("notes")]
    [InlineData("medical")]
    [InlineData("chart")]
    [InlineData("perio")]
    [InlineData("prescriptions")]
    public async Task Reception_is_refused_the_clinical_sections_of_a_patient_it_may_open(string tab)
    {
        var client = await app.SignInAsync(DentalAppFixture.Receptionist);
        var patientId = await FirstPatientIdAsync(client);

        var response = await client.GetAsync($"/patients/{patientId}/{tab}");
        var html = await response.Content.ReadAsStringAsync();

        // The page loads — reception may reach the patient — and refuses the section.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("You cannot open this section", html);
    }

    [Fact]
    public async Task Reception_is_not_even_offered_the_clinical_tabs()
    {
        var client = await app.SignInAsync(DentalAppFixture.Receptionist);
        var patientId = await FirstPatientIdAsync(client);

        var html = await client.GetStringAsync($"/patients/{patientId}");

        Assert.Contains("Overview", html);
        Assert.Contains("Appointments", html);

        Assert.DoesNotContain("Clinical notes", html);
        Assert.DoesNotContain("Dental chart", html);
        Assert.DoesNotContain("Periodontal", html);
    }

    [Theory]
    [InlineData("notes")]
    [InlineData("medical")]
    [InlineData("chart")]
    [InlineData("perio")]
    public async Task A_dentist_opens_the_same_sections_without_a_refusal(string tab)
    {
        var client = await app.SignInAsync(DentalAppFixture.Dentist);
        var patientId = await FirstPatientIdAsync(client);

        var response = await client.GetAsync($"/patients/{patientId}/{tab}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("You cannot open this section", html);
    }
}
