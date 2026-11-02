---
_layout: landing
---

# TinyCsv

TinyCsv is a .NET library to read and write CSV data in an easy way: define a model, the columns, and load or save the file.

[![Nuget](https://img.shields.io/nuget/v/Mafe.TinyCsv?style=flat-square)](https://www.nuget.org/packages/Mafe.TinyCsv)
[![codecov](https://img.shields.io/codecov/c/github/fmazzant/TinyCsv?style=flat-square)](https://codecov.io/gh/fmazzant/TinyCsv)

```shell
dotnet add package Mafe.TinyCsv
```

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});

var products = csv.LoadFromFile("products.csv").ToList();
csv.Save("products_copy.csv", products);
```

- **RFC 4180**: quoted values, escaped quotes and multi-line records, read and written back as they are.
- **Fast**: a `Span<char>` reader, [faster than CsvHelper](articles/performance.md) with half of the memory.
- **Typed**: built-in converters for numbers, dates, enums, `Guid`, `Uri`, `byte[]` and their nullable versions, or [your own](articles/converters.md).
- **Safe by default**: the invariant culture, [bad data](articles/bad-data.md) reported with the row and the column, [mapping errors at compile time](articles/analyzers.md).
- **Everywhere**: .NET Framework 4.5.2+, .NET Standard 2.0/2.1, .NET 5 to .NET 10.

## Where to start

- [Getting started](articles/getting-started.md)
- [Examples](articles/examples.md): practical recipes, from a file upload in ASP.NET Core to a 10 GB file read row by row.
- [Migrating from CsvHelper](articles/migrating-from-csvhelper.md)
- [API reference](api/TinyCsv.yml)
