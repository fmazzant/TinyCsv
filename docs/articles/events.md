# Events

The events are raised when `EnableHandlers` is true:

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.EnableHandlers = true;

    options.Handlers.Start += (s, e) => Console.WriteLine("Start");
    options.Handlers.Completed += (s, e) => Console.WriteLine($"Completed: {e.Elements}");

    options.Handlers.Read.RowHeader += (s, e) => Console.WriteLine($"Header: {e.RowHeader}");
    options.Handlers.Read.RowReading += (s, e) => Console.WriteLine($"{e.Index}: {e.Row}");
    options.Handlers.Read.RowRead += (s, e) => Console.WriteLine($"{e.Index}: {e.Model}");

    options.Handlers.Write.RowHeader += (s, e) => Console.WriteLine($"Header: {e.RowHeader}");
    options.Handlers.Write.RowWriting += (s, e) => Console.WriteLine($"{e.Index}: {e.Model}");
    options.Handlers.Write.RowWritten += (s, e) => Console.WriteLine($"{e.Index}: {e.Row}");
});
```

| Event | Arguments |
|---|---|
| `Handlers.Start` | |
| `Handlers.Completed` | `Elements`: the number of rows processed, the header included |
| `Read.RowHeader` | `Index`, `RowHeader` |
| `Read.RowReading` | `Index`, `Row`: the row as it is in the file |
| `Read.RowRead` | `Index`, `Row`, `Model` |
| `Write.RowHeader` | `Index`, `RowHeader` |
| `Write.RowWriting` | `Index`, `Model` |
| `Write.RowWritten` | `Index`, `Model`, `Row` |

`Index` is the index of the row, starting from 0, the same as `CsvException.RowIndex`.

The events need the string of each row, so the read is a bit slower when they are enabled.

## Example: progress

```c#
options.EnableHandlers = true;
options.Handlers.Read.RowRead += (s, e) =>
{
    if (e.Index % 10_000 == 0) progress.Report(e.Index);
};
```
