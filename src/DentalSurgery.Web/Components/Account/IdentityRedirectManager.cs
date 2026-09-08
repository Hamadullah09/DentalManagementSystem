using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace DentalSurgery.Web.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    [DoesNotReturn]
    public void RedirectTo(string? uri)
    {
        uri = MakeLocal(uri);

        // During static rendering, NavigateTo throws a NavigationException which is handled by the framework as a redirect.
        // So as long as this is called from a statically rendered Identity component, the InvalidOperationException is never thrown.
        navigationManager.NavigateTo(uri);
        throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
    }

    /// <summary>
    /// Reduces a candidate redirect target to a path inside this application,
    /// falling back to the home page.
    /// <para>
    /// The scaffolded check this replaces asked only whether the string was a
    /// well-formed relative URI. That is not enough: <c>//evil.example</c> and
    /// <c>/\evil.example</c> both satisfy it, and both are read by browsers as
    /// protocol-relative absolute URLs. A sign-in page that honours them is an
    /// open redirect — the standard way to make a phishing link look like it
    /// points at the practice's own domain.
    /// </para>
    /// <para>
    /// It has to accept three shapes, because all three are used: a rooted path
    /// from a ReturnUrl (<c>/patients</c>), a relative path from the identity
    /// pages themselves (<c>Account/Lockout</c>), and an absolute URL on this
    /// origin, which the query-parameter overload below produces. Rejecting the
    /// bare relative form would silently send the lockout, two-factor and
    /// set-password redirects to the home page instead of where they belong.
    /// </para>
    /// </summary>
    internal string MakeLocal(string? uri)
    {
        const string home = "/";

        if (string.IsNullOrWhiteSpace(uri)) return home;

        var candidate = uri.Trim();

        // Backslashes are normalised to forward slashes by some browsers, so
        // they have to be judged as if they already were.
        var normalised = candidate.Replace('\\', '/');

        if (normalised.Any(char.IsControl)) return home;

        // An authority ("//host") is an absolute URL wearing a relative coat.
        // Checked on the percent-decoded form as well, so that "%5C%5Chost"
        // cannot smuggle one past a check that only reads the raw characters.
        if (LooksLikeAnAuthority(normalised)) return home;

        var baseUri = new Uri(navigationManager.BaseUri);

        // Anything carrying a scheme — https:, javascript:, data: — is judged as
        // absolute, and survives only if it names this very origin.
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteCandidate))
        {
            return SameOrigin(absoluteCandidate, baseUri)
                ? absoluteCandidate.PathAndQuery + absoluteCandidate.Fragment
                : home;
        }

        // Rooted or relative. Resolve against the base so an application hosted
        // under a path prefix cannot be escaped with "/../".
        Uri resolved;
        try
        {
            resolved = navigationManager.ToAbsoluteUri(normalised);
        }
        catch (UriFormatException)
        {
            return home;
        }

        return SameOrigin(resolved, baseUri)
            ? resolved.PathAndQuery + resolved.Fragment
            : home;
    }

    /// <summary>
    /// True when the target begins with an authority, before or after percent
    /// decoding. Decoding is attempted repeatedly because a doubly encoded value
    /// decodes to a singly encoded one.
    /// </summary>
    private static bool LooksLikeAnAuthority(string value)
    {
        var current = value;

        for (var pass = 0; pass < 3; pass++)
        {
            if (current.StartsWith("//", StringComparison.Ordinal)) return true;

            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(current).Replace('\\', '/');
            }
            catch (UriFormatException)
            {
                return true;
            }

            if (decoded == current) break;
            current = decoded;
        }

        return current.StartsWith("//", StringComparison.Ordinal);
    }

    /// <summary>True when the target is this application, path prefix included.</summary>
    private static bool SameOrigin(Uri candidate, Uri baseUri) =>
        candidate.Scheme == baseUri.Scheme
        && candidate.Authority == baseUri.Authority
        && candidate.AbsolutePath.StartsWith(baseUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);

    [DoesNotReturn]
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    [DoesNotReturn]
    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    [DoesNotReturn]
    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    [DoesNotReturn]
    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);
}
