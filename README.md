# Meridian Dental Surgery

A full practice-management system for a dental surgery: patient records, clinical
charting, the appointment book, treatment planning, surgical and implant records,
billing and insurance, stock and sterilisation, laboratory work, document and
image storage, reporting and exports.

Built as an ASP.NET Core 8 solution with Blazor Server and Entity Framework Core.
Production runs on SQL Server; local development runs on SQLite from a clean
checkout with no external services.

---

## Running it locally

```bash
dotnet run --project src/DentalSurgery.Web
```

The development profile uses SQLite, applies migrations on start-up, and seeds a
demonstration dataset. The first start takes a minute or two; afterwards start-up
is immediate.

Then open <http://localhost:5218>.

To run against SQL Server the way production does:

```bash
docker compose up --build
```

That brings up SQL Server and the application together on
<http://localhost:8080>. See [Deployment](#deployment) for what a real
deployment additionally requires.

**Sign in as** (development only — these accounts exist only when demonstration
data is seeded, and cannot be created outside Development):

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@dentalsurgery.local` | `Dental#2026!` |
| Dentist | `a.okonkwo@meridiandental.example` | `Dental#2026!` |
| Oral surgeon | `r.llewellyn@meridiandental.example` | `Dental#2026!` |
| Hygienist | `f.kelleher@meridiandental.example` | `Dental#2026!` |
| Practice manager | `b.nwosu@meridiandental.example` | `Dental#2026!` |
| Receptionist | `j.barnes@meridiandental.example` | `Dental#2026!` |

Each role sees a different navigation set and a different set of permissions, so
signing in as the receptionist and then as the surgeon is the quickest way to see
how authorisation is applied.

Demonstration data is off by default and is ignored outside Development unless
`Seed:AllowDemoDataOutsideDevelopment` is also set, because these logins share a
password published in this file. Without it you get the reference catalogues and
a single administrator.

To run the tests:

```bash
dotnet test
```

The API is documented at `/api-docs` when running in Development.

Messaging and claim submission are switched off as shipped;
`/admin/integrations` shows what is configured and lets you test each one.

---

## Deployment

### Configuration

`appsettings.json` holds no secrets and no connection string. Everything
sensitive comes from the environment or a secret store, using the standard
double-underscore form:

| Setting | Required | Notes |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | yes | Start-up fails if unset, rather than falling back to a local file. |
| `Database__Provider` | | `SqlServer` (default) or `Sqlite`. |
| `Seed__AdminPassword` | first start | Bootstrap administrator. Required outside Development; the account is created with a forced password change. |
| `Seed__MigrateOnStartup` | | Leave `false` for more than one instance. See below. |
| `DataProtection__CertificateThumbprint` | recommended | Encrypts the key ring at rest. |
| `Notifications__Email__Password` | if email is on | |
| `Notifications__Sms__AuthToken` | if SMS is on | |
| `ClaimSubmission__ApiKey` | if claims post over HTTP | |

Configuration is validated at start-up. A gateway that is switched on but
incomplete, an SMTP connection with no TLS, an `https`-less endpoint, or live
claim submission outside Production all stop the host with one clear message
rather than failing later, per request.

### Schema

Migrations are **not** applied on start-up by default: several instances
starting together would race. Apply them as a release step and let the
application verify:

```bash
DENTAL_MIGRATION_PROVIDER=SqlServer dotnet ef migrations script --idempotent \
  --output migrate.sql \
  --project src/DentalSurgery.Migrations.SqlServer \
  --startup-project src/DentalSurgery.Web
```

The script is idempotent, so it is safe against a database at any prior version.
CI produces it as a build artefact. If the schema is behind the code the
application refuses to start and names the missing migrations.

For a single-instance install, `Seed__MigrateOnStartup=true` is the simpler
choice.

### Running it

```bash
docker build -t dental-surgery .
```

The image runs as a non-root user and exposes `8080`. HTTPS is expected to
terminate at a proxy in front of it; `UseForwardedHeaders` is enabled so the
scheme and client address survive the hop.

Probes:

- `/health/live` — process only. Wire this to liveness.
- `/health/ready` — includes the database. Wire this to readiness.

They are deliberately separate: reporting a database outage as a liveness
failure would restart every instance while the database recovers.

