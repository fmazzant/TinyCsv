# Getting started

## Install

```shell
dotnet add package Mafe.TinyCsv
```

TinyCsv targets .NET Framework 4.5.2 and later, .NET Standard 2.0 and 2.1, and .NET 5 to .NET 10.

## Define the model

A model is a class with a parameterless constructor and a public property for each column:

```c#
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
}
```

## Define the options

```c#
using TinyCsv;

var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";

    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
    options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
});
```

The columns are read by position: the first column of the file is the first column defined, and so on.
To read them by the names in the header, see [Columns and mapping](mapping.md).

When no column is defined, the columns are the public properties of the model, in the order of declaration.

## Read and write

```c#
// read
foreach (var product in csv.LoadFromFile("products.csv"))
{
    Console.WriteLine($"{product.Id} {product.Name} {product.Price}");
}

// write
csv.Save("products_export.csv", products);
```

Given this file:

```csv
Id;Name;Price;CreatedOn
1;Keyboard;49.90;02/11/2026
2;"Mouse; wireless";19.50;03/11/2026
```

`LoadFromFile` returns two products. The second name contains the delimiter, so it is quoted.

## The options

| Option | Default | Description |
|---|---|---|
| `HasHeaderRecord` | `false` | the first row is the header: it is skipped while reading and written while saving |
| `Delimiter` | `;` | the delimiter of the values |
| `DoubleQuotes` | `"` | the quote char |
| `AllowRowEnclosedInDoubleQuotesValues` | `true` | a quoted value can contain new lines |
| `AllowBackSlashToEscapeQuote` | `false` | `\"` is a quote inside a quoted value, in addition to `""` |
| `Comment` | `#` | the comment char |
| `AllowComment` | `true` | the rows starting with `Comment` are skipped; when `false`, they throw `NotAllowCommentException` |
| `RowsToSkip` | `0` | the number of rows to skip at the start of the file |
| `SkipRow` | none | a function `(row, index) => bool` that skips a row |
| `TrimData` | `false` | removes the spaces around the values |
| `ValidateColumnCount` | `false` | a row with a different number of columns throws `InvalidColumnCountException` |
| `EndOfLineDelimiterChar` | `true` | a row can end with the delimiter |
| `MapColumnsByHeader` | `false` | the columns are matched by the names in the header |
| `HeaderNameComparer` | `OrdinalIgnoreCase` | compares the header names with `MapColumnsByHeader` |
| `Culture` | `InvariantCulture` | the culture of numbers and dates, see [Culture](culture.md) |
| `ValidateValues` | `false` | a value that cannot be converted throws, see [Bad data](bad-data.md) |
| `BadDataFound` | `null` | called for each row with bad data, the row is skipped |
| `TextEncoding` | UTF-8 | the encoding of files and streams |
| `NewLine` | `Environment.NewLine` | the new line written by `Save` |
| `EnableHandlers` | `false` | raises the [events](events.md) |

Every option can also be set with an [attribute](attributes.md) on the model.
