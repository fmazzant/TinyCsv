# Compile-time analyzers

The `Mafe.TinyCsv` package includes Roslyn analyzers that report a wrong mapping while you write the code,
instead of an exception when the file is read. They need nothing to be installed or configured:
they work in Visual Studio 2022, Rider, VS Code and `dotnet build` (.NET 6 SDK or later).

| Id | Severity | Description |
|---|---|---|
| [TCSV001](#tcsv001) | Error | The column must be a property of the model |
| [TCSV002](#tcsv002) | Warning | The property of the column must have a public getter and setter |
| [TCSV003](#tcsv003) | Warning | The type of the column needs a converter |
| [TCSV004](#tcsv004) | Error | The type must implement the interface and have a public parameterless constructor |
| [TCSV005](#tcsv005) | Warning | Two columns have the same index |
| [TCSV006](#tcsv006) | Error | MapColumnsByHeader requires HasHeaderRecord |

## TCSV001

**The column must be a property of the model.**
`AddColumn` reads and writes a property of the model, so its expression must be `m => m.Property`.

```c#
options.Columns.AddColumn(m => m.Name.Length); // TCSV001: a nested property
options.Columns.AddColumn(m => m.GetName());   // TCSV001: a method
options.Columns.AddColumn(m => m.Field);       // TCSV001: a field
options.Columns.AddColumn(m => m.Id + 1);      // TCSV001: a computed value
```

At runtime, these columns throw an `ArgumentException` when `TinyCsv<T>` is created, or an `InvalidColumnValueException` when the file is read.
To read a nested or computed value, read a flat model and project it, or use a [converter](converters.md).

## TCSV002

**The property of the column must have a public getter and setter.**
TinyCsv sets a column with its public setter while reading, and gets it with its public getter while writing.

```c#
public class Product
{
    public decimal Price { get; set; }
    public decimal Total => Price * 1.22m;      // no setter
    public int Code { get; private set; }       // the setter is not public
}

options.Columns.AddColumn(m => m.Total); // TCSV002: the column cannot be read
```

A column without a setter can still be written: when a `TinyCsv<T>` is used only to export, suppress the warning.

```c#
#pragma warning disable TCSV002 // export only
options.Columns.AddColumn(m => m.Total);
#pragma warning restore TCSV002
```

## TCSV003

**The type of the column needs a converter.**
The type has no [built-in converter](mapping.md#supported-types): it is written with `ToString()` and it cannot be read back.

```c#
options.Columns.AddColumn(m => m.Address);                         // TCSV003
options.Columns.AddColumn(m => m.Address, new AddressConverter()); // OK
```

```c#
[Column] public Address Address { get; set; }                                    // TCSV003
[Column(converter: typeof(AddressConverter))] public Address Address { get; set; } // OK
```

## TCSV004

**The type must implement the interface and have a public parameterless constructor.**
TinyCsv creates the types of these attributes when the model is used:

| Attribute | The type must implement |
|---|---|
| `[Column(converter: typeof(T))]` | `IValueConverter` |
| `[Column(formatProvider: typeof(T))]` | `IFormatProvider` |
| `[SkipRow(typeof(T))]` | `ISkipRow` |

and it must be a class with a public parameterless constructor, not abstract.

```c#
[Column(converter: typeof(string))]              // TCSV004: string is not an IValueConverter
[Column(formatProvider: typeof(CultureInfo))]    // TCSV004: CultureInfo has no parameterless constructor
[Column(formatProvider: typeof(NumberFormatInfo))] // OK
```

## TCSV005

**Two columns have the same index.**
The index of a column is the position of its value in the row: two columns with the same index read the same value.
A column without an index has the index of its position.

```c#
options.Columns.AddColumn(m => m.Id);                              // index 0
options.Columns.AddColumn(m => m.Name);                            // index 1
options.Columns.AddColumn(1, "Code", m => m.Code, new CodeConverter()); // TCSV005: 1 is the index of Name
```

```c#
public class Model
{
    [Column(index: 1)] public int X { get; set; }
    [Column(index: 1)] public int Y { get; set; } // TCSV005
}
```

The indexes of `AddColumn` are checked when the columns are added in the same block of code, not in a loop or a condition.

## TCSV006

**MapColumnsByHeader requires HasHeaderRecord.**
`[MapColumnsByHeader]` reads the names of the columns from the header record: without `[HasHeaderRecord(true)]`, the read throws an `InvalidOperationException`.

```c#
[MapColumnsByHeader] // TCSV006
public class Model { ... }

[HasHeaderRecord(true)]
[MapColumnsByHeader] // OK
public class Model { ... }
```

## Configure the analyzers

Change the severity of a rule in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.TCSV002.severity = none
dotnet_diagnostic.TCSV003.severity = error
```

or disable all the analyzers of the package in the project file:

```xml
<PropertyGroup>
  <RunAnalyzers>false</RunAnalyzers>
</PropertyGroup>
```

> [!NOTE]
> `RunAnalyzers` disables every analyzer of the project, not only the TinyCsv ones. To disable only the TinyCsv analyzers,
> set the severity of each rule to `none`.
