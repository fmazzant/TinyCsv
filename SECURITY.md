# Security Policy

## Supported Versions

| Version | Supported |
| ------- | --------- |
| 4.x (`Mafesoft.TinyCsv*`) | :white_check_mark: |
| < 4.0 (`Mafe.TinyCsv*`) | :x: |

The `Mafe.TinyCsv*` packages are deprecated: migrate to `Mafesoft.TinyCsv*`
(see [Migrating to 4.0](README.md#migrating-to-40)). `Mafe.TinyCsv.Extensions` is retired, use `Mafesoft.TinyCsv.AspNetCore`.

## Reporting a Vulnerability

Please **do not open a public issue** for a security problem.

Report it privately with [GitHub private vulnerability reporting](https://github.com/fmazzant/TinyCsv/security/advisories/new)
(**Security** tab → **Report a vulnerability**), describing:

- the affected package and version;
- how to reproduce the problem;
- the impact you expect.

You will receive an answer as soon as possible. When the problem is confirmed, a fixed version is released and
the advisory is published with credit to the reporter, unless you prefer to stay anonymous.

## CSV injection

A CSV file opened in Excel or LibreOffice runs as a formula a value that starts with `=`, `+`, `-`, `@`, tab or carriage return
([CSV injection](https://owasp.org/www-community/attacks/CSV_Injection)). TinyCsv writes the values as they are by default (`InjectionProtection = None` in 4.x),
so an export of data entered by users can carry a formula to whoever opens the file.

When the values come from users, set `InjectionProtection` when writing:

- `CsvInjectionProtection.Escape`: the value is prefixed with an apostrophe and enclosed in quotes, so it is shown as text;
- `CsvInjectionProtection.Strip`: the leading injection characters are removed;
- `CsvInjectionProtection.Throw`: a `CsvInjectionException` is thrown, with the row and the column of the value.

Only the text values (the columns of type `string`) are checked: the numbers are written as they are.
See [Protect against CSV injection](README.md#protect-against-csv-injection) in the README.