Documents are written to `App_Data` on local disk. That is correct for a single
instance; for more than one, mount shared storage or replace `IFileStorage`
with an object-storage implementation.

### What is still outstanding

**Tenant isolation is not implemented.** The domain model has no tenant concept,
so a multi-tenant deployment would not keep one practice's records away from
another's. This is the blocking item for multi-tenant SaaS and is tracked
separately; single-tenant deployments are unaffected.

---

## What it does

### Patients
Registration with a generated patient number, demographics, contacts and
guardians, GDPR-aware communication preferences, alerts, documents and a full
audit trail. The patient record is tabbed: overview, medical history, dental
chart, periodontal, treatment plans, appointments, procedures, clinical notes,
prescriptions, documents, account, insurance and communications.

### Medical history and risk
Catalogues of systemic conditions, allergens and drugs, each carrying the
clinical implications that matter chairside — antibiotic prophylaxis, bleeding
risk, adrenaline caution, delayed healing, MRONJ risk.

`MedicalRiskAssessor` turns the recorded history into a banner of ranked flags
shown on every clinical screen, and estimates an ASA classification. It reads
anticoagulants, antiresorptives and immunosuppressants out of the drug list by
name and generic, so a warfarin entry raises the bleeding flag whether or not
anyone ticked the box.

### Dental charting
All 52 tooth positions are seeded in FDI notation with Universal and Palmer
cross-references, root and canal counts, and the surfaces each tooth actually
has (palatal above, lingual below).

The odontogram draws each tooth as five clickable surfaces. Findings are recorded
per surface with clinical shorthand (MOD), and the chart is an append-only history
rather than a mutable picture: new findings supersede old ones, so the chart can
be replayed to any past date. DMFT is computed from it.

Charting is validated: you cannot record a surface a tooth does not have, cannot
record caries on a tooth already charted as missing, and cannot record a
surface-based finding without a surface.

### Periodontal
Six sites per tooth, with pocket depth, gingival margin, bleeding, suppuration,
plaque, calculus, mobility and furcation. `PeriodontalAnalyser` computes the
indices, derives the six BPE sextant codes, and suggests a stage and grade under
the 2017 classification, escalated by smoking and diabetes. It sets the recall
interval from the resulting risk.

### Appointment book
A multi-operatory grid with drag-free slot booking. `AvailabilityCalculator`
validates every booking against opening hours, clinic closures, the provider's
rota, approved leave, and existing bookings for the provider, the surgery and the
patient. Conflicts are reported individually rather than as a single failure, and
a clash on the patient cannot be overridden.

Slot search, the waiting-room board with live wait times, the waiting list, and
recall worklists all sit on the same engine.

### Treatment planning
Phased plans with per-item fees, insurance estimates and acceptance tracking.
Presenting a plan starts a 90-day clock; recording the patient's decision rolls
item-level answers up to a plan status. Revising a plan supersedes the original
and carries only outstanding items across, so completed work stays where it
happened.

### Procedures and surgery
Completing a procedure is a single transaction that updates the odontogram, closes
the treatment plan item, links the appointment, consumes stock, rolls the recall
forward and raises the charge. Procedures requiring written consent are blocked
until a valid signed form exists.

Surgical procedures carry an operative record — flap design, bone removal, grafts,
membranes, sutures, specimens, nerve proximity, outcome — plus an anaesthesia
record with per-cartridge dosing, technique and total milligrams. Implants go into
a lot-traceable registry with insertion torque, ISQ, bone quality and restoration
dates.

### Billing
Invoices, payments allocated oldest-first, adjustments, write-offs, refunds,
payment plans with an instalment schedule that absorbs rounding on the last
payment, and an append-only ledger with running balances.

`InsuranceEstimator` applies deductibles before coverage, respects waiting periods
and annual maximums, and coordinates primary with secondary cover so the patient
pays the remainder once. Claims track charged, allowed, paid and patient
responsibility, and settling one posts the payment to the ledger and the
contractual write-off as an adjustment.

Claims submit electronically as ANSI X12 837D (005010X224A2). A claim is
validated before it goes anywhere — missing date of birth, no payer identifier,
no member number and zero-value lines all block, a tooth-specific code without a
tooth warns — and the interchange can be read on screen before it is sent.
Delimiters are stripped from patient data on the way in, so a name containing an
asterisk cannot corrupt the envelope. Three transports are supported: a watched
folder for a clearing-house agent, an HTTP endpoint, and manual, which produces
the file for someone to upload by hand.

