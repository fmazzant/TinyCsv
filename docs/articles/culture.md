# Culture

The `Culture` option is used to read and write numbers, dates and the columns with a format.
The default is `CultureInfo.InvariantCulture`, so a file is written and read in the same way on every machine.

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.Culture = new CultureInfo("it-IT"); // 1234,5 and 02/11/2026
    options.Delimiter = ";";
    // ...
});
```

With attributes, use `[Culture("it-IT")]` on the model.

A column with its own format provider uses that provider instead of `Culture`:

```c#
options.Columns.AddColumn(m => m.Price, new CultureInfo("en-US"));
```

To read and write the values with the culture of the machine, set `options.Culture = CultureInfo.CurrentCulture`.

> [!TIP]
> A file exported by Excel with an Italian, German or French locale uses `;` as delimiter and `,` as decimal separator:
> set both `Delimiter = ";"` and the `Culture` of the file.
