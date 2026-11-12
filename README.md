
# TinyCsv

[![NuGet](https://img.shields.io/nuget/v/Mafesoft.TinyCsv?style=flat-square&label=nuget)](https://www.nuget.org/packages/Mafesoft.TinyCsv)
[![Downloads](https://img.shields.io/nuget/dt/Mafesoft.TinyCsv?style=flat-square&label=downloads)](https://www.nuget.org/packages/Mafesoft.TinyCsv)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE.md)

[![Mafe.TinyCsv downloads](https://img.shields.io/nuget/dt/Mafe.TinyCsv?style=flat-square&label=Mafe.TinyCsv%20downloads)](https://www.nuget.org/packages/Mafe.TinyCsv)

TinyCsv is a .NET library to read and write CSV data in an easy way.

To use it in your project, add the Mafesoft.TinyCsv NuGet package to your project:

```shell
dotnet add package Mafesoft.TinyCsv
```

```c#
using Mafesoft.TinyCsv;
```

> Since 4.0.0 the packages are published as `Mafesoft.TinyCsv` and `Mafesoft.TinyCsv.AspNetCore` (previously `Mafe.TinyCsv` and `Mafe.TinyCsv.AspNetCore`)
> and every namespace starts with `Mafesoft.`: `using TinyCsv;` becomes `using Mafesoft.TinyCsv;`, `using TinyCsv.Attributes;` becomes `using Mafesoft.TinyCsv.Attributes;` and so on.

Coming from CsvHelper? See the [Migrating from CsvHelper](https://github.com/fmazzant/TinyCsv/blob/main/docs/migrating-from-csvhelper.md) guide.

It is available an AspNetCore extension:

[![NuGet](https://img.shields.io/nuget/v/Mafesoft.TinyCsv.AspNetCore?style=flat-square&label=nuget)](https://www.nuget.org/packages/Mafesoft.TinyCsv.AspNetCore)
[![Downloads](https://img.shields.io/nuget/dt/Mafesoft.TinyCsv.AspNetCore?style=flat-square&label=downloads)](https://www.nuget.org/packages/Mafesoft.TinyCsv.AspNetCore)

Define the model that you want to use, like this:

```c#
public class Model
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public string TextBase64 { get; set; }
}
```

Now, it is necessary to define the TinyCSV options, like this:

```c#
var csv = new TinyCsv<Model>(options =>
{
    // Options
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.RowsToSkip = 0;
    options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
    options.TrimData = true;
    options.AllowRowEnclosedInDoubleQuotesValues = true;
    options.EnableHandlers = false;
    options.ValidateColumnCount = true;

    // Columns
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
    options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.TextBase64, new Base64Converter());
});
```

The options define that the file has the header in the first row and the delimiter is ";" (the default), furthermore some columns are defined.
RowsToSkip and SkipRow are used to skip the first rows of the file.
When `SkipRow` is not set and the handlers are not enabled, the records are read without creating a string for each record, so the read is faster and allocates less memory.
TrimData is used to remove the white spaces from the data.

The rows starting with the `Comment` char (`#` by default) are skipped when `AllowComment` is true (default),
while `AllowComment = false` throws a `NotAllowCommentException`.
The `TextEncoding` option (UTF-8 by default) is used to read and write files, streams and text; a byte order mark, if present, takes precedence while reading.

### Defaults

| Option | Default | Why |
|---|---|---|
| `Delimiter` | `";"` | the delimiter of Excel with the Italian and most European regional settings. Set `","` for RFC 4180 files, or `DetectDelimiter = true` to detect it when reading (it is opt-in) |
| `NewLine` | `"\r\n"` | RFC 4180. It is the same on every operating system, so the same models are written with the same bytes on Windows and Linux. When reading, `\r\n`, `\n` and `\r` are all accepted |
| `TextEncoding` | UTF-8, written **without** the byte order mark (BOM) | most tools do not want the BOM. Excel needs it to recognize UTF-8: set `options.TextEncoding = new UTF8Encoding(true)` |
| `HasHeaderRecord` | `false` | |

## Culture

The `Culture` option is used to read and write numbers, dates and the columns with a format.
The default is `CultureInfo.InvariantCulture`, so a file is written and read in the same way on every machine:

```c#
var csv = new TinyCsv<Model>(options =>
{
    options.Culture = new CultureInfo("it-IT"); // 1234,5 and 02/11/2026
    // ...
});
```

With attributes, use `[Culture("it-IT")]` on the model.
A column with its own format provider (`AddColumn(..., IFormatProvider)` or `[Column(formatProvider: ...)]`) uses that provider.

## Quoted values and multi-line records

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

The file above contains 2 rows. `Save` escapes the values in the same way, so a saved file is always read back as it is.

## Migrating to 4.0

4.0.0 is a breaking release.

### Packages and namespaces

| 3.x | 4.0 |
|---|---|
| `Mafe.TinyCsv` | `Mafesoft.TinyCsv` |
| `Mafe.TinyCsv.AspNetCore` | `Mafesoft.TinyCsv.AspNetCore` |
| `using TinyCsv;` (and `TinyCsv.*`) | `using Mafesoft.TinyCsv;` (and `Mafesoft.TinyCsv.*`) |
| `Mafe.TinyCsv.Extensions` | retired: use `Mafesoft.TinyCsv.AspNetCore` |

`Mafe.TinyCsv.Extensions` was an old copy of the AspNetCore package, with every type obsolete (compile error) since 2.x:
it is no longer built nor published, and there is no `Mafesoft.TinyCsv.Extensions` package.

### Removed APIs

The APIs marked obsolete in 3.x have been removed:

| Removed | Use instead |
|---|---|
| `Load(string path)` | `LoadFromFile(path)` |
| `Load(StreamReader reader)` | `LoadFromStream(reader)` |
| `LoadAsync(string path)`, `LoadAsync(StreamReader reader)` | `LoadFromFileAsync(path)`, `LoadFromStreamAsync(reader)` |
| `GetAlGetAllLinesAndFieldslLinesFromText` | `GetAllLinesAndFieldsFromText` |
| `Handlers.Write.RowWrittin` (`RowWrittinEventArgs<T>`) | `Handlers.Write.RowWritten` (`RowWrittenEventArgs<T>`) |
| `CsvOptions.SkipEmptyRows = true` | `SkipRow = (row, index) => string.IsNullOrWhiteSpace(row)` |
| `ICsvDataReader.GetFieldsByLine(string line)` | `GetFieldsByLine(line, columnsCount)` |

### Target frameworks

| Package | 3.x | 4.0 |
|---|---|---|
| `Mafesoft.TinyCsv` | net5.0, net6.0, net7.0, net8.0, net10.0, netstandard2.0, netstandard2.1, net452, net462, net472, net48 | netstandard2.0, netstandard2.1, net462, net8.0, net10.0 |
| `Mafesoft.TinyCsv.AspNetCore` | netstandard2.0, netstandard2.1, net5.0, net6.0, net7.0, net8.0, net10.0 | netstandard2.0, net8.0, net10.0 |

- .NET 5, 6 and 7 are out of support: the applications on these versions use the `netstandard2.1` (`Mafesoft.TinyCsv`) or `netstandard2.0` (`Mafesoft.TinyCsv.AspNetCore`) asset.
- .NET Framework 4.5.2 is no longer supported; .NET Framework 4.7.2 and 4.8 use the `net462` asset.
- `System.Memory` is referenced only by `netstandard2.0` and `net462`.

### Async API

On `netstandard2.0` and .NET Framework, `LoadFromFileAsync`, `LoadFromStreamAsync` and `LoadFromTextAsync` returned a
`Task<IEnumerable<T>>` with the whole file in memory. In 4.0 they return an `IAsyncEnumerable<T>` on every target framework
(`Microsoft.Bcl.AsyncInterfaces` is referenced by `netstandard2.0` and `net462`), so the same code compiles everywhere:

```c#
// 3.x on .NET Framework / netstandard2.0
var models = await csv.LoadFromFileAsync(path);

// 4.0 on every target framework
await foreach (var model in csv.LoadFromFileAsync(path)) { }
// or
var models = await csv.LoadFromFileAsync(path).ToListAsync(); // using Mafesoft.TinyCsv.Extensions;
```

`TinyCsvWriter<T>` implements `IAsyncDisposable` on every target framework.

### Defaults

- `NewLine` is `"\r\n"` on every operating system (before: `Environment.NewLine`, so `"\n"` on Linux and macOS),
  and it is used by `Save`, `SaveAsync`, `TinyCsvWriter<T>` and `CsvDataWriter<T>` too (before: only by `GetAllText`;
  the writers used the `NewLine` of the `TextWriter`). Set `options.NewLine = "\n"` to write the previous files on Linux.
  Reading is not affected: `\r\n`, `\n` and `\r` are all accepted.
- `NewLineAttribute()` without a value is `"\r\n"` too.
- `Delimiter` is still `";"`, `DetectDelimiter` is still opt-in.
- UTF-8 is still written without the BOM: set `TextEncoding = new UTF8Encoding(true)` to write it.

### ASP.NET Core

- `ITinyCsvFactory` and `TinyCsvFactory` are in the `Mafesoft.TinyCsv.AspNetCore` namespace (before: `TinyCsv` and `TinyCsv.AspNetCore`).
- The registrations are kept by the service provider, not in a static dictionary: two service providers do not share them,
  and `new TinyCsvFactory()` does not see the instances registered with `AddTinyCsv<T>(name, options)` (inject `ITinyCsvFactory`).
- `AddTinyCsv<T>(name, options)` no longer calls `BuildServiceProvider`: the instance is created the first time it is requested.
- New `AddTinyCsv<T>(options)`, that registers `ITinyCsv<T>` as singleton; on .NET 8 or later the named instances are keyed services too.
- The `AddTinyCsv` methods return the `IServiceCollection`.
- A name registered twice throws `InvalidOperationException` (before: `ArgumentNullException`);
  `Get<T>` with a name that is not registered throws `KeyNotFoundException` (before: `ArgumentNullException`).
- The package references `Microsoft.Extensions.DependencyInjection.Abstractions` instead of `Microsoft.Extensions.DependencyInjection`.

### Nullable reference types

Both packages are annotated for nullable reference types. A project with `<Nullable>enable</Nullable>` may see new warnings,
i.e. for the options that may be null (`BadDataFound`, `SkipRow`, `Culture`, `TextEncoding`, `HeaderNameComparer`:
null is the default behavior), for the values of `TinyCsvRecords` (`IDictionary<string, string?>`: a value missing from a short record is null)
and for `ITinyCsvFactory.TryGet<T>`, whose `out` value is not null when it returns true.

`IValueConverter`:
- `ConvertBack(string value, ...)` receives the text of the field, never null: an empty field is an empty string. It returns `object?`.
- `Convert(object? value, ...)` receives null for a null property, and it returns `string?`: null and an empty string are written as an empty field.
- `parameter` is `object?` (TinyCsv passes null), `provider` is never null.

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

### Renamed in 3.1.0

Two names with a typo have a new name.
The old names were obsolete (warning `CS0618`) in 3.x and have been removed in 4.0:

| Obsolete | New |
|---|---|
| `GetAlGetAllLinesAndFieldslLinesFromText` | `GetAllLinesAndFieldsFromText` |
| `Handlers.Write.RowWrittin` (`RowWrittinEventArgs<T>`) | `Handlers.Write.RowWritten` (`RowWrittenEventArgs<T>`) |

3.1.0 replaces 3.0.0, which has been unlisted from NuGet.

### Changes in 3.1

3.1 adds `MapColumnsByHeader`, the automatic columns and the `Culture` option.
The `Culture` option fixes how the values are read, please check them when you upgrade:

| Area | 3.0 | 3.1 |
|---|---|---|
| Dates and times without a format (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`) | written with the invariant culture, read with the culture of the machine: on a non US machine a saved file was not read back as it is (`11/02/2026` was read as 11 February) | written and read with `Culture` |
| Columns with a format (i.e. `"dd MMMM yyyy"`, `"N2"`) | written and read with the culture of the machine | written and read with `Culture` |
| `byte` and `sbyte` | written and read with the culture of the machine | written and read with `Culture` |

To read the values with the culture of the machine, as in 3.0, set `options.Culture = CultureInfo.CurrentCulture`.

### Changes in 3.2

3.2 adds the `ValidateValues` and `BadDataFound` options (see [Bad data](#bad-data)). Furthermore:

| Area | 3.1 | 3.2 |
|---|---|---|
| Exception thrown by a converter while reading (i.e. a custom converter, or an invalid base64 value) | the exception of the converter, without the position | an `InvalidColumnValueException` with the row, the column and the value; the exception of the converter is the `InnerException` |
| Exceptions thrown while reading | without the position | they derive from `CsvException`, with `RowIndex`, `Row`, `ColumnName` and `Value`, and the message ends with the position, i.e. `(row 2, column Id)` |
| Message of `InvalidColumnCountException` | `Invalid column count 2 at 3 line, required 3 columns` | `Invalid column count 2, required 3 columns. (row 2)` |
| Built-in converters of numbers, dates, `bool`, `char` and `Guid` | derive from `DefaultValueConverter` | derive from `ParsableValueConverter<T>`, that derives from `DefaultValueConverter` |

### Changes in 3.4

| Area | 3.3 | 3.4 |
|---|---|---|
| `GetAllText`/`GetAllLines` of a sequence that can be enumerated once (i.e. the result of `LoadFromText`) | only the header was written, the rows were lost | every row is written |

### Changes in 3.5

3.5 adds the [delimiter detection](#detect-the-delimiter), the [nested properties](#nested-properties),
the [records without a model](#records-without-a-model), the [writer of one record at a time](#write-one-record-at-a-time)
and the [class map](#class-map). Furthermore:

| Area | 3.4 | 3.5 |
|---|---|---|
| [Automatic columns](#automatic-columns): a property whose type is a class with a public parameterless constructor (i.e. `Address`) | ignored | its properties are columns, i.e. `Address_City` |
| `SaveAsync(Stream, ...)` | the cancellation token was not used | the cancellation token is used |

To keep the columns of 3.4, define the columns or use a [class map](#class-map).

#### Examples of 3.5

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

These examples, and others, are in [CsvSampleConsoleApp](samples/CsvSampleConsoleApp/Features35.cs).

## Custom Value Converter

It is possible to define a custom converter for a column, like this:

```c#
public class Base64Converter : IValueConverter
{
    public string? Convert(object? value, object? parameter, IFormatProvider provider) 
        => Base64Encode($"{value}");

    public object? ConvertBack(string value, Type targetType, object? parameter, IFormatProvider provider) 
        => Base64Decode(value);

    private string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    private string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }
}
```

Base64Converter is used to convert the data column to a base64 string and vice versa for TextBase64 column.

A column is defined with the model's property only. The library get or set the value in automatically.

## Read

To read a csv file is only necessary invoke the load method, like this:

```c#
var models = csv.LoadFromFile("file.csv");
foreach(var model in models){
    // processing
}
```

or you can to use the asynchronously method, that returns an `IAsyncEnumerable<T>` on every target framework
(from .NET Framework 4.6.2 to .NET 10): the records are read one at a time, without loading the whole file in memory.

```c#
await foreach (var model in csv.LoadFromFileAsync("file.csv"))
{
    // processing
}
```

or, to get a list (`ToListAsync` is in `Mafesoft.TinyCsv.Extensions`; on .NET 10 it is the one of `System.Linq`):

```c#
var models = await csv.LoadFromFileAsync("file.csv").ToListAsync();
```

`SaveAsync` and `TinyCsvWriter<T>.WriteRecordsAsync` accept an `IAsyncEnumerable<T>` as well,
so a source can be copied to a file without buffering it (i.e. from EF Core `AsAsyncEnumerable()`):

```c#
await csv.SaveAsync("orders.csv", db.Orders.AsNoTracking().AsAsyncEnumerable());
```

The records without a model can be read asynchronously as dictionaries with
`TinyCsvRecords.LoadFromFileAsync`, `LoadFromStreamAsync` and `LoadFromTextAsync`.

The load method returns a collection of Model type items and takes as argument the file's path.

### Map the columns by header

By default the columns are read by position: the first column of the file is the first column defined in the options, and so on.
With `MapColumnsByHeader`, the columns are read by the names in the header record:

```c#
var csv = new TinyCsv<Model>(options =>
{
    options.HasHeaderRecord = true;
    options.MapColumnsByHeader = true;

    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn("Product name", m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});
```

- the order of the columns in the file does not matter;
- the columns of the file that are not defined in the options are ignored;
- the names are compared ignoring the case and the spaces around them. Use `HeaderNameComparer` to change it, e.g. `StringComparer.Ordinal`;
- a column that is not in the header throws a `MissingColumnException`, with the names in `MissingColumns`;
- it requires `HasHeaderRecord = true`, otherwise an `InvalidOperationException` is thrown.

With attributes, use `[MapColumnsByHeader]` on the model, the column name is the `name` of `[Column]` or the property name.

### Automatic columns

When no column is defined, the columns are the public properties of the model that have a getter and a setter,
in the order of declaration, and the column name is the property name:

```c#
var csv = new TinyCsv<Model>(options =>
{
    options.HasHeaderRecord = true;
    options.MapColumnsByHeader = true;
});
```

The properties of a class with a public parameterless constructor are columns too, see [nested properties](#nested-properties).
The other properties whose type has no value converter (i.e. collections) are ignored.
To use a format, a custom converter or a different name, define the columns.

### Nested properties

A property of a property is a column, whose name is the names of the properties joined by an underscore:

```c#
public class Customer
{
    public int Id { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

var csv = new TinyCsv<Customer>(options =>
{
    options.HasHeaderRecord = true;
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Address.City); // the column Address_City
});
```

With the [automatic columns](#automatic-columns), the columns are `Id` and `Address_City`.
When reading, a null object (i.e. `Address`) is created, so its type must have a public parameterless constructor.
When writing, the value of a null object is empty.

### Detect the delimiter

With `DetectDelimiter`, the delimiter is detected from the first records of the file, when reading:

```c#
var csv = new TinyCsv<Model>(options =>
{
    options.HasHeaderRecord = true;
    options.DetectDelimiter = true;
});
```

- the delimiter is one of `DelimiterCandidates`: comma, semicolon, tab and pipe;
- the delimiter found the same number of times in each of the first 10 records wins, ignoring the quoted values, the rows to skip and the comments;
- when no candidate is found, the delimiter is `Delimiter`, that is also the delimiter used to write.

With attributes, use `[DetectDelimiter(true)]` on the model.

### Records without a model

`TinyCsvRecords` reads the records without a model: as dictionaries, as dynamic objects or into a `DataTable`.
The values are strings, named by the header:

```c#
var csv = new TinyCsvRecords(options =>
{
    options.Delimiter = ",";
});

foreach (var record in csv.LoadFromFile("file.csv"))
{
    Console.WriteLine(record["Name"]);
}

foreach (var record in csv.LoadDynamicFromFile("file.csv"))
{
    Console.WriteLine(record.Name);
}

DataTable table = csv.LoadDataTableFromFile("file.csv");
```

- `HasHeaderRecord` is true by default. Without the header, the names are `Field1`, `Field2` and so on;
- a value beyond the header is named by its position too (i.e. `Field4`), a value missing from a short record is `null`;
- an empty name of the header is named by its position, a repeated name is followed by a number (i.e. `Name_2`);
- the dictionaries compare the names with `HeaderNameComparer`, that ignores the case by default.

### Bad data

By default, a value that cannot be converted to the type of its column (i.e. `abc` for an `int`) is read as the default value.
With `ValidateValues`, it is bad data and an `InvalidColumnValueException` is thrown.
An empty value is always valid: it is `null` for a nullable type, otherwise the default value.

Every exception thrown while reading a row derives from `CsvException`, and it says where the error is:

| Property | Description |
|---|---|
| `RowIndex` | the index of the row, starting from 0, the same index of the read events (the header is the row 0) |
| `Row` | the row, as it is in the file |
| `ColumnName` | the column, when the error is about a value |
| `Value` | the value, as it is in the file, when the error is about a value |

With `BadDataFound`, the rows with bad data are skipped and the read goes on:

```c#
var errors = new List<CsvException>();
var csv = new TinyCsv<Model>(options =>
{
    options.HasHeaderRecord = true;
    options.ValidateValues = true;
    options.BadDataFound = error => errors.Add(error);
});

var models = csv.LoadFromFile("file.csv").ToList();
foreach (var error in errors)
{
    Console.WriteLine(error.Message); // i.e. The value 'abc' is not a valid Int32. (row 2, column Id)
}
```

The bad data are the values that cannot be converted, the rows with a wrong column count (`ValidateColumnCount`),
the comments when `AllowComment` is false and the exceptions of the custom converters.
A column that is not in the header (`MapColumnsByHeader`) is not bad data of a row, so it is always thrown.
With attributes, use `[ValidateValues]` on the model.

## Write

To write a csv file is only necessary invoke the Save method, like this:

```c#
csv.Save("file_export.csv", models);
```

or you can to use the asynchronously method, like this:

```c#
await csv.SaveAsync("file_export.csv", models);
```

The save method takes the file's path and a collection of Model type.
The method saves the file.

### Write one record at a time

For very large files, the writer writes the records one at a time, without keeping them in memory:

```c#
using (var writer = csv.CreateWriter("file_export.csv"))
{
    foreach (var model in GetModels())
    {
        writer.WriteRecord(model);
    }
}
```

The header is written before the first record, and the file is completed when the writer is disposed.
`CreateWriter` takes a path, a `Stream` or a `StreamWriter`: the stream and the stream writer are left open.
There are `WriteRecordAsync`, `WriteRecordsAsync` (with an `IEnumerable<T>` or an `IAsyncEnumerable<T>`) and `await using`, on every target framework.

## Event handlers

It's possible to define event handlers for the events of the library, like this:

```c#
options.Handlers.Read.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
options.Handlers.Read.RowReading += (s, e) => Console.WriteLine($"{e.Index}-{e.Row}");
options.Handlers.Read.RowRead += (s, e) => Console.WriteLine($"{e.Index}-{e.Model}");
```

and the write handlers are:

```c#
options.Handlers.Write.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
options.Handlers.Write.RowWriting += (s, e) => Console.WriteLine($"{e.Index} - {e.Model}");
options.Handlers.Write.RowWritten += (s, e) => Console.WriteLine($"{e.Index} - {e.Row}");
```

The handlers are enabled if the option "EnableHandlers" is set true value, like this:

```c#
var csv = new TinyCsv<Model>(options =>
{
    // Options
    // ...
    options.EnableHandlers = true;
    //...

    // Columns
    // ...
});
```
## Using attributes in the model definition

You can use, in alternative way, the attributes in the model definition, like this:

```c#
[Delimiter(";")]
[RowsToSkip(0)]
[SkipRow(typeof(CustomSkipRow))]
[TrimData(true)]
[ValidateColumnCount(true)]
[HasHeaderRecord(true)]
public class AttributeModel
{
    [Column]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }

    [Column]
    public decimal Price { get; set; }

    [Column(format: "dd/MM/yyyy")]
    public DateTime CreatedOn { get; set; }

    [Column(converter: typeof(Base64Converter))]
    public string TextBase64 { get; set; }

    public override string ToString()
    {
        return $"ToString: {Id}, {Name}, {Price}, {CreatedOn}, {TextBase64}";
    }
}

class CustomSkipRow : ISkipRow
{
    public Func<string, int, bool> SkipRow { get; } = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
}

class Base64Converter : IValueConverter {
    ...
}

```

and you can use it, like this

```c#
 var csv = new TinyCsv<AttributeModel>();
```

## Class map

A class map defines the columns and the options outside the model, so the model has no attributes:

```c#
public sealed class ProductMap : CsvClassMap<Product>
{
    public ProductMap()
    {
        Options(o =>
        {
            o.HasHeaderRecord = true;
            o.Delimiter = ",";
        });
        Map(m => m.Id);
        Map(m => m.Name).Name("product_name");
        Map(m => m.Price).Format("0.00");
        Map(m => m.Supplier.Name); // the column Supplier_Name
    }
}

var csv = new TinyCsv<Product>(new ProductMap());
```

`Map` takes the property and returns the column, with `Index`, `Name`, `Format`, `FormatProvider` and `Converter`.
The index is the position of `Map` and the name is the property name, unless they are set.
To change the options of the map, use `UseClassMap` and then the options:

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.UseClassMap<ProductMap>();
    options.Delimiter = ";";
});
```

## Performance

### Compared with other libraries

The [competitive benchmark](benchmark/README.md#competitive-benchmark) reads and writes 100,000 rows of 8 typed columns
(`int`, `string`, `decimal`, `DateTime`, `bool`, with some quoted values) with TinyCsv and 10 other libraries for .NET.

| Library | Read | Write |
|---|---:|---:|
| [Sylvan.Data.Csv](https://github.com/MarkPflug/Sylvan) | 24.1 ms / 23.8 MB | 33.9 ms / 0.05 MB |
| **TinyCsv** | **25.5 ms / 23.7 MB** | **31.4 ms / 2.2 MB** |
| [Sep](https://github.com/nietras/Sep) | 40.6 ms / 23.7 MB | 41.2 ms / 0.02 MB |
| [RecordParser](https://github.com/leandromoh/RecordParser) | 61.4 ms / 23.7 MB | 30.6 ms / 0.02 MB |
| [NReco.Csv](https://github.com/nreco/csv) ¹ | 83.1 ms / 41.8 MB | 41.8 ms / 21.3 MB |
| [Csv](https://github.com/stevehansen/csv) ¹ | 84.2 ms / 73.0 MB | 34.2 ms / 19.6 MB |
| [TinyCsvParser](https://github.com/bytefish/TinyCsvParser) ¹ | 92.4 ms / 68.6 MB | - |
| [CsvHelper](https://joshclose.github.io/CsvHelper/) | 99.4 ms / 53.2 MB | 71.3 ms / 25.2 MB |
| [LumenWorksCsvReader](https://github.com/phatcher/CsvReader) ¹ | 119.3 ms / 164.5 MB | - |
| [FileHelpers](https://www.filehelpers.net/) ¹ | 126.7 ms / 92.9 MB | 59.9 ms / 103.0 MB |
| [TextFieldParser](https://learn.microsoft.com/dotnet/api/microsoft.visualbasic.fileio.textfieldparser) ¹ | 425.8 ms / 1,026.5 MB | - |

The times are the means on .NET 10, with the allocated memory; the [benchmark page](benchmark/README.md#results) has the .NET 8 results too.
TinyCsv, CsvHelper, FileHelpers, TinyCsvParser and RecordParser map the columns to the properties,
while the other libraries are used with a hand written mapping: the [benchmark page](benchmark/README.md#competitive-benchmark) says how each library is used.
TinyCsvParser, LumenWorks and `TextFieldParser` only read.
Environment: BenchmarkDotNet 0.15.2, .NET 10.0.11, Windows Server 2019 (Hyper-V), Intel Xeon Platinum 8370C 2.80GHz.

¹ From the previous run with all the libraries, not measured again with the main ones: compare them with care.

### Compared with the previous versions

The [versions benchmark](benchmark/README.md#versions-benchmark) compares the last versions of TinyCsv with [BenchmarkDotNet](https://benchmarkdotnet.org/), reading and writing `N` rows of 7 columns.
Each cell is the mean time and the allocated memory.

#### LoadFromText

| N | 2.1.0 | 2.2.2 | 3.0 |
|---:|---:|---:|---:|
| 100 | 0.072 ms / 119 KB | 0.106 ms / 195 KB | **0.065 ms / 94 KB** |
| 1,000 | 0.727 ms / 1.14 MB | 1.067 ms / 1.89 MB | **0.647 ms / 0.82 MB** |
| 10,000 | 8.39 ms / 11.40 MB | 14.62 ms / 18.88 MB | **7.42 ms / 8.13 MB** |
| 100,000 | 74.4 ms / 114.0 MB | 110.6 ms / 188.7 MB | **71.5 ms / 81.2 MB** |
| 1,000,000 | 772 ms / 1,140 MB | 1,099 ms / 1,887 MB | **679 ms / 812 MB** |

With 1,000,000 rows, 3.0 reads **38% faster** than 2.2.2 and allocates **57% less memory**.

#### SaveToText

| N | 2.1.0 | 2.2.2 | 3.0 |
|---:|---:|---:|---:|
| 100 | 0.092 ms / 107 KB | 0.093 ms / 107 KB | 0.100 ms / 124 KB |
| 1,000 | 1.117 ms / 1.10 MB | 1.120 ms / 1.10 MB | 1.105 ms / 1.26 MB |
| 10,000 | 11.88 ms / 10.47 MB | 11.47 ms / 10.47 MB | 12.71 ms / 12.08 MB |
| 100,000 | 94.4 ms / 100.7 MB | 95.8 ms / 100.7 MB | 104.4 ms / 116.7 MB |
| 1,000,000 | 988 ms / 1,103 MB | 983 ms / 1,103 MB | 1,107 ms / 1,263 MB |

In 3.0, `Save` escapes the values as RFC 4180, so the saved file is always read back as it is.
The write is about 12% slower and allocates about 15% more memory than 2.2.2.

Environment: BenchmarkDotNet 0.13.12, .NET 8.0.29, Windows Server 2019 (Hyper-V), Intel Xeon Platinum 8370C 2.80GHz.
The 3.0 column was measured with the 3.0.0-rc1 package. The results of 3.5.0 will be added after its release.
The full report is in [benchmark/results](benchmark/results/TinyCsvLibraryBenchmark-report-github.md).

<details>
<summary>Previous results (1.5.2 - 2.1.0-beta1)</summary>

![alt TinyCsv Performance](benchmark/images/performance.png)

</details>


## TinyCsv.AspNetCore Extensions

The Mafesoft.TinyCsv.AspNetCore package registers TinyCsv in the ASP.NET Core dependency injection:

```shell
dotnet add package Mafesoft.TinyCsv.AspNetCore
```

```c#
using Mafesoft.TinyCsv;
using Mafesoft.TinyCsv.AspNetCore;
using Mafesoft.TinyCsv.AspNetCore.Extensions;
```

The package depends only on `Microsoft.Extensions.DependencyInjection.Abstractions`.

A `TinyCsv<T>` instance is thread-safe after its construction, as long as its options are not changed: it is registered as singleton.

### One instance per model

```c#
builder.Services.AddTinyCsv<Order>(options =>
{
    options.HasHeaderRecord = true;
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Total);
});

app.MapGet("/orders", (ITinyCsv<Order> csv) => csv.LoadFromFileAsync("orders.csv"));
```

### Named instances

Several instances of the same model are registered with a name, unique in the service collection.
They are returned by `ITinyCsvFactory.Get<T>(name)` and, on .NET 8 or later, they are also keyed services:

```c#
builder.Services
    .AddTinyCsv<Order>("orders-it", o => o.Delimiter = ";")
    .AddTinyCsv<Order>("orders-us", o => o.Delimiter = ",");

// .NET 8 or later
app.MapGet("/orders/it", ([FromKeyedServices("orders-it")] ITinyCsv<Order> csv) => csv.LoadFromFileAsync("orders-it.csv"));

// every target framework
app.MapGet("/orders/us", (ITinyCsvFactory factory) => factory.Get<Order>("orders-us").LoadFromFileAsync("orders-us.csv"));
```

`ITinyCsvFactory.Create<T>(options)` creates a new instance, that is not registered.

The factory is a singleton of the service provider: two service providers (i.e. two `WebApplicationFactory` in the integration tests)
do not share their registrations.

| Error | Exception |
|---|---|
| a name registered twice | `InvalidOperationException` (when it is registered) |
| `Get<T>` with a name that is not registered | `KeyNotFoundException` |
| `Get<T>` with the name of an instance of another model | `InvalidOperationException` |
