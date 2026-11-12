// TinyCsv with attributes: the options and the columns are defined on the model.
using Mafesoft.TinyCsv;
using Mafesoft.TinyCsv.Attributes;
using Mafesoft.TinyCsv.Conversions;

var csv = new TinyCsv<AttributeModel>();

// read: one record at a time
foreach (var model in csv.LoadFromFile("file.csv"))
{
    Console.WriteLine($"{model.Id} - {model.Name} - {model.Price} - {model.CreatedOn:d} - {model.TextBase64}");
}

// async read and write
var path = Path.Combine(Path.GetTempPath(), "attribute-models.csv");
await csv.SaveAsync(path, csv.LoadFromFileAsync("file.csv"));
Console.WriteLine($"Written to {path}:");
Console.WriteLine(File.ReadAllText(path));

[Delimiter(";")]
[SkipRow(typeof(SkipEmptyRows))]
[TrimData(true)]
[HasHeaderRecord(true)]
public class AttributeModel
{
    [Column]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = "";

    [Column]
    public decimal Price { get; set; }

    [Column(format: "dd/MM/yyyy")]
    public DateTime CreatedOn { get; set; }

    [Column(converter: typeof(Base64Converter))]
    public string TextBase64 { get; set; } = "";
}

/// <summary>
/// Skips the empty rows (the comments, starting with #, are skipped by default)
/// </summary>
public class SkipEmptyRows : ISkipRow
{
    public Func<string, int, bool> SkipRow { get; } = (row, index) => string.IsNullOrWhiteSpace(row);
}

/// <summary>
/// The column is written as base64 and read back as plain text
/// </summary>
public class Base64Converter : IValueConverter
{
    public string? Convert(object? value, object? parameter, IFormatProvider provider)
        => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{value}"));

    public object? ConvertBack(string value, Type targetType, object? parameter, IFormatProvider provider)
        => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(value));
}
