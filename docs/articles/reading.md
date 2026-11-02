# Reading

## From a file, a stream or a string

```c#
IEnumerable<Product> fromFile = csv.LoadFromFile("products.csv");
IEnumerable<Product> fromStream = csv.LoadFromStream(stream);
IEnumerable<Product> fromReader = csv.LoadFromStream(streamReader);
IEnumerable<Product> fromText = csv.LoadFromText(text);
```

The result is lazy: the file is read while you enumerate it, one record at a time, so a large file is not loaded in memory.
Call `ToList()` to read it all at once. The file is closed when the enumeration ends.

The encoding is `TextEncoding` (UTF-8 by default); a byte order mark, if present, takes precedence.

## Async

On .NET 5+ and .NET Standard 2.1, the async methods return an `IAsyncEnumerable<T>`:

```c#
await foreach (var product in csv.LoadFromFileAsync("products.csv", cancellationToken))
{
    // ...
}
```

On .NET Framework and .NET Standard 2.0 they return a `Task<IEnumerable<T>>`:

```c#
var products = await csv.LoadFromFileAsync("products.csv");
```

`LoadFromStreamAsync` and `LoadFromTextAsync` work in the same way.

## Skip rows

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.RowsToSkip = 2;                                          // a preamble before the header
    options.HasHeaderRecord = true;
    options.SkipRow = (row, index) => string.IsNullOrWhiteSpace(row); // blank lines
});
```

- `RowsToSkip` skips the first rows, before the header;
- `SkipRow` receives each row as it is in the file and its index, starting from 0;
- the rows starting with the `Comment` char (`#`) are skipped when `AllowComment` is true (the default).

A blank line that is not skipped is read as a model with the default values.

When `SkipRow` is not set and the [events](events.md) are not enabled, TinyCsv does not create a string for each row, so the read is faster and allocates less memory.

## Raw rows and fields

To read the rows without a model:

```c#
IEnumerable<string> rows = csv.GetAllLinesFromFile("products.csv");
IEnumerable<string[]> fields = csv.GetAllLinesAndFieldsFromFile("products.csv");
```

Each row is a record: a quoted value with new lines is part of one row.
The same methods exist for streams and strings (`...FromStream`, `...FromText`).
