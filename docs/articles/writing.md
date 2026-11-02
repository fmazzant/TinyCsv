# Writing

## To a file, a stream or a string

```c#
csv.Save("products.csv", products);
csv.Save(stream, products);
csv.Save(streamWriter, products);

string text = csv.GetAllText(products);
string[] rows = csv.GetAllLines(products);
```

and the async versions:

```c#
await csv.SaveAsync("products.csv", products, cancellationToken);
string text = await csv.GetAllTextAsync(products);
```

`Save` with a path creates or overwrites the file and closes it.

## What is written

- the header, when `HasHeaderRecord` is true, with the column names;
- a row for each model, the values separated by `Delimiter` and the rows by `NewLine`;
- the values are converted with the converter and the format of their column, and with `Culture`;
- a value that contains the delimiter, a quote or a new line is quoted, and its quotes are doubled (see [Quoting](quoting.md)).

So a saved file is always read back as it is by the same `TinyCsv<T>`.

## Write while reading

`Save` takes an `IEnumerable<T>`, so you can transform a file without loading it in memory:

```c#
var input = new TinyCsv<Product>(options => { options.HasHeaderRecord = true; options.Delimiter = ";"; });
var output = new TinyCsv<Product>(options => { options.HasHeaderRecord = true; options.Delimiter = ","; });

output.Save("products_comma.csv", input.LoadFromFile("products.csv").Where(p => p.Price > 0));
```
