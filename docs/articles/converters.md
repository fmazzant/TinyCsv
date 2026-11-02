# Value converters

A converter converts a value to its text in the file and back. Implement `IValueConverter`, from `TinyCsv.Conversions`:

```c#
using TinyCsv.Conversions;

public class Base64Converter : IValueConverter
{
    // value → text, used by Save
    public string Convert(object value, object parameter, IFormatProvider provider)
        => System.Convert.ToBase64String(Encoding.UTF8.GetBytes($"{value}"));

    // text → value, used by Load
    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        => Encoding.UTF8.GetString(System.Convert.FromBase64String(value));
}
```

and use it on a column:

```c#
options.Columns.AddColumn(m => m.Notes, new Base64Converter());
```

or with an attribute:

```c#
[Column(converter: typeof(Base64Converter))]
public string Notes { get; set; }
```

- `parameter` is the format of the column;
- `provider` is the format provider of the column, or the `Culture` option;
- `targetType` is the type of the property.

An exception thrown by `ConvertBack` is wrapped in an `InvalidColumnValueException`, with the row, the column and the value (see [Bad data](bad-data.md)).

## Example: a money value with a currency

```c#
public record Money(decimal Amount, string Currency);

public class MoneyConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider)
        => value is Money money ? $"{money.Amount.ToString(provider)} {money.Currency}" : string.Empty;

    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split(' ');
        return new Money(decimal.Parse(parts[0], provider), parts[1]);
    }
}
```

## Example: yes/no booleans

```c#
public class YesNoConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider)
        => value is true ? "yes" : "no";

    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        => string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}
```

## Built-in converters

The built-in converters are in `TinyCsv.Conversions`. The converters of numbers, dates, `bool`, `char` and `Guid`
derive from `ParsableValueConverter<T>`: with `ValidateValues`, a value that cannot be parsed is bad data, otherwise it is the default value.
