#!/usr/bin/env python3
"""
Emits an lftp script for one stage of a deployment, to be piped into lftp.

The point of generating it rather than writing it inline is the password. A
password passed as a command argument is visible in the process list to anything
else on the machine and is echoed verbatim by any shell tracing; one written into
a file outlives the step that made it. Coming through a pipe it does neither.

Stages, in the order a deployment uses them:

  probe    lists the remote directory and stops. FTP_REMOTE_DIR has to be right
           before anything destructive runs, and on shared hosting nobody can
           tell from the control panel alone whether the account lands in the
           site folder or above it. This turns that into something checked.

  offline  puts app_offline.htm on the server. The ASP.NET Core Module serves
           that page and shuts the application down, which is what releases the
           lock IIS holds on the application's own DLLs. Without it an upload
           over a running site fails part-way and leaves two versions mixed.

  mirror   uploads the published tree. It prunes files an earlier release left
           behind only when FTP_PRUNE is set, because a wrong FTP_REMOTE_DIR plus
           an unconditional --delete is how a deployment removes somebody else's
           site. App_Data is excluded either way: it holds patient documents,
           radiographs and the application's logs, and is never uploaded and
           never deleted.

  online   removes app_offline.htm, which starts the new version.

  logs     downloads the module's stdout capture. When the application fails
           before its own logging exists, IIS answers the browser with a bare
           500 and that file is the only account of why.
"""
from __future__ import annotations

import os
import sys

STAGES = ("probe", "offline", "mirror", "online", "logs")


def require(name: str) -> str:
    value = os.environ.get(name, "").strip()
    if not value:
        raise SystemExit(f"{name} is not set.")
    return value


def quote(value: str) -> str:
    """lftp quoting: it unescapes backslashes inside double quotes."""
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in STAGES:
        raise SystemExit(f"usage: ftp-session.py [{'|'.join(STAGES)}]")

    stage = sys.argv[1]

    host = require("FTP_HOST")
    user = require("FTP_USER")
    password = require("FTP_PASSWORD")

    # No default. A guessed deployment directory is the one mistake here that
    # damages something other than this application.
    #
    # Validated before the trailing slash is stripped, not after: "/" is the
    # server root and a perfectly ordinary answer, but stripping first turns it
    # into "" and the check then rejects it. Paths are built by appending, so
    # the empty string is exactly the right internal form for the root.
    raw = require("FTP_REMOTE_DIR")
    if not raw.startswith("/"):
        raise SystemExit(
            f"FTP_REMOTE_DIR must be an absolute path on the server, for example / or /dental. Got: {raw!r}"
        )
    remote = raw.rstrip("/")

    require_tls = os.environ.get("FTP_REQUIRE_TLS", "true").lower() != "false"
    prune = os.environ.get("FTP_PRUNE", "false").lower() == "true"

    out: list[str] = [
        "set ftp:passive-mode true",
        "set net:max-retries 3",
        "set net:timeout 30",
        "set net:reconnect-interval-base 5",
        "set xfer:clobber on",
        # Shared hosts routinely present a certificate for the hosting provider
        # rather than for the FTP hostname, which fails verification. Encrypting
        # anyway is worth far more than refusing to encrypt at all.
        "set ssl:verify-certificate no",
        # The target is IIS on Windows, which has no Unix permission bits and
        # answers "MFF and SITE CHMOD are not supported by this site". lftp
        # otherwise attempts one per file, and returns a failure at the end of
        # an upload that in fact transferred everything.
        "set ftp:use-site-chmod false",
    ]

    out += (
        ["set ftp:ssl-force true", "set ftp:ssl-protect-data true"]
        if require_tls
        else ["set ftp:ssl-force false"]
    )

    out.append(f"open -u {quote(user)},{quote(password)} {quote(host)}")

    if stage == "probe":
        out += [
            f"cd {quote(remote or '/')}",
            "pwd",
            "cls -l --sort=name",
        ]
    elif stage == "offline":
        # The module will not create this, and the mirror deliberately excludes
        # App_Data, so nothing else ever would. Without it stdout logging is
        # configured and silently writes nowhere - which is the worst outcome,
        # because it looks like the application produced no output at all.
        #
        # -f keeps an existing directory from being an error. This runs in the
        # same session as the put below, so a genuinely broken connection still
        # fails the step.
        out.append(f"mkdir -p -f {quote(remote + '/App_Data/logs')}")
        out.append(f"put app_offline.htm -o {quote(remote + '/app_offline.htm')}")
    elif stage == "mirror":
        publish = require("PUBLISH_DIR")
        flags = [
            "--reverse",
            "--parallel=4",
            "--verbose=1",
            # Belt and braces with ftp:use-site-chmod above: there are no
            # permissions to carry from a Linux runner to IIS, and attempting it
            # is what turns a complete upload into a failed step.
            "--no-perms",
            "--exclude-glob app_offline.htm",
            "--exclude App_Data/",
        ]
        if prune:
            flags.insert(1, "--delete")
        out.append(f"mirror {' '.join(flags)} {quote(publish)} {quote(remote + '/')}")
    elif stage == "logs":
        # When the application fails before its own logging is running, the
        # module's stdout capture is the only account of why, and it is on the
        # server rather than anywhere a CI run can see.
        out.append(f"mirror --verbose=1 {quote(remote + '/App_Data/logs')} logs")
    else:
        # -f so an already-absent file is not an error: a previous run may have
        # removed it, or this deployment may never have taken the site offline.
        out.append(f"rm -f {quote(remote + '/app_offline.htm')}")

    out.append("bye")
    print("\n".join(out))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
