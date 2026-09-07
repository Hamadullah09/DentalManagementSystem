using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Persistence.Seed;
using DentalSurgery.Web.Components;
using DentalSurgery.Web.Components.Account;
using DentalSurgery.Infrastructure.Services;
using DentalSurgery.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

// QuestPDF is free under its community licence below the revenue threshold.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- logging
// Structured, so that a production incident can be queried rather than read.
// Console output is JSON in a deployment, where a log shipper consumes it, and
// human-readable text locally.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "DentalSurgery")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning);

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console();
    }
    else
    {
        configuration.WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    }
});

// Uploads are capped in DocumentService; this raises the transport limit to match.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = DocumentService.MaxUploadBytes + (1024 * 1024);
    options.ValueLengthLimit = int.MaxValue;
});

// ---------------------------------------------------------------- presentation
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums are written as names, so they must also be read as names.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dental Surgery API",
        Version = "v1",
        Description = "Read and write access to patients, the appointment book, clinical records and billing."
    });
});

// ---------------------------------------------------------------- identity
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ICurrentUser, WebCurrentUser>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // The session cookie carries access to patient records. It must never be
    // readable by script, never travel unencrypted, and never ride along on a
    // cross-site request.
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;

    // The __Host- prefix is the strongest binding a cookie can have: the browser
    // enforces Secure, host-only scope and Path=/, so a neighbouring subdomain
    // cannot overwrite the session. It also means the browser rejects the cookie
    // outright over plain HTTP, which is why the development profile — served on
    // http://localhost — uses an unprefixed name instead.
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "DentalSurgery.Session";
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Name = "__Host-DentalSurgery.Session";
    }

    options.Events.OnValidatePrincipal = AccountSecurityCookieEvents.ValidatePrincipalAsync;
});

// ---------------------------------------------------------------- infrastructure
builder.Services.AddDentalInfrastructure(builder.Configuration);

// ---------------------------------------------------------------- hardening
builder.Services.AddSharedDataProtection(builder.Configuration, builder.Environment);
builder.Services.AddProxyAwareness();
builder.Services.AddRequestThrottling(builder.Configuration);
builder.Services.AddApplicationHealthChecks();

builder.Services.AddIdentityCore<ApplicationUser>(DependencyInjection.ConfigureIdentityOptions)
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<DentalDbContext>()
    .AddSignInManager<DentalSignInManager>()
    .AddClaimsPrincipalFactory<DentalClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------------------------------------------------------------- authorisation
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.CanViewClinical, p => p.RequireRole(
        Roles.Administrator, Roles.PracticeManager, Roles.Dentist, Roles.Hygienist,
        Roles.Nurse, Roles.Receptionist, Roles.ReadOnly))
    .AddPolicy(Policies.CanEditClinical, p => p.RequireRole(
        Roles.Administrator, Roles.Dentist, Roles.Hygienist, Roles.Nurse))
    .AddPolicy(Policies.CanPrescribe, p => p.RequireRole(
        Roles.Administrator, Roles.Dentist))
    .AddPolicy(Policies.CanManageSchedule, p => p.RequireRole(
        Roles.Administrator, Roles.PracticeManager, Roles.Receptionist, Roles.Dentist, Roles.Hygienist))
    .AddPolicy(Policies.CanManageBilling, p => p.RequireRole(
        Roles.Administrator, Roles.PracticeManager, Roles.Accounts, Roles.Receptionist))
    .AddPolicy(Policies.CanManageInventory, p => p.RequireRole(
        Roles.Administrator, Roles.PracticeManager, Roles.Nurse))
    .AddPolicy(Policies.CanManageStaff, p => p.RequireRole(
        Roles.Administrator, Roles.PracticeManager))
    .AddPolicy(Policies.CanViewReports, p => p.RequireRole(
        Roles.Administrator, Roles.PracticeManager, Roles.Accounts, Roles.Dentist))
    .AddPolicy(Policies.CanAdminister, p => p.RequireRole(Roles.Administrator));

var app = builder.Build();

// ---------------------------------------------------------------- database
await using (var scope = app.Services.CreateAsyncScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>();
    await initialiser.InitialiseAsync();
}

// ---------------------------------------------------------------- pipeline

// First in the pipeline: everything downstream needs the real scheme and
// client address rather than the load balancer's.
app.UseForwardedHeaders();

// One structured line per request. Serilog's RequestPath is the path only, so
// the query string never reaches the log — which matters here, because the
// patient search submits names as a query parameter and those must not be
// written to a log shipper.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, exception) =>
        exception is not null ? LogEventLevel.Error
        : httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
        : httpContext.Response.StatusCode == 429 ? LogEventLevel.Warning
        // Health probes fire constantly and would drown everything else.
        : httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
        : LogEventLevel.Information;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());

        if (httpContext.User.Identity?.IsAuthenticated == true)
            diagnosticContext.Set("User", httpContext.User.Identity.Name);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Dental Surgery API v1");
        o.RoutePrefix = "api-docs";
        o.DocumentTitle = "Dental Surgery API";
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// After authentication, and it must stay there.
//
// An antiforgery token is bound to the user it was issued to. Validating it
// before the authentication middleware has resolved HttpContext.User compares
// the token against an anonymous principal, so every form posted by a signed-in
// user is rejected with "the provided antiforgery token was meant for a
// different claims-based user". Anonymous posts such as sign-in still succeed,
// which is what makes the fault look intermittent rather than total.
app.UseAntiforgery();

// After authentication, so it knows who the user is; before the endpoints, so
// an account owing a password change cannot reach any of them.
app.UseMustChangePassword();

app.MapApplicationHealthChecks();

app.MapControllers().RequireRateLimiting(ProductionHardening.ApiPolicy);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .ApplySignInRateLimit();

app.MapAdditionalIdentityEndpoints();

app.Run();
