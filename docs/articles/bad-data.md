# Bad data and errors

## Values that cannot be converted

By default, a value that cannot be converted to the type of its column (i.e. `abc` for an `int`) is read as the default value.
With `ValidateValues`, it is bad data and an `InvalidColumnValueException` is thrown.
An empty value is always valid: it is `null` for a nullable type, otherwise the default value.

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.ValidateValues = true;
});
```

## Where is the error

Every exception thrown while reading a row derives from `CsvException`, and it says where the error is:

| Property | Description |
|---|---|
| `RowIndex` | the index of the row, starting from 0, the same index of the read events (the header is the row 0) |
| `Row` | the row, as it is in the file |
| `ColumnName` | the column, when the error is about a value |
| `Value` | the value, as it is in the file, when the error is about a value |

The message ends with the position, i.e. `The value 'abc' is not a valid Int32. (row 2, column Id)`.

| Exception | When |
|---|---|
| `InvalidColumnValueException` | a value cannot be converted (with `ValidateValues`), or a converter throws |
| `InvalidColumnCountException` | a row has a different number of columns (with `ValidateColumnCount`) |
| `MissingColumnException` | a column is not in the header (with `MapColumnsByHeader`) |
| `NotAllowCommentException` | a row starts with the comment char (with `AllowComment = false`) |
| `EndOfLineDelimiterCharException` | a row ends with the delimiter (with `EndOfLineDelimiterChar = false`) |

## Skip the bad rows

With `BadDataFound`, the rows with bad data are skipped and the read goes on:

```c#
var errors = new List<CsvException>();
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.ValidateValues = true;
    options.BadDataFound = error => errors.Add(error);
});

var products = csv.LoadFromFile("products.csv").ToList();
foreach (var error in errors)
{
    Console.WriteLine(error.Message);
}
```

The bad data are the values that cannot be converted, the rows with a wrong column count (`ValidateColumnCount`),
the comments when `AllowComment` is false and the exceptions of the custom converters.
A column that is not in the header (`MapColumnsByHeader`) is not bad data of a row, so it is always thrown.