### Operations
Stock with lot and expiry tracking, first-expiry-first-out issuing, automatic
reorder drafts and goods-in. Sterilisation cycle logging with chemical, helix,
vacuum and biological indicators; a failed cycle quarantines every set in the
load, and the trace function lists which patients were treated with instruments
from that cycle. Laboratory case tracking with due dates and remake rates.

### Reporting and exports
Dashboard, financial summary, appointment-book analytics, clinical activity,
treatment acceptance, recall worklists and aged receivables.

Twenty-two reports are defined once as a `ReportTable` — columns with types,
rows, headline metrics, totals — and rendered by four exporters, so a report is
written once and comes out as CSV, an `.xlsx` workbook, a PDF on the practice
letterhead, or JSON for another system. `/exports` lists them by category with a
period picker; `GET /api/exports/{key}?format=&from=&to=` is the same thing for a
scheduled job.

Five documents render straight from the record they belong to, on letterhead:
invoice, treatment plan, prescription, claim form and clinical summary. Each is a
button on the record itself rather than a separate export step.

### Documents and imaging
Files attach to the patient record: consent forms, referrals, photographs,
radiographs, correspondence. Uploads are sniffed by magic bytes rather than
trusted by extension, so a renamed executable is refused; content is addressed by
SHA-256, so uploading the same file twice returns the first one instead of
storing it again. Images and PDFs display inline, everything else downloads.

Radiographs pair the image with the exposure record — type, kV, exposure time,
dose in microsieverts, justification, quality grade and findings. The
justification is required: an exposure without a clinical reason is refused at
the API, not merely discouraged in the UI. Deleting an image that a radiograph
record depends on is refused for the same reason.

---

## How it is put together

```
src/
  DentalSurgery.Domain              entities, enums, value objects. No dependencies.
  DentalSurgery.Application         pure logic: risk, perio, charting, availability, money.
  DentalSurgery.Infrastructure      EF Core, Identity, seeding, application services.
  DentalSurgery.Migrations.SqlServer  production migrations.
  DentalSurgery.Migrations.Sqlite     development migrations.
  DentalSurgery.Web                 Blazor Server UI and the REST API.
tests/
  DentalSurgery.Tests               231 tests over the Application, claim, import
                                    and configuration-security logic.
```

Migrations live in their own projects per provider because EF discovers every
migration in a migrations assembly regardless of namespace, so two providers
cannot share one.

The decision logic lives in `Application` as plain classes with no persistence
dependency, which is why it is directly testable. `Infrastructure` services
orchestrate the database around it.

### Persistence notes

**Auditing.** A `SaveChanges` interceptor stamps audit metadata, converts deletes
into soft deletes, refreshes the concurrency token, and writes an `AuditLog` row
for every change. The audit rows join the same `SaveChanges` call, so the trail
and the change it describes commit or roll back together.

**Decimals.** Money is `decimal(18,4)` on SQL Server, stored exactly. SQLite has
no decimal type — left to itself EF stores decimals as TEXT, which makes `SUM`
untranslatable and sorts money lexicographically, "9.00" above "10.00" — so on
SQLite only, money goes through a value converter to REAL, rounded to four
places on read. The model applies whichever is right for the configured
provider.

**Concurrency.** On SQL Server `RowVersion` is a native `rowversion`, stamped by
the database on every write, so two hosts updating one row cannot both believe
they won. SQLite has no such type, so there the interceptor regenerates the
token on each save — weaker, since the read-modify-write is not atomic, but
sufficient for the single-process development SQLite is kept for. The
interceptor detects which case it is in and leaves a database-generated column
alone.

**DbContext lifetime.** Blazor Server components can start overlapping queries
within one circuit, and a `DbContext` is not re-entrant. UI components take
`IDbContextFactory<DentalDbContext>` and create a short-lived context; application
services take the scoped instance.

**Cascade behaviour.** Cascade delete is off by default and re-enabled only where
a genuine composition exists (a plan owns its phases, an invoice owns its lines).
Clinical history does not disappear because a parent row was removed.

---

## Integrations

