# Columns and mapping

## Define the columns

A column maps a property of the model:

```c#
options.Columns.AddColumn(m => m.Id);                                   // name: "Id"
options.Columns.AddColumn("Product name", m => m.Name);                 // a different name
options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");              // a format
options.Columns.AddColumn(m => m.Price, new CultureInfo("it-IT"));      // a format provider
options.Columns.AddColumn(m => m.Notes, new Base64Converter());         // a converter
options.Columns.AddColumn(4, "Code", m => m.Code, new CodeConverter()); // an index, a name and a converter
```

The expression must be a property of the model, with a getter and a setter.
The [analyzers](analyzers.md) report at compile time an expression that is not a property, and a format on a type that has no format.

## By position

By default, the columns are read by position: the first column of the file is the first column defined in the options, and so on.
The columns of the file after the last one defined are ignored.

## By header

With `MapColumnsByHeader`, the columns are read by the names in the header record:

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.MapColumnsByHeader = true;

    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn("Product name", m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});
```

- the order of the columns in the file does not matter;
- the columns of the file that are not defined are ignored;
- the names are compared ignoring the case and the spaces around them. Use `HeaderNameComparer` to change it, e.g. `StringComparer.Ordinal`;
- a column that is not in the header throws a `MissingColumnException`, with the names in `MissingColumns`;
- it requires `HasHeaderRecord = true`, otherwise an `InvalidOperationException` is thrown.

`Save` writes the columns in the order they are defined.

## Automatic columns

When no column is defined, the columns are the public properties of the model that have a getter and a setter,
in the order of declaration, and the column name is the property name:

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.MapColumnsByHeader = true;
});
```

The properties whose type has no value converter (i.e. classes and collections) are ignored.
To use a format, a custom converter or a different name, define the columns.

## Supported types

`string`, `char`, `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `BigInteger`,
`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, `byte[]` (base64), the enums,
and the nullable versions of the value types. Any other type needs a [converter](converters.md).
