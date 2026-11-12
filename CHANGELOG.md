# Changelog

All notable changes to the `Mafesoft.TinyCsv*` packages (`Mafe.TinyCsv*` up to 3.5.1) are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.0.1] - 2026-11-12

No changes to the library code: same features as 4.0.0.

### Fixed

- Release workflow: publishing a version that is already on nuget.org no longer fails, and a GitHub Release that
  already exists for the tag gets the packages attached instead of failing.

## [4.0.0] - Unreleased

The `Mafesoft.*` release: the packages and the namespaces have a new name and a few APIs changed.
Read [Migrating to 4.0](README.md#migrating-to-40) before upgrading.

```shell
dotnet add package Mafesoft.TinyCsv
dotnet add package Mafesoft.TinyCsv.AspNetCore
```

### Changed

- **Breaking:** packages and assemblies renamed: `Mafe.TinyCsv` → `Mafesoft.TinyCsv`, `Mafe.TinyCsv.AspNetCore` →
  `Mafesoft.TinyCsv.AspNetCore`; root namespace `TinyCsv` → `Mafesoft.TinyCsv` (and the sub-namespaces).
  Both packages have version 4.0.0. ([#61](https://github.com/fmazzant/TinyCsv/issues/61))
- **Breaking:** target frameworks: `Mafesoft.TinyCsv` netstandard2.0, netstandard2.1, net462, net8.0, net10.0;
  `Mafesoft.TinyCsv.AspNetCore` netstandard2.0, net8.0, net10.0 (net5.0, net6.0, net7.0, net452, net472, net48 removed).
  ([#63](https://github.com/fmazzant/TinyCsv/issues/63))
- **Breaking:** `LoadFromFileAsync`, `LoadFromStreamAsync` and `LoadFromTextAsync` return `IAsyncEnumerable<T>` on every
  target framework (on netstandard2.0 and .NET Framework they returned `Task<IEnumerable<T>>`), and the records are
  streamed. ([#64](https://github.com/fmazzant/TinyCsv/issues/64))
- **Breaking:** ASP.NET Core: no static state and no `BuildServiceProvider`, the registrations belong to the service
  provider; `ITinyCsvFactory` in the `Mafesoft.TinyCsv.AspNetCore` namespace; `InvalidOperationException` /
  `KeyNotFoundException` instead of `ArgumentNullException`; only `Microsoft.Extensions.DependencyInjection.Abstractions`
  is referenced. ([#66](https://github.com/fmazzant/TinyCsv/issues/66))
- **Breaking:** `NewLine` is `"\r\n"` by default on every operating system and it is used by every writer, so the same
  models produce the same bytes on Windows and Linux. ([#67](https://github.com/fmazzant/TinyCsv/issues/67))
- Nullable reference types: the public API is annotated; the `IValueConverter` contract for null values and empty fields
  is documented. ([#68](https://github.com/fmazzant/TinyCsv/issues/68))
- Package icon, per-package readme, license in both packages, SourceLink and symbol packages (`.snupkg`),
  deterministic build; this changelog. ([#69](https://github.com/fmazzant/TinyCsv/issues/69))
- Samples on .NET 10, one file per topic (read and write, class map, records without a model, bad data), attributes and
  minimal API; one solution at the root (`TinyCsv.slnx`) with src, tests, samples and benchmarks; the versions benchmark
  of the old `Mafe.TinyCsv` packages is retired. ([#71](https://github.com/fmazzant/TinyCsv/issues/71))
- Publishing: one `nuget.yml` workflow for both packages, with NuGet Trusted Publishing (OIDC, environment `production`)
  instead of a long-lived API key; the tests run before publishing and a tag creates the GitHub Release.
  ([#85](https://github.com/fmazzant/TinyCsv/issues/85))

### Added

- `SaveAsync(..., IAsyncEnumerable<T>)` and `TinyCsvWriter<T>.WriteRecordsAsync(IAsyncEnumerable<T>)`, to copy a
  source without buffering it; `TinyCsvRecords.LoadFromFileAsync`, `LoadFromStreamAsync` and `LoadFromTextAsync`.
  ([#64](https://github.com/fmazzant/TinyCsv/issues/64))
- `AddTinyCsv<T>(options)` registers `ITinyCsv<T>` as singleton; the named instances are keyed services on .NET 8+;
  `ITinyCsvFactory.TryGet<T>`. ([#66](https://github.com/fmazzant/TinyCsv/issues/66))
- `CsvOptions.DefaultDelimiter` and `CsvOptions.DefaultNewLine`. ([#67](https://github.com/fmazzant/TinyCsv/issues/67))

### Removed

- **Breaking:** the APIs obsolete in 3.x: `Load(string)`, `Load(StreamReader)`, `LoadAsync`,
  `GetAlGetAllLinesAndFieldslLinesFromText`, `RowWrittin` / `RowWrittinEventArgs<T>`, `CsvOptions.SkipEmptyRows`,
  `ICsvDataReader.GetFieldsByLine(string)`. ([#62](https://github.com/fmazzant/TinyCsv/issues/62))
- **Breaking:** the `Mafe.TinyCsv.Extensions` package (obsolete since 2.x): use `Mafesoft.TinyCsv.AspNetCore`.
  ([#65](https://github.com/fmazzant/TinyCsv/issues/65))

## [3.5]

3.5 adds the [delimiter detection](README.md#detect-the-delimiter), the [nested properties](README.md#nested-properties),
the [records without a model](README.md#records-without-a-model), the [writer of one record at a time](README.md#write-one-record-at-a-time)
and the [class map](README.md#class-map). Furthermore:

| Area | 3.4 | 3.5 |
|---|---|---|
| [Automatic columns](README.md#automatic-columns): a property whose type is a class with a public parameterless constructor (i.e. `Address`) | ignored | its properties are columns, i.e. `Address_City` |
| `SaveAsync(Stream, ...)` | the cancellation token was not used | the cancellation token is used |

To keep the columns of 3.4, define the columns or use a [class map](README.md#class-map).

### Examples

The examples use a model without attributes, with a nested address, and its class map:

```c#
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Balance { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

public sealed class CustomerMap : CsvClassMap<Customer>
{
    public CustomerMap()
    {
        Options(o =>
        {
            o.HasHeaderRecord = true;
            o.Delimiter = ";";
            o.DetectDelimiter = true; // the files to read may use another delimiter
        });
        Map(m => m.Id);
        Map(m => m.Name).Name("customer_name");
        Map(m => m.Balance).Format("0.00");
        Map(m => m.Address.Street); // the column Address_Street
        Map(m => m.Address.City);   // the column Address_City
    }
}
```

Convert a large file, whose delimiter is not known, to a file with the semicolon, one record at a time:

```c#
var csv = new TinyCsv<Customer>(new CustomerMap());

await using (var writer = csv.CreateWriter("customers_export.csv"))
{
    await foreach (var customer in csv.LoadFromFileAsync("customers.csv"))
    {
        customer.Name = customer.Name.ToUpperInvariant();
        await writer.WriteRecordAsync(customer);
    }
}
```

The input file, i.e. with the pipe:

```text
Id|customer_name|Balance|Address_Street|Address_City
1|Mario Rossi|10.50|Via Roma 1|Firenze
2|"Bianchi | Luigi"|20.00|Via Milano 2|Milano
```

The output file:

```text
Id;customer_name;Balance;Address_Street;Address_City
1;MARIO ROSSI;10.50;Via Roma 1;Firenze
2;BIANCHI | LUIGI;20.00;Via Milano 2;Milano
```

Write the records of a sequence that is not in memory, on a stream that stays open:

```c#
using (var writer = csv.CreateWriter(stream))
{
    foreach (var customer in GetCustomersFromDatabase())
    {
        writer.WriteRecord(customer);
    }
}
```

Look at a file whose columns are not known, without a model:

```c#
var records = new TinyCsvRecords(options => options.DetectDelimiter = true);

foreach (var record in records.LoadDynamicFromFile("customers.csv"))
{
    Console.WriteLine($"{record.customer_name} lives in {record.Address_City}");
}

DataTable table = records.LoadDataTableFromFile("customers.csv");
Console.WriteLine($"{table.Columns.Count} columns, {table.Rows.Count} rows");
```

These examples, and others, are in [CsvSampleConsoleApp](samples/CsvSampleConsoleApp).

## [3.4]

| Area | 3.3 | 3.4 |
|---|---|---|
| `GetAllText`/`GetAllLines` of a sequence that can be enumerated once (i.e. the result of `LoadFromText`) | only the header was written, the rows were lost | every row is written |

## [3.2]

3.2 adds the `ValidateValues` and `BadDataFound` options (see [Bad data](README.md#bad-data)). Furthermore:

| Area | 3.1 | 3.2 |
|---|---|---|
| Exception thrown by a converter while reading (i.e. a custom converter, or an invalid base64 value) | the exception of the converter, without the position | an `InvalidColumnValueException` with the row, the column and the value; the exception of the converter is the `InnerException` |
| Exceptions thrown while reading | without the position | they derive from `CsvException`, with `RowIndex`, `Row`, `ColumnName` and `Value`, and the message ends with the position, i.e. `(row 2, column Id)` |
| Message of `InvalidColumnCountException` | `Invalid column count 2 at 3 line, required 3 columns` | `Invalid column count 2, required 3 columns. (row 2)` |
| Built-in converters of numbers, dates, `bool`, `char` and `Guid` | derive from `DefaultValueConverter` | derive from `ParsableValueConverter<T>`, that derives from `DefaultValueConverter` |

## [3.1]

3.1 adds `MapColumnsByHeader`, the automatic columns and the `Culture` option.
The `Culture` option fixes how the values are read, please check them when you upgrade:

| Area | 3.0 | 3.1 |
|---|---|---|
| Dates and times without a format (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`) | written with the invariant culture, read with the culture of the machine: on a non US machine a saved file was not read back as it is (`11/02/2026` was read as 11 February) | written and read with `Culture` |
| Columns with a format (i.e. `"dd MMMM yyyy"`, `"N2"`) | written and read with the culture of the machine | written and read with `Culture` |
| `byte` and `sbyte` | written and read with the culture of the machine | written and read with `Culture` |

To read the values with the culture of the machine, as in 3.0, set `options.Culture = CultureInfo.CurrentCulture`.

### Renamed in 3.1.0

Two names with a typo have a new name.
The old names were obsolete (warning `CS0618`) in 3.x and have been removed in 4.0:

| Obsolete | New |
|---|---|
| `GetAlGetAllLinesAndFieldslLinesFromText` | `GetAllLinesAndFieldsFromText` |
| `Handlers.Write.RowWrittin` (`RowWrittinEventArgs<T>`) | `Handlers.Write.RowWritten` (`RowWrittenEventArgs<T>`) |

3.1.0 replaces 3.0.0, which has been unlisted from NuGet.

## [3.0]

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


The versions before 3.0 are listed in the [GitHub releases](https://github.com/fmazzant/TinyCsv/releases) and on [NuGet](https://www.nuget.org/packages/Mafe.TinyCsv#versions-body-tab).
