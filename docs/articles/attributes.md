# Attributes

The options and the columns can be defined on the model with attributes, from the `TinyCsv.Attributes` namespace:

```c#
using TinyCsv.Attributes;

[HasHeaderRecord(true)]
[Delimiter(";")]
[RowsToSkip(0)]
[SkipRow(typeof(SkipBlankRows))]
[TrimData(true)]
[ValidateColumnCount(true)]
[Culture("it-IT")]
public class Product
{
    [Column]
    public int Id { get; set; }

    [Column(name: "Product name")]
    public string Name { get; set; }

    [Column]
    public decimal Price { get; set; }

    [Column(format: "dd/MM/yyyy")]
    public DateTime CreatedOn { get; set; }

    [Column(converter: typeof(Base64Converter))]
    public string Notes { get; set; }

    public string NotInTheFile { get; set; }
}

public class SkipBlankRows : ISkipRow
{
    public Func<string, int, bool> SkipRow { get; } = (row, index) => string.IsNullOrWhiteSpace(row);
}
```

and `TinyCsv<T>` is created without options:

```c#
var csv = new TinyCsv<Product>();
```

When a model has at least one `[Column]`, only the properties with `[Column]` are columns.

## `[Column]`

| Parameter | Description |
|---|---|
| `index` | the position of the column; by default, the order of the properties |
| `name` | the name in the header; by default, the property name |
| `format` | the format of the value, i.e. `"dd/MM/yyyy"` or `"N2"` |
| `formatProvider` | a type that implements `IFormatProvider`, with a parameterless constructor |
| `converter` | a type that implements `IValueConverter`, with a parameterless constructor |

The [analyzers](analyzers.md) report at compile time a `converter` or `formatProvider` type that does not implement the interface, and two columns with the same index.

## Attributes of the model

| Attribute | Option |
|---|---|
| `[HasHeaderRecord(true)]` | `HasHeaderRecord` |
| `[Delimiter(";")]` | `Delimiter` |
| `[DoubleQuotes('"')]` | `DoubleQuotes` |
| `[AllowRowEnclosedInDoubleQuotesValues(true)]` | `AllowRowEnclosedInDoubleQuotesValues` |
| `[AllowBackSlashToEscapeQuote(true)]` | `AllowBackSlashToEscapeQuote` |
| `[Comment('#')]`, `[AllowComment(true)]` | `Comment`, `AllowComment` |
| `[RowsToSkip(2)]` | `RowsToSkip` |
| `[SkipRow(typeof(MySkipRow))]` | `SkipRow`, the type implements `ISkipRow` |
| `[TrimData(true)]` | `TrimData` |
| `[ValidateColumnCount(true)]` | `ValidateColumnCount` |
| `[ValidateValues]` | `ValidateValues` |
| `[EndOfLineDelimiterChar(true)]` | `EndOfLineDelimiterChar` |
| `[MapColumnsByHeader]` | `MapColumnsByHeader` |
| `[Culture("it-IT")]` | `Culture` |
| `[TextEncoding("utf-16")]` or `[TextEncoding(1200)]` | `TextEncoding` |
| `[NewLine("\r\n")]` | `NewLine` |
