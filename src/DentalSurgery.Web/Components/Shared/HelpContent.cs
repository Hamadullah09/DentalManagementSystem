namespace DentalSurgery.Web.Components.Shared;

/// <summary>
/// What a page is for, what its fields mean and what can be done on it.
/// <para>
/// Every entry describes a page that exists in this build. Nothing here is
/// aspirational: help that promises a control the page does not have is worse
/// than no help, because the reader spends their time looking for it.
/// </para>
/// </summary>
public record HelpTopic(
    string Title,
    string Purpose,
    IReadOnlyList<string> Fields,
    IReadOnlyList<string> Actions,
    IReadOnlyList<(string Keys, string Does)>? Shortcuts = null);

public static class HelpContent
{
    private static readonly (string Keys, string Does)[] GlobalShortcuts =
    [
        ("Ctrl + H", "Open and close this help panel"),
        ("F1", "The same, for browsers that keep Ctrl + H for themselves"),
        ("?", "The same again, when you are not typing in a field"),
        ("Esc", "Close this panel, or the dialog on top of it")
    ];

    /// <summary>
    /// Keyed by route prefix, longest match first. A patient sub-tab therefore
    /// resolves to its own topic rather than to the patient record's.
    /// </summary>
    private static readonly Dictionary<string, HelpTopic> Topics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/"] = new(
            "Home",
            "Your home screen is built around your role. A clinician sees their own list and the records they have left open; the front desk sees who is in the building and who still needs a call; a practice manager sees how the day is running. Figures you are not permitted to see are not shown at all rather than hidden behind a message.",
            [
                "Needs attention — standing exceptions that stay true until someone deals with them, such as a failed steriliser cycle or an overdue laboratory case.",
                "Production — the value of treatment carried out. Collections — money actually received. They differ, and both are shown month to date.",
                "Chair utilisation — booked chair time as a proportion of the time rostered providers were available."
            ],
            [
                "Use the quick actions along the top for the things your role does repeatedly.",
                "Click any row in a list to open that patient's record.",
                "Click a 'needs attention' badge to go to the screen where it can be resolved."
            ]),

        ["/patients"] = new(
            "Patients",
            "The patient register. Search, filter and open a record. What you can see inside a record depends on your role: reaching a patient is not the same as being allowed to read their clinical notes.",
            [
                "Patient number — the practice's own reference, allocated in sequence and never reused.",
                "Balance — what the patient owes after insurance, in red when it is outstanding.",
                "Recall due — when they are next due for review. Overdue entries are highlighted.",
                "Alerts — active clinical or administrative warnings; a red count means at least one is critical."
            ],
            [
                "Search by name, patient number, telephone number or email address.",
                "Use the quick filters for recalls due, patients who owe money, and patients with alerts.",
                "Export the filtered list if you hold the reporting permission."
            ]),

        ["/patients/new"] = new(
            "Register a patient",
            "Creates a new patient record and allocates the next patient number. Only demographic and contact details are captured here; medical history is recorded separately once the patient has been created.",
            [
                "Date of birth — required, and used to calculate age wherever it appears.",
                "Preferred contact method — decides which channel reminders and recalls are sent on.",
                "Primary provider — the clinician the patient is normally booked with."
            ],
            [
                "Required fields are marked with a red asterisk.",
                "The form will not submit twice: the button disables while it is saving."
            ]),

        ["/schedule"] = new(
            "Appointment book",
            "The practice diary. Book, move and cancel appointments, and move patients through the visit. Conflicts are checked on the server when you save, so a booking cannot be created against a provider who is unavailable or an operatory that is already occupied.",
            [
                "Status — Unconfirmed, Confirmed, Arrived, Seated, In treatment, Completed, Checked out, Cancelled, No-show.",
                "Operatory — the chair the appointment occupies. Two appointments cannot hold the same chair at once.",
                "Provider colour — each clinician has a colour so the day reads at a glance."
            ],
            [
                "Choose a day and switch between provider and operatory views.",
                "Change an appointment's status from its row to check a patient in or out.",
                "Cancelling asks for a reason, which is recorded against the appointment."
            ]),

        ["/waiting-room"] = new(
            "Waiting room",
            "Everyone currently in the building, in the order they arrived. Waiting times are counted from check-in, and a patient waiting longer than expected is highlighted so the delay is noticed before the patient mentions it.",
            [
                "Waiting — minutes since check-in, not minutes since the appointment time.",
                "Status — where the patient is in the visit."
            ],
            [
                "Move a patient through arrived, seated, in treatment and completed.",
                "Open the patient record from the row to see clinical alerts before they are seated."
            ]),

        ["/treatment-plans"] = new(
            "Treatment plans",
            "Plans across the whole practice, with what each one is worth and whether the patient has answered. Totals, insurance estimates and the patient's share are calculated on the server from the fee schedule; they are not taken from anything typed into the browser.",
            [
                "Status — Draft, Presented, Accepted, Partially accepted, Declined, In progress, Completed, Expired, Superseded.",
                "Patient portion — the estimated amount the patient pays after the insurance estimate.",
                "Valid until — after this date the plan is treated as expired and needs re-costing."
            ],
            [
                "Present a plan to record that it was discussed with the patient.",
                "Record the patient's acceptance or refusal; both are kept, along with the date.",
                "Create a revision rather than editing an answered plan, so the earlier version survives."
            ]),

        ["/surgery"] = new(
            "Surgical register",
            "Operations booked and carried out, with their consent status. A procedure that requires written consent cannot be recorded as complete until that consent is on the record.",
            [
                "Consent — whether consent has been obtained and when it was signed.",
                "Outcome — recorded after the operation, along with any complication."
            ],
            [
                "Open a case to record operative findings, materials, implants and post-operative instructions.",
                "Follow-up appointments are booked from the patient's record."
            ]),

        ["/implants"] = new(
            "Implant registry",
            "Every implant placed by the practice, with its manufacturer, system, dimensions and lot number. Kept so an implant can be traced to a patient, and a patient's implant identified years later.",
            [
                "Lot number — the manufacturer's batch, needed if a product is recalled.",
                "Site — the tooth position the implant occupies."
            ],
            [
                "Search by patient, manufacturer or placement date.",
                "Open the patient record for the surgical note that accompanies the placement."
            ]),

        ["/prescriptions"] = new(
            "Prescriptions",
            "Prescriptions issued by the practice. Issuing one runs an interaction and allergy check against the patient's recorded medical history first, and warnings must be acknowledged before the prescription can be produced.",
            [
                "Status — issued, printed or cancelled.",
                "Prescriber — the clinician responsible; only clinicians permitted to prescribe appear here."
            ],
            [
                "Prescriptions are issued from the patient's record, not from this list.",
                "Print or download a prescription as a PDF."
            ]),

        ["/billing/invoices"] = new(
            "Invoices",
            "Charges raised against patients. Every total is calculated on the server; changing a figure in the browser has no effect on what is stored. Invoices are never deleted — an incorrect one is voided, with a reason, and the void is recorded.",
            [
                "Status — Draft, Issued, Part paid, Paid, Void.",
                "Patient responsibility — what is owed after the insurance estimate has been applied."
            ],
            [
                "Issue a draft invoice to make it payable.",
                "Record a payment and allocate it across invoices.",
                "Void an invoice with a reason; refunds are a separate permission."
            ]),

        ["/billing/receivables"] = new(
            "Outstanding balances",
            "What the practice is owed, aged by how long it has been outstanding. Amounts pending with insurers are shown separately from amounts owed by patients, because they are chased differently.",
            [
                "Ageing — current, 1–30, 31–60, 61–90 and over 90 days from the invoice date.",
                "Insurance pending — submitted to a payer and not yet settled."
            ],
            [
                "Sort by the oldest balance to find what has been outstanding longest.",
                "Open a patient to see the ledger behind their balance."
            ]),

        ["/billing/claims"] = new(
            "Insurance claims",
            "Claims to payers, from being built through to being paid. A claim is generated from the procedures actually recorded, so it cannot claim for treatment that was never carried out.",
            [
                "Status — Draft, Ready to send, Submitted, In review, Approved, Partially approved, Denied, Paid, Appealed, Closed.",
                "Interchange — the generated X12 837D content, which can be previewed before submission."
            ],
            [
                "Preview a claim before submitting it.",
                "Submit individually or as a batch, if you hold the submission permission.",
                "Record the payer's response and the amount paid."
            ]),

        ["/inventory"] = new(
            "Stock control",
            "Consumables and materials, by lot and expiry. Stock moves when a procedure consumes it, so levels reflect what has actually been used rather than what someone remembered to record.",
            [
                "Reorder level — the point at which an item is flagged as low.",
                "Lot and expiry — needed for traceability and to spot stock about to expire."
            ],
            [
                "Receive stock against a purchase order.",
                "Adjust a level with a reason when a physical count disagrees.",
                "Review lots expiring within ninety days."
            ]),

        ["/sterilisation"] = new(
            "Sterilisation",
            "Steriliser cycles and what was in them. A failed cycle quarantines every set in the load; because instrument sets are linked to the patients they were used on, a failure can be traced to the people affected.",
            [
                "Indicators — the chemical and biological checks that decide whether a cycle passed.",
                "Result — pass, fail or quarantined. Nothing from a failed load may be released."
            ],
            [
                "Record a cycle with its load contents and indicator results.",
                "Release a passed load, or trace a failed one to the sets and patients involved."
            ]),

        ["/lab-cases"] = new(
            "Laboratory",
            "Work sent out to dental laboratories, from being raised through to being fitted. Cases past their due date are flagged, because the appointment the work was ordered for usually has to move with it.",
            [
                "Status — Draft, Sent, In production, Shipped, Received, Try-in, Adjusting, Delivered, Remake, Cancelled.",
                "Due date — when the work is needed back, normally before the fitting appointment."
            ],
            [
                "Raise a case against a patient and a laboratory.",
                "Move a case through its stages as it progresses.",
                "Record a remake, which keeps the original case for the laboratory's quality record."
            ]),

        ["/reports"] = new(
            "Reports",
            "Operational, clinical and financial reporting over a date range you choose. Financial reports need a separate permission from the rest, so a clinical report can be run without exposing the practice's revenue.",
            [
                "Date range — every report is bounded; nothing runs unbounded across the whole history.",
                "Provider — most reports can be narrowed to one clinician."
            ],
            [
                "Choose a report and a period, then run it.",
                "Export to CSV, Excel, JSON or PDF if you hold the export permission."
            ]),

        ["/tasks"] = new(
            "Tasks",
            "Work assigned to people rather than to patients: calls to make, forms to chase, equipment to check. Overdue tasks are highlighted.",
            [
                "Priority — decides the order the list is shown in.",
                "Due date — tasks past it are marked overdue."
            ],
            [
                "Create a task and assign it to a member of staff.",
                "Complete a task to take it off everyone's list."
            ]),

        ["/staff"] = new(
            "Team",
            "Clinicians and support staff, with the registration and indemnity dates the practice has to keep current. Registrations expiring within sixty days are flagged.",
            [
                "Provider — whether this person can be booked with patients.",
                "Registration expiry — the professional registration date; lapsing it stops them working."
            ],
            [
                "Review who is due to renew a registration or indemnity policy.",
                "Login accounts are managed separately, under User accounts."
            ]),

        ["/admin/users"] = new(
            "User accounts",
            "The logins that can reach this system. There is no self-registration: an account exists only because an administrator created it. A new account is required to change its password at first sign-in, so the password you hand over stops working immediately afterwards.",
            [
                "Role — decides what the account may do. An account with no role can sign in and open nothing.",
                "Linked staff record — attributes clinical work to the right clinician.",
                "State — active, deactivated, locked out, or owing a password change."
            ],
            [
                "Create an account, choose its role, and hand over the temporary password shown once.",
                "Deactivate an account to withdraw access immediately, keeping everything it has signed.",
                "Reset a password or clear a lockout after five failed sign-in attempts."
            ]),

        ["/admin/roles"] = new(
            "Roles and permissions",
            "What each role is allowed to do. This is the live grant, not a description of one: a tick here is what the server checks when a page is opened and again when a service is asked to do the work. Changes take effect on the affected users' next request.",
            [
                "Module — permissions are grouped by the part of the system they govern.",
                "Administrator — always holds every permission, and cannot be reduced from here; that is what stops the system being left with nobody able to administer it."
            ],
            [
                "Click a role card to highlight its column.",
                "Filter by module name to find a permission quickly.",
                "Tick or untick, then save. Nothing is written until you do."
            ]),

        ["/admin/audit"] = new(
            "Audit trail",
            "Who did what, and when. Entries are written by the application as work happens and are not editable from anywhere in the interface — an audit trail that can be corrected is not an audit trail.",
            [
                "Action — created, updated, deleted, read, signed in, sign-in failed, exported, permission changed.",
                "Resource — the record the action was performed on, with its identifier."
            ],
            [
                "Filter by user, action, date range or resource type.",
                "Use it to answer who opened a particular patient's record, and when."
            ]),

        ["/admin"] = new(
            "Configuration",
            "Practice details, opening hours, operatories, fee schedules and recall intervals. These are reference data the rest of the system reads: changing a fee schedule changes what new treatment plans cost, and does not alter plans already presented.",
            [
                "Opening hours — used when checking whether an appointment can be booked.",
                "Fee schedule — the price list treatment plans and invoices are costed from."
            ],
            [
                "Edit practice and location details.",
                "Import a fee schedule from a spreadsheet, previewing the change before applying it."
            ]),

        ["/admin/integrations"] = new(
            "Integrations",
            "The status of the outside services this system talks to: email, SMS and insurance claim submission. Secret values are never displayed — the page reports whether a channel is configured and working, not what its credentials are.",
            [
                "Configured — whether the channel has enough settings to operate.",
                "Mode — how claims leave the practice: manually, to a folder, or over HTTP."
            ],
            [
                "Send a test message to yourself to confirm a channel works.",
                "Settings come from configuration and environment variables, not from this page."
            ]),

        ["/recalls"] = new(
            "Recalls",
            "Patients due or overdue for review, most overdue first. A recall stays on this list until an appointment is booked for it.",
            [
                "Due date — calculated from the interval set on the patient's record.",
                "Days over — how long the patient has been overdue."
            ],
            [
                "Contact a patient using the number or address shown.",
                "Book an appointment to take the patient off the list."
            ]),

        ["/exports"] = new(
            "Exports",
            "Data extracts for use outside the system. Every export is recorded in the audit trail, including who ran it and what it covered, because an export takes patient data out of the controls that protect it here.",
            [
                "Format — CSV, Excel, JSON or PDF.",
                "Range — every export is bounded by a date range."
            ],
            [
                "Choose an extract, set the period, and download it."
            ])
    };

    /// <summary>
    /// The topic for a path, matching the longest registered prefix. Returns
    /// null when the page has no help written for it, in which case the panel
    /// says so rather than inventing something.
    /// </summary>
    public static HelpTopic? For(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) path = "/";
        if (!path.StartsWith('/')) path = "/" + path;

        // Trim the query string and any trailing slash.
        var query = path.IndexOf('?');
        if (query >= 0) path = path[..query];
        if (path.Length > 1) path = path.TrimEnd('/');

        if (Topics.TryGetValue(path, out var exact)) return exact;

        return Topics
            .Where(t => t.Key != "/" && path.StartsWith(t.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Key.Length)
            .Select(t => t.Value)
            .FirstOrDefault();
    }

    public static IReadOnlyList<(string Keys, string Does)> Global => GlobalShortcuts;
}
