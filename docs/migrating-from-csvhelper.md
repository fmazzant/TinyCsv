# Migrating from CsvHelper

This guide is for TinyCsv 4.0 (`Mafesoft.TinyCsv`).

This guide maps the most common [CsvHelper](https://joshclose.github.io/CsvHelper/) code to TinyCsv.
TinyCsv has a smaller API: one object, `TinyCsv<T>`, configured once with its options, that reads and writes the whole file.

## Install

```shell
dotnet remove package CsvHelper
dotnet add package Mafesoft.TinyCsv
```

The namespaces are `Mafesoft.TinyCsv`, `Mafesoft.TinyCsv.Attributes`, `Mafesoft.TinyCsv.Conversions` and `Mafesoft.TinyCsv.Exceptions`.

## The defaults are different

Check these first: most of the surprises after a migration come from a different default.

| Setting | CsvHelper | TinyCsv | To get the CsvHelper behaviour |
|---|---|---|---|
| Header record | `HasHeaderRecord = true` | `HasHeaderRecord = false` | `options.HasHeaderRecord = true` |
| Delimiter | `,` (the list separator of the culture) | `;` | `options.Delimiter = ","` |
| Columns matched | by header name | by position | `options.MapColumnsByHeader = true` |
| Culture | the culture you pass to `CsvReader` | `CultureInfo.InvariantCulture` | `options.Culture = ...` |
| Value that cannot be converted (`abc` for an `int`) | `TypeConverterException` | the default value | `options.ValidateValues = true` |
| Rows starting with `#` | read as data (`AllowComments = false`) | skipped as comments (`AllowComment = true`) | `options.AllowComment = false` (it throws on a comment) or change `options.Comment` |
| Blank lines | skipped (`IgnoreBlankLines = true`) | read as a model with default values | `options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row)` |
| Members without a mapping | all the public properties (auto map) | all the public properties, only when no column is defined | define every column, or none |

## Reading

CsvHelper:

```c#
using var reader = new StreamReader("products.csv");
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
var products = csv.GetRecords<Product>().ToList();
```

TinyCsv:

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
    options.MapColumnsByHeader = true;
});

var products = csv.LoadFromFile("products.csv").ToList();
```

`TinyCsv<T>` opens and closes the file. It can also read a `Stream`, a `StreamReader` or a string:

| CsvHelper | TinyCsv |
|---|---|
| `new CsvReader(new StreamReader(path), ...)` + `GetRecords<T>()` | `LoadFromFile(path)` |
| `new CsvReader(streamReader, ...)` + `GetRecords<T>()` | `LoadFromStream(streamReader)` / `LoadFromStream(stream)` |
| `new CsvReader(new StringReader(text), ...)` + `GetRecords<T>()` | `LoadFromText(text)` |
| `GetRecordsAsync<T>()` | `LoadFromFileAsync(path)` (an `IAsyncEnumerable<T>` on .NET 5+ and .NET Standard 2.1) |
| `GetRecords<dynamic>()` | `new TinyCsvRecords(...).LoadDynamicFromFile(path)`, or `LoadFromFile(path)` for dictionaries |
| `CsvDataReader` + `DataTable.Load` | `new TinyCsvRecords(...).LoadDataTableFromFile(path)` |
| `csv.Read()` + `csv.GetField(i)` | `GetAllLinesAndFieldsFromFile(path)`, each item is the `string[]` of a row |

Async:

```c#
await foreach (var product in csv.LoadFromFileAsync("products.csv"))
{
    // ...
}
```

The `TinyCsv<T>` instance is reusable: create it once and use it for every read and write of the same format (with ASP.NET Core, see [TinyCsv.AspNetCore](../README.md#aspnet-core)).

## Writing

CsvHelper:

```c#
using var writer = new StreamWriter("products.csv");
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
csv.WriteRecords(products);
```

TinyCsv:

```c#
csv.Save("products.csv", products);
// or
await csv.SaveAsync("products.csv", products);
// or, as a string
string text = csv.GetAllText(products);
```

The header is written when `HasHeaderRecord` is true. The values are quoted as [RFC 4180](https://www.rfc-editor.org/rfc/rfc4180) when they contain the delimiter, a quote or a new line.

To write one record at a time, as `CsvWriter.WriteRecord`:

```c#
using (var writer = csv.CreateWriter("products.csv"))
{
    foreach (var product in products)
    {
        writer.WriteRecord(product);
    }
}
```

## Mapping

### ClassMap → `CsvClassMap<T>` or `options.Columns`

CsvHelper:

```c#
public sealed class ProductMap : ClassMap<Product>
{
    public ProductMap()
    {
        Map(m => m.Id).Index(0);
        Map(m => m.Name).Name("Product name");
        Map(m => m.CreatedOn).TypeConverterOption.Format("dd/MM/yyyy");
        Map(m => m.Notes).TypeConverter<Base64Converter>();
        Map(m => m.Internal).Ignore();
    }
}

