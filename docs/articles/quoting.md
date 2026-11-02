# Quoting and multi-line values

TinyCsv follows [RFC 4180](https://www.rfc-editor.org/rfc/rfc4180):

- a value is quoted (`"..."`) when it contains the delimiter, a quote or a new line;
- a quote inside a quoted value is escaped by doubling it (`""`);
- a quoted value can span multiple lines: the whole record is **one row**, for `RowsToSkip`, `SkipRow`, the events and `GetAllLines*`.

```csv
Id;Name;Note
1;"Rossi; Mario";"He said ""hello"""
2;Luigi;"first line
second line"
```

The file above contains 2 rows:

| Id | Name | Note |
|---|---|---|
| 1 | `Rossi; Mario` | `He said "hello"` |
| 2 | `Luigi` | `first line` + new line + `second line` |

`Save` escapes the values in the same way, so a saved file is always read back as it is.

## Options

| Option | Description |
|---|---|
| `DoubleQuotes` | the quote char, `"` by default |
| `AllowRowEnclosedInDoubleQuotesValues` | when `false`, a new line always ends the row |
| `AllowBackSlashToEscapeQuote` | `\"` is also a quote inside a quoted value; `Save` escapes quotes and backslashes with `\` |
| `TrimData` | removes the spaces around a value; the spaces inside the quotes are kept |

A quote in the middle of an unquoted value (`5" pipe`) is kept as it is.
