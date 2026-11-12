# Mafesoft.TinyCsv

Read and write CSV files with strongly typed models and a few lines of options, from .NET Framework 4.6.2 to .NET 10:
lambda columns, attributes or class maps, header mapping, delimiter detection, per-row error handling,
records without a model (dictionary, `dynamic`, `DataTable`) and streaming async read and write.

Target frameworks: .NET 10, .NET 8, .NET Standard 2.1, .NET Standard 2.0, .NET Framework 4.6.2.

## Quick start

```
dotnet add package Mafesoft.TinyCsv
```

```c#
using Mafesoft.TinyCsv;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";          // the default
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});

await foreach (var product in csv.LoadFromFileAsync("products.csv"))
{
    Console.WriteLine($"{product.Id} {product.Name} {product.Price}");
}

csv.Save("products_export.csv", products);
```

Defaults: delimiter `;`, new line `\r\n` on every operating system, UTF-8 without BOM.

## Links

- [Documentation](https://github.com/fmazzant/TinyCsv#readme)
- [Migrating to 4.0](https://github.com/fmazzant/TinyCsv#migrating-to-40) (from `Mafe.TinyCsv`)
- [Migrating from CsvHelper](https://github.com/fmazzant/TinyCsv/blob/main/docs/migrating-from-csvhelper.md)
- [Changelog](https://github.com/fmazzant/TinyCsv/blob/main/CHANGELOG.md)
- [Issues](https://github.com/fmazzant/TinyCsv/issues)

Licensed under the [MIT License](https://github.com/fmazzant/TinyCsv/blob/main/LICENSE.md).