csv.Context.RegisterClassMap<ProductMap>();
```

TinyCsv, with a class map that keeps the columns outside the model:

```c#
public sealed class ProductMap : CsvClassMap<Product>
{
    public ProductMap()
    {
        Options(o => o.HasHeaderRecord = true);
        Map(m => m.Id).Index(0);
        Map(m => m.Name).Name("Product name");
        Map(m => m.CreatedOn).Format("dd/MM/yyyy");
        Map(m => m.Notes).Converter(new Base64Converter());
        // Internal is not mapped: it is ignored
    }
}

var csv = new TinyCsv<Product>(new ProductMap());
```

or with the options:

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;

    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn("Product name", m => m.Name);
    options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.Notes, new Base64Converter());
    // Internal is not mapped: it is ignored
});
```

| CsvHelper `MemberMap` | `CsvClassMap<T>` | `options.Columns` |
|---|---|---|
| `Index(i)` | `Index(i)` | the order of `AddColumn`, or `AddColumn(i, "Name", m => m.X, converter)` |
| `Name("x")` | `Name("x")` | `AddColumn("x", m => m.X)` |
| `TypeConverterOption.Format("...")` | `Format("...")` | `AddColumn(m => m.X, "...")` |
| `TypeConverterOption.CultureInfo(c)` | `FormatProvider(c)` | `AddColumn(m => m.X, c)` (an `IFormatProvider`) |
| `TypeConverter<C>()` | `Converter(new C())` | `AddColumn(m => m.X, new C())` |
| `Ignore()` | do not map the property | do not add the column |
| `References<TMap>()`, `Map(m => m.Address.City)` | `Map(m => m.Address.City)` | `AddColumn(m => m.Address.City)` |
| `Optional()` | not supported | with `MapColumnsByHeader`, a missing column throws `MissingColumnException` |
| `Default(v)` | not supported | an empty value is `null` for a nullable type, otherwise the default value of the type |
| `Constant(v)` | not supported | set the value after the read |
| `TypeConverterOption.NullValues("NULL")` | `NullValues("NULL")` | `options.NullValues` (all the columns) |

A nested property (`m => m.Address.City`) is the column `Address_City`: when reading, the null objects (`Address`) are created.

When no column is defined, TinyCsv maps every public property with a getter, a setter and a supported type, in the order of declaration,
like the auto map of CsvHelper. The properties of a class with a public parameterless constructor are mapped too, i.e. `Address_City`.
As soon as one column is defined, only the defined columns are mapped.

### Attributes

| CsvHelper | TinyCsv |
|---|---|
| `[Name("x")]` | `[Column(name: "x")]` |
| `[Index(0)]` | `[Column(index: 0)]` |
| `[Format("dd/MM/yyyy")]` | `[Column(format: "dd/MM/yyyy")]` |
| `[TypeConverter(typeof(C))]` | `[Column(converter: typeof(C))]` |
| `[Ignore]` | no `[Column]` on the property |
| `[NullValues("NULL")]` | `[NullValues("NULL")]` on the property, or on the class for all the columns |
| `[CultureInfo("it-IT")]` | `[Culture("it-IT")]` on the class |
| `[Delimiter(";")]`, `[HasHeaderRecord(true)]`, ... | `[Delimiter(";")]`, `[HasHeaderRecord(true)]`, ... on the class |

With attributes, the model is configured by itself and `TinyCsv<T>` is created without options:

```c#
[HasHeaderRecord(true)]
[Delimiter(",")]
[MapColumnsByHeader]
public class Product
{
    [Column] public int Id { get; set; }
    [Column(name: "Product name")] public string Name { get; set; }
    [Column(format: "dd/MM/yyyy")] public DateTime CreatedOn { get; set; }
}

var csv = new TinyCsv<Product>();
```