Everything below is configured in `appsettings.json` and testable from
`/admin/integrations`, which shows what is switched on without ever displaying a
credential. All of it is off by default, and the application says so plainly
rather than pretending to have sent something.

**Messaging.** `INotificationSender` has an SMTP implementation (MailKit) and an
HTTP SMS implementation shaped for Twilio-compatible providers. A hosted
`ReminderDispatcher` wakes on an interval, takes the reminders whose send time has
arrived, respects the patient's channel preferences, retries, abandons anything
too stale to send honestly, and writes a communications-log entry either way.
With no gateway configured, messages are recorded rather than sent and the
reminder is marked as such, so the front desk knows to phone instead.

**Claim submission.** File-drop, HTTP and manual gateways behind
`IClaimSubmissionGateway`; the interchange identifiers come from the clearing
house's companion guide. `UsageIndicator` distinguishes test from production.

**Fee import.** A price list arrives as CSV — `Code` and `Fee` required,
`Allowed`, `Coverage` and `Description` optional, column order irrelevant. The
preview shows every row classified as an addition, an update or unchanged, with
rejects and their reasons, and writes nothing until it is applied. Exporting the
template gives back the current schedule in the shape the importer accepts, so
the round trip is the normal way to reprice.

**File storage.** `IFileStorage` is implemented against the local disk under
`App_Data`. Swapping in S3 or Azure Blob is a single class.

---

## Deliberate limitations

- **No DICOM.** Radiographs are stored as ordinary image files with a full
  exposure record. There is no DICOM parsing, no sensor driver and no
  measurement or enhancement tooling; this is a record of imaging, not a viewer.
- **Fees ship indicative.** The 130-code catalogue uses CDT-style codes and
  plausible UK private fees. A practice imports its own price list on install —
  which is now a CSV import rather than 130 rows of typing.
- **One clearing house shape.** The 837D follows 005010X224A2, but every trading
  partner has its own companion guide. Expect to adjust the identifier segments
  for a specific payer; the generator is a single class with the envelope in one
  method.
- **The clinical decision support is advisory.** Risk flags, periodontal staging
  and interaction checks assist the clinician; they do not replace judgement, and
  the prescribing check warns rather than blocks except on a recorded allergy.

## Security posture

Passwords require ten characters with mixed case, a digit and a symbol; accounts
lock for fifteen minutes after five failures. Eight roles map to nine
authorisation policies applied at both the page and the API. Signed clinical notes
cannot be edited — corrections are addenda, and a SHA-256 digest of the signed
content makes tampering outside the application detectable. Every change is
audited with before-and-after values, with password hashes, security stamps and
signature data excluded.

Uploads are identified by content, not by file name: the first bytes must match a
format on the allow-list, so a renamed executable is refused whatever its
extension says. Outbound integrations never echo a credential back to the screen,
and claim data is stripped of X12 delimiters before it reaches the interchange.

**Accounts.** Deactivating a login takes effect at once: it is refused at
sign-in, its cookie is rejected on the next request, and an open Blazor circuit
is dropped at the next revalidation. An account flagged for a password change
cannot reach any other page until it has set one.

**Sessions.** The session cookie is `HttpOnly`, `SameSite=Strict`, `Secure`, and
carries the `__Host-` prefix outside Development, so a neighbouring subdomain
cannot overwrite it. Data protection keys are shared through the database rather
than the local profile, so sessions survive a restart and work across instances.

**Throttling.** Account lockout bounds attempts against one account; it does
nothing against one address trying one password across many accounts. Sign-in
submissions and the API are therefore rate limited per client as well. Only
submissions count — rendering the login form is not throttled, so a user cannot
be locked out of the page itself.

**Responses.** A Content-Security-Policy forbids inline script, framing and
cross-origin form posts; patient-bearing responses are marked `no-store` so no
shared cache retains them. The full set is `X-Content-Type-Options`,
`X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`,
`Cross-Origin-Opener-Policy` and CSP.

**Logging.** Structured, one line per request, as JSON outside Development.
Serilog's `RequestPath` excludes the query string, which matters here because
patient search submits names as a query parameter.

**Dependencies.** CI fails the build on any package with a published advisory,
including transitive ones. Four such packages — two rated High — are pinned to
patched versions in `Directory.Build.props`.
