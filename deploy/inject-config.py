#!/usr/bin/env python3
"""
Writes deployment settings into the published web.config's <environmentVariables>.

The settings arrive as a JSON object on stdin, never as command-line arguments:
an argument list is readable by any other process on the machine and is echoed
into CI logs on failure. Nothing is printed but the names.

Two kinds of key are treated specially.

"SEED_ACCOUNTS_JSON" holds an array of accounts and is expanded into the indexed
keys the configuration binder expects (Seed__Accounts__0__Email and so on), so a
deployment carries a single secret rather than six passwords spread across six.

A key beginning "@" sets an attribute on the <aspNetCore> element rather than an
environment variable - "@stdoutLogEnabled" being the one that matters, because
when the application fails before logging is running, that file is the only
account of why. It is off by default: it records every line the process writes,
and grows without bound on a host nobody is watching.
"""
from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET

ACCOUNT_FIELDS = {
    "Email": "Email",
    "Password": "Password",
    "Role": "Role",
    "FirstName": "FirstName",
    "LastName": "LastName",
    "StaffNumber": "StaffNumber",
    "MustChangePassword": "MustChangePassword",
}


def expand_accounts(raw: str, must_change_default: bool) -> dict[str, str]:
    """Turns the accounts array into Seed__Accounts__N__Field keys."""
    accounts = json.loads(raw)
    if not isinstance(accounts, list):
        raise SystemExit("SEED_ACCOUNTS_JSON must be a JSON array of account objects.")

    expanded: dict[str, str] = {}
    for index, account in enumerate(accounts):
        if not isinstance(account, dict):
            raise SystemExit(f"SEED_ACCOUNTS_JSON entry {index} is not an object.")

        for required in ("Email", "Password", "Role"):
            if not account.get(required):
                raise SystemExit(f"SEED_ACCOUNTS_JSON entry {index} has no {required}.")

        # Absent means "use the deployment default", which is to force a change.
        account.setdefault("MustChangePassword", must_change_default)

        for field, key in ACCOUNT_FIELDS.items():
            if field not in account or account[field] is None:
                continue
            value = account[field]
            if isinstance(value, bool):
                value = "true" if value else "false"
            expanded[f"Seed__Accounts__{index}__{key}"] = str(value)

    return expanded


def main() -> int:
    if len(sys.argv) != 2:
        raise SystemExit("usage: inject-config.py <path to web.config>")

    path = sys.argv[1]
    settings = json.load(sys.stdin)

    must_change = str(settings.pop("SEED_MUST_CHANGE_PASSWORD", "true")).lower() != "false"

    accounts_raw = settings.pop("SEED_ACCOUNTS_JSON", "").strip()
    if accounts_raw:
        settings.update(expand_accounts(accounts_raw, must_change))

    # An empty value is a value the operator did not supply. Writing it would
    # overwrite a good default with nothing, so it is dropped instead.
    settings = {k: v for k, v in settings.items() if str(v).strip()}

    tree = ET.parse(path)
    root = tree.getroot()

    node = root.find("./location/system.webServer/aspNetCore")
    if node is None:
        node = root.find("./system.webServer/aspNetCore")
    if node is None:
        raise SystemExit(f"{path} has no <aspNetCore> element to configure.")

    # Attributes on <aspNetCore> itself, before the environment variables.
    attributes = {k[1:]: v for k, v in settings.items() if k.startswith("@")}
    settings = {k: v for k, v in settings.items() if not k.startswith("@")}

    for name, value in sorted(attributes.items()):
        node.set(name, str(value))

    variables = node.find("environmentVariables")
    if variables is None:
        variables = ET.SubElement(node, "environmentVariables")

    existing = {e.get("name"): e for e in variables.findall("environmentVariable")}

    for name, value in sorted(settings.items()):
        if name in existing:
            existing[name].set("value", str(value))
        else:
            ET.SubElement(variables, "environmentVariable", {"name": name, "value": str(value)})

    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)

    # Names only. A value here is a password or a connection string.
    print(f"Configured {len(settings)} environment variable(s) in {path}:")
    for name in sorted(settings):
        print(f"  {name}")

    if attributes:
        # Attribute values are switches, not secrets, so they are worth showing.
        print("aspNetCore attributes:")
        for name, value in sorted(attributes.items()):
            print(f"  {name} = {value}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