When a model has at least one `[Column]`, only the properties with `[Column]` are mapped.

## Type converters

CsvHelper:

```c#
public class Base64Converter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        => Encoding.UTF8.GetString(Convert.FromBase64String(text));

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{value}"));
}
```

TinyCsv:

```c#
public class Base64Converter : IValueConverter
{
    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        => Encoding.UTF8.GetString(Convert.FromBase64String(value));

    public string Convert(object value, object parameter, IFormatProvider provider)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{value}"));
}
```

`ConvertBack` reads (string → value), `Convert` writes (value → string). `provider` is the format provider of the column or the `Culture` option.
An exception thrown by a converter while reading is wrapped in an `InvalidColumnValueException`, with the row and the column.

TinyCsv has built-in converters for the primitive types, `decimal`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, `BigInteger`, `byte[]` (base64), enums and their nullable versions.

## Configuration

| `CsvConfiguration` | `CsvOptions<T>` |
|---|---|
| `Delimiter` | `Delimiter` |
| `DetectDelimiter`, `DetectDelimiterValues` | `DetectDelimiter`, `DelimiterCandidates` (comma, semicolon, tab and pipe) |
| `HasHeaderRecord` | `HasHeaderRecord` |
| `Quote` | `DoubleQuotes` |
| `Escape = '\\'` | `AllowBackSlashToEscapeQuote = true` |
| `Comment`, `AllowComments` | `Comment`, `AllowComment` (note the different default) |
| `NewLine` | `NewLine` (used to write, `"\r\n"` by default on every operating system) |
| `Encoding` | `TextEncoding` |
| `CultureInfo` (constructor) | `Culture` |
| `TrimOptions = TrimOptions.Trim` | `TrimData = true` (the spaces inside a quoted value are kept) |
| `IgnoreBlankLines` | `SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row)` |
| `ShouldSkipRecord` | `SkipRow` (the raw row and its index) or `RowsToSkip` (the first N rows) |
| `PrepareHeaderForMatch` | `HeaderNameComparer` (case insensitive by default) |
| `DetectColumnCountChanges` | `ValidateColumnCount` |
| `HeaderValidated` | with `MapColumnsByHeader`, a missing column throws `MissingColumnException` |
| `MissingFieldFound` | `ValidateColumnCount`; without it a missing field is `null` or the default value of the type |
| `BadDataFound`, `ReadingExceptionOccurred` | `BadDataFound` (the row is skipped and the read goes on) |
| `Mode = CsvMode.RFC4180` | always RFC 4180 |
| `ShouldQuote` | `QuoteMode` (`Minimal` by default, `All`, `NonNumeric`, `None`) |
| `InjectionOptions`, `InjectionCharacters` | `InjectionProtection` (`None` by default, `Escape`, `Strip`, `Throw`), `InjectionCharacters`; the escape character is always the apostrophe |

## Errors

| CsvHelper | TinyCsv |
|---|---|
| `TypeConverterException` | `InvalidColumnValueException` (with `ValidateValues = true`) |
| `HeaderValidationException` | `MissingColumnException`, with the names in `MissingColumns` |
| `MissingFieldException` / `BadDataException` | `InvalidColumnCountException` (with `ValidateColumnCount = true`) |
| `CsvHelperException.Context.Parser.Row` | `CsvException.RowIndex`, `Row`, `ColumnName`, `Value` |

Every reading exception derives from `CsvException`. To collect the errors and go on:

```c#
var errors = new List<CsvException>();
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.ValidateValues = true;
    options.BadDataFound = error => errors.Add(error);
});
```

## Events

CsvHelper has no events on the records. TinyCsv raises an event for each row when `EnableHandlers` is true:
`Handlers.Read.RowHeader`, `RowReading`, `RowRead` and `Handlers.Write.RowHeader`, `RowWriting`, `RowWritten`.
See [Event handlers](../README.md#event-handlers).

## What TinyCsv does not do

- anonymous types: TinyCsv maps to a class with a parameterless constructor, or reads the records without a model with `TinyCsvRecords`;
- writing dynamic records and dictionaries;
- constructor mapping (records with a primary constructor and no setters);
- reading field by field with `Read()`/`GetField<T>()`: use `GetAllLinesAndFieldsFromFile` to get the raw fields of each row.
