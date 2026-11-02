# Upgrading

## Breaking changes in 3.0

TinyCsv 3.0 has a new reader, based on `Span<char>`, that reads a CSV record instead of a physical line.
It is faster and allocates less memory, and it fixes the parsing of quoted values.
Some behaviours changed, please check them when you upgrade from 2.x:

| Area | 2.x | 3.0 |
|---|---|---|
| Quoted value on multiple lines | split in two broken rows | one row, the new lines are kept in the value |
| Doubled quotes `""` in a quoted value | not always unescaped (`"";x` was read as `;x`) | unescaped to `"` |
| Quote in the middle of an unquoted value (`5" pipe`) | removed | kept |
| `"` and `'` at the start/end of a value | always removed | only the enclosing quotes of a quoted value are removed |
| `TrimData` on a quoted value | trimmed inside the quotes | the spaces inside the quotes are kept |
| Rows starting with `#` (the `Comment` char) with `AllowComment = true` | read as data | skipped. Change `Comment` if your data starts with `#` |
| `EndOfLineDelimiterChar = false` | the last value of the row was lost | the last value is read, a trailing delimiter throws |
| Empty last value (`1;Mario;` with 3 columns) | `null` | empty string |
| Empty line without `SkipRow` | `IndexOutOfRangeException` | a model with default values |
| `SkipRow` index with `RowsToSkip` | inconsistent (and different between sync and async) | the record index, starting from 0 |
| `GetAllLinesAndFields*` | always empty arrays | the values of each row |
| `Save`/`GetAllText` of values with quotes or new lines | not escaped, the file could not be read back | escaped as RFC 4180 (with `AllowBackSlashToEscapeQuote`, quotes and backslashes are escaped with `\`) |
| `TextEncoding` | ignored while reading | used to read and write |
| `[TextEncoding]` attribute | could not be used as attribute | `[TextEncoding("utf-16")]` or `[TextEncoding(1200)]` |
| `DoubleQuotes` option | ignored | used as quote char |
| `LoadFromFile`, `Save` and the other methods with a path | the file was not closed | the file is closed |
| `[Column(name: "...")]` | `KeyNotFoundException` | the name is used for the header, the value is mapped to the property |
| Nullable enum properties | `ArgumentException` | supported |
| Unknown enum value | `null` | the default enum value |
| `ToListAsync()` on .NET 10 | ambiguous with `System.Linq.AsyncEnumerable.ToListAsync` | TinyCsv's extension is not defined on .NET 10, the BCL one is used |

Furthermore:

- added the `net10.0` target;
- `netstandard2.0` and `net462`-`net48` depend on `System.Memory`.

## Renamed in 3.1.0

Two names with a typo have a new name.
The old names still work, but they are obsolete (warning `CS0618`) and they will be removed in 4.0:

| Obsolete | New |
|---|---|
| `GetAlGetAllLinesAndFieldslLinesFromText` | `GetAllLinesAndFieldsFromText` |
| `Handlers.Write.RowWrittin` (`RowWrittinEventArgs<T>`) | `Handlers.Write.RowWritten` (`RowWrittenEventArgs<T>`) |

3.1.0 replaces 3.0.0, which has been unlisted from NuGet.

## Changes in 3.1

3.1 adds `MapColumnsByHeader`, the automatic columns and the `Culture` option.
The `Culture` option fixes how the values are read, please check them when you upgrade:

| Area | 3.0 | 3.1 |
|---|---|---|
| Dates and times without a format (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`) | written with the invariant culture, read with the culture of the machine: on a non US machine a saved file was not read back as it is (`11/02/2026` was read as 11 February) | written and read with `Culture` |
| Columns with a format (i.e. `"dd MMMM yyyy"`, `"N2"`) | written and read with the culture of the machine | written and read with `Culture` |
| `byte` and `sbyte` | written and read with the culture of the machine | written and read with `Culture` |

To read the values with the culture of the machine, as in 3.0, set `options.Culture = CultureInfo.CurrentCulture`.

## Changes in 3.2

3.2 adds the `ValidateValues` and `BadDataFound` options (see [Bad data](bad-data.md)). Furthermore:

| Area | 3.1 | 3.2 |
|---|---|---|
| Exception thrown by a converter while reading (i.e. a custom converter, or an invalid base64 value) | the exception of the converter, without the position | an `InvalidColumnValueException` with the row, the column and the value; the exception of the converter is the `InnerException` |
| Exceptions thrown while reading | without the position | they derive from `CsvException`, with `RowIndex`, `Row`, `ColumnName` and `Value`, and the message ends with the position, i.e. `(row 2, column Id)` |
| Message of `InvalidColumnCountException` | `Invalid column count 2 at 3 line, required 3 columns` | `Invalid column count 2, required 3 columns. (row 2)` |
| Built-in converters of numbers, dates, `bool`, `char` and `Guid` | derive from `DefaultValueConverter` | derive from `ParsableValueConverter<T>`, that derives from `DefaultValueConverter` |

## Changes in 3.4

3.4 includes the [compile-time analyzers](analyzers.md) in the package. Furthermore:

| Area | 3.3 | 3.4 |
|---|---|---|
| A wrong mapping (i.e. `AddColumn(m => m.Name.Length)`, `[Column(converter: typeof(string))]`) | an exception at runtime | an error at compile time (TCSV001, TCSV004, TCSV006) |
| `GetAllText`/`GetAllLines` of a sequence that can be enumerated once (i.e. the result of `LoadFromText`) | only the header was written, the rows were lost | every row is written |

The errors of the analyzers are reported only on mappings that throw at runtime. To restore the previous build, set their severity to `none` in `.editorconfig` (see [Configure the analyzers](analyzers.md#configure-the-analyzers)).
