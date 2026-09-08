using DentalSurgery.Application.Abstractions;

namespace DentalSurgery.Web.Components.Pages.Home;

/// <summary>
/// Which of the six primary roles a signed-in user is working as, and what
/// their home screen should lead with.
/// <para>
/// A user may hold more than one role. Rather than merge the layouts into
/// something that suits nobody, the most specialised role wins and decides the
/// arrangement. What they are permitted to <em>see</em> is still decided by
/// permission, so a dentist who is also the practice manager gets the clinical
/// layout with the financial panels their permissions allow.
/// </para>
/// </summary>
public enum HomeFocus
{
    /// <summary>Signed in, but holding no role that maps to a home screen.</summary>
    None,
    FrontDesk,
    Clinician,
    Surgeon,
    Hygiene,
    Management,
    Administration
}

public static class HomeFocusResolver
{
    /// <summary>
    /// Picks the home layout for a set of roles. The order is deliberate: the
    /// narrowest, most operationally specific role wins, because that is the
    /// work the person is most likely doing when they open the application.
    /// </summary>
    public static HomeFocus Resolve(IReadOnlyList<string> roles)
    {
        if (roles.Contains(Roles.OralSurgeon)) return HomeFocus.Surgeon;
        if (roles.Contains(Roles.Hygienist)) return HomeFocus.Hygiene;
        if (roles.Contains(Roles.Dentist)) return HomeFocus.Clinician;
        if (roles.Contains(Roles.Nurse)) return HomeFocus.Clinician;
        if (roles.Contains(Roles.Receptionist)) return HomeFocus.FrontDesk;
        if (roles.Contains(Roles.PracticeManager)) return HomeFocus.Management;
        if (roles.Contains(Roles.Accounts)) return HomeFocus.Management;
        if (roles.Contains(Roles.Administrator)) return HomeFocus.Administration;
        if (roles.Contains(Roles.ReadOnly)) return HomeFocus.Management;

        return HomeFocus.None;
    }

    /// <summary>The heading shown above the home screen.</summary>
    public static string Title(HomeFocus focus) => focus switch
    {
        HomeFocus.FrontDesk => "Front desk",
        HomeFocus.Clinician => "My clinic today",
        HomeFocus.Surgeon => "Surgical list",
        HomeFocus.Hygiene => "Hygiene clinic",
        HomeFocus.Management => "Practice overview",
        HomeFocus.Administration => "System overview",
        _ => "Home"
    };

    /// <summary>One line describing what this screen is for.</summary>
    public static string Purpose(HomeFocus focus) => focus switch
    {
        HomeFocus.FrontDesk =>
            "Who is here, who is coming, and what still needs a call.",
        HomeFocus.Clinician =>
            "Your list, your waiting patients and the records still open.",
        HomeFocus.Surgeon =>
            "Your operating list, consent status and the cases needing follow-up.",
        HomeFocus.Hygiene =>
            "Your hygiene list, periodontal reviews and recalls falling due.",
        HomeFocus.Management =>
            "How the practice is running today, and what is holding it up.",
        HomeFocus.Administration =>
            "Accounts, access and the health of the system.",
        _ => "Your account is not yet set up for clinical work."
    };
}
