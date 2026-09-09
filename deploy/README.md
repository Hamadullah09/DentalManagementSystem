# Deploying to Windows shared hosting (myASP)

This repository is **public**. Nothing here names the hosting account, the FTP
host, the database server or any login. Those live in repository secrets, which
are not readable from a fork, a clone or the Actions log. Keep it that way: a
connection string pasted into a file here is a connection string published.

The whole deployment is `.github/workflows/deploy.yml`, run by hand from the
Actions tab. It is never triggered by a push — a merge should not reach a system
holding patient records on its own.

## What you set once

### Repository secrets — Settings → Secrets and variables → Actions → Secrets

| Secret | What it is |
|---|---|
| `FTP_SERVER` | The FTP host from the hosting control panel. Host name only, no `ftp://`. |
| `FTP_USERNAME` | The FTP account. |
| `FTP_PASSWORD` | Its password. |
| `DATABASE_CONNECTION_STRING` | The full SQL Server connection string, including the password. |
| `SEED_ADMIN_EMAIL` | The address of the first administrator. |
| `SEED_ACCOUNTS_JSON` | The logins to create on first start. See below. Optional; without it only the bootstrap administrator is made, on a random password written once to the log. |

`SEED_ACCOUNTS_JSON` is a JSON array. `Email`, `Password` and `Role` are
required; the rest are optional. `StaffNumber` attaches the login to an existing
staff record so the person signs in to their own diary rather than an empty one.

```json
[
  { "Email": "…", "Password": "…", "Role": "Administrator" },
  { "Email": "…", "Password": "…", "Role": "Dentist", "StaffNumber": "S-0001" }
]
```

Roles are the names in `Roles`: `Administrator`, `PracticeManager`, `Dentist`,
`OralSurgeon`, `Hygienist`, `Nurse`, `Receptionist`, `Accounts`.

These passwords are **bootstrap values**, not standing credentials — you typed
them into a settings page, so they are known to whoever can read that page.
Accounts are therefore created owing a password change, and the person picks
their own at first sign-in. Set the `SEED_MUST_CHANGE_PASSWORD` variable to
`false` only if you accept that the password in this secret stays live.

Seeding is idempotent **by email**: an account that already exists is left
alone. Changing a password here after the account exists changes nothing on the
server, by design, so a restart can never reset a password someone has since
chosen. Change it in the application instead.

### Repository variables — the same page, Variables tab

| Variable | Default | What it is |
|---|---|---|
| `FTP_REMOTE_DIR` | *(none — required)* | Absolute path on the FTP server to the folder IIS serves. Find it with `probe`, below. |
| `SITE_URL` | `https://dental.sma-techno.net` | Where the deployment checks that the site came back. |
| `SITE_HOST` | `dental.sma-techno.net` | Written to `AllowedHosts`, so the application answers for its own name and not for whatever a `Host` header claims. |
| `FTP_REQUIRE_TLS` | `true` | Encrypts the control and data connections. Turning it off sends the deployment password across the internet in clear text. |
| `SEED_MUST_CHANGE_PASSWORD` | `true` | Whether seeded accounts must choose their own password at first sign-in. |
| `MIGRATE_ON_STARTUP` | `false` | Fallback for when the runner cannot reach the database server. See below. |
| `PRACTICE_NAME`, `PRACTICE_SLUG` | built-in defaults | The first tenant's name and slug. |

## Running it

Actions → **Deploy** → *Run workflow*, then pick a mode.

**1. `probe` — first, and any time the hosting layout changes.**
Connects and lists `FTP_REMOTE_DIR`, and touches nothing. Shared hosting does
not say whether the FTP account lands *inside* the site folder or above it, and
guessing wrong means a later release uploads a site into the wrong directory —
or, with pruning on, deletes one. Start with `/`, read the listing, and point
the variable at the folder that already holds the site.

**2. `build-only` — a rehearsal.**
Builds, tests, applies no migrations, uploads nothing. Confirms the tree
compiles, the suite passes and `web.config` came out with every setting.

**3. `deploy` — the release.** In order:

1. refuses to start if a secret is missing, before anything is touched
2. builds with warnings as errors, and runs the test suite
3. generates the idempotent migration script, keeps it as a run artifact, and
   applies it to the database
4. publishes, drops native libraries for platforms this host will never be, and
   writes the settings into `web.config`
5. uploads `app_offline.htm`, which stops the application and releases the lock
   IIS holds on its own DLLs — without this an upload over a running site fails
   half-way and leaves two versions mixed
6. mirrors the published tree, **excluding `App_Data`** in both directions:
   that folder holds patient documents, radiographs and logs, and is never
   uploaded and never deleted
7. removes `app_offline.htm`, which starts the new version
8. polls `/health/ready` until it answers 200 — the probe only answers once the
   application has started *and* reached its database, so a green run means the
   thing actually works, not merely that an upload finished

Leave **prune** off for the first release. It turns on `--delete`, which is what
removes files an old version left behind; it is worth having, but not before one
deployment has proved the directory is the right one.

## When it goes wrong

**The site shows the maintenance page.** A release failed after step 5. Delete
`app_offline.htm` from the site folder over FTP and the previous version returns
immediately.

**`/health/ready` never answers.** Start-up failed. Set `stdoutLogEnabled="true"`
in `web.config` on the server, request the site once, and read
`App_Data/logs/stdout*.log`. The usual causes are a connection string that does
not reach the database and a pending migration.

**The migration step cannot reach the database.** Some hosts refuse SQL
connections from outside their own network, which no amount of retrying fixes.
Set the `MIGRATE_ON_STARTUP` variable to `true` and re-run with
`apply_migrations` off: the application then migrates itself as it starts. That
is safe here because this is a single instance — on several hosts they would
race — and it means the first request after a schema change waits for the
migration.

**Sign-in appears to work but bounces back to the login page.** The session
cookie is `__Host-` prefixed, which a browser refuses to store over plain HTTP.
The site needs a working certificate before anyone can sign in. This is a real
protection, not an obstacle to route around: without HTTPS the session cookie
that opens every patient record would cross the network in clear.
