#!/usr/bin/env python3
"""
Checks the deployment's connection string and says what is wrong with it.

Read from the environment, never an argument, and only key names are ever
printed - values are replaced by their length. A connection string carries the
database password, and a diagnostic that helps by printing it is not a help.

This exists because the raw failure is unreadable. A missing space in "User Id"
surfaces from the driver as:

    System.ArgumentException: Keyword not supported: 'userid'.

with no mention of which setting, which deployment, or what to type instead -
and it arrives after the build and the test suite have already run.
"""
from __future__ import annotations

import os
import sys

# What Microsoft.Data.SqlClient accepts, lower-cased, spaces intact.
KNOWN = {
    "data source", "server", "address", "addr", "network address",
    "initial catalog", "database",
    "user id", "uid", "password", "pwd",
    "integrated security", "trusted_connection",
    "encrypt", "trustservercertificate", "hostnameincertificate",
    "connect timeout", "connection timeout", "command timeout",
    "multipleactiveresultsets", "multisubnetfailover", "applicationintent",
    "application name", "app", "workstation id", "wsid",
    "persist security info", "pooling", "max pool size", "min pool size",
    "connection lifetime", "load balance timeout", "packet size",
    "authentication", "column encryption setting", "enclave attestation url",
    "attestation protocol", "ip address preference", "failover partner",
    "current language", "language", "replication", "transaction binding",
    "type system version", "context connection", "connectretrycount",
    "connectretryinterval", "poolblockingperiod", "user instance",
    "attachdbfilename", "extended properties", "initial file name",
}

# The mistakes a person actually makes typing one of these by hand.
SUGGESTIONS = {
    "userid": "User Id",
    "user": "User Id",
    "username": "User Id",
    "user name": "User Id",
    "initialcatalog": "Initial Catalog",
    "datasource": "Data Source",
    "connecttimeout": "Connect Timeout",
    "connectiontimeout": "Connection Timeout",
    "trustservercertificates": "TrustServerCertificate",
    "multipleactiveresultset": "MultipleActiveResultSets",
    "applicationname": "Application Name",
    "persistsecurityinfo": "Persist Security Info",
    "integratedsecurity": "Integrated Security",
}


def split_pairs(value: str) -> list[tuple[str, str]]:
    """Splits on ';', honouring a value wrapped in single or double quotes."""
    pairs: list[tuple[str, str]] = []
    token, quote = "", ""

    for char in value:
        if quote:
            token += char
            if char == quote:
                quote = ""
        elif char in "'\"":
            token += char
            quote = char
        elif char == ";":
            if token.strip():
                pairs.append(split_one(token))
            token = ""
        else:
            token += char

    if token.strip():
        pairs.append(split_one(token))

    return pairs


def split_one(token: str) -> tuple[str, str]:
    key, sep, value = token.partition("=")
    return (key.strip(), value.strip() if sep else "")


def main() -> int:
    raw = os.environ.get("CONNECTION_STRING", "")

    if not raw.strip():
        print("::error::DATABASE_CONNECTION_STRING is empty.")
        return 1

    if raw != raw.strip():
        print("::warning::The connection string has leading or trailing whitespace. "
              "GitHub keeps a trailing newline if one was pasted in.")

    pairs = split_pairs(raw)
    problems: list[str] = []

    print("Connection string settings (values hidden):")
    for key, value in pairs:
        marker = "ok " if key.lower() in KNOWN else "BAD"
        print(f"  [{marker}] {key} = <{len(value)} characters>")

        if key.lower() in KNOWN:
            continue

        fix = SUGGESTIONS.get(key.lower())
        problems.append(
            f"'{key}' is not a setting Microsoft.Data.SqlClient understands."
            + (f" Write it as '{fix}'." if fix else "")
        )

    keys = {k.lower() for k, _ in pairs}

    if not keys & {"server", "data source", "address", "addr", "network address"}:
        problems.append("There is no Server (or Data Source) setting.")

    if not keys & {"database", "initial catalog"}:
        problems.append("There is no Database (or Initial Catalog) setting.")

    authenticates = bool(keys & {"password", "pwd"}) and bool(keys & {"user id", "uid"})
    trusted = bool(keys & {"integrated security", "trusted_connection", "authentication"})

    if not authenticates and not trusted:
        problems.append(
            "There is no username and password. Shared hosting has no Windows identity "
            "to fall back on, so the connection would be refused."
        )

    # Not fatal, but worth saying once rather than debugging twice.
    if "encrypt" in keys and "trustservercertificate" not in keys:
        print("::warning::Encrypt is set without TrustServerCertificate. Shared hosts often "
              "present a certificate that does not match the server name, and the connection "
              "is then refused at the TLS handshake.")

    if not problems:
        print(f"The connection string parses and has all {len(pairs)} required settings.")
        return 0

    print()
    for problem in problems:
        print(f"::error::{problem}")

    print()
    print("Fix the DATABASE_CONNECTION_STRING secret and run this again. The shape is:")
    print("  Server=<host>;Database=<name>;User Id=<login>;Password=<password>;"
          "Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
