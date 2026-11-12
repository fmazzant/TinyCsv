namespace CsvSampleConsoleApp;

using Mafesoft.TinyCsv;
using Mafesoft.TinyCsv.Conversions;

public enum RowType { A, B }

public class Product
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public string TextBase64 { get; set; } = "";
    public Uri? WebSite { get; set; }
    public RowType RowType { get; set; }
}

/// <summary>
/// A custom converter: the column is written as base64 and read back as plain text
/// </summary>
public class Base64Converter : IValueConverter
{
    public string? Convert(object? value, object? parameter, IFormatProvider provider)
        => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{value}"));

    public object? ConvertBack(string value, Type targetType, object? parameter, IFormatProvider provider)
        => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(value));
}

/// <summary>
/// Reads file.csv (header, comments, quoted values, empty rows), writes it back, and copies it one record at a time
/// </summary>
public static class ReadAndWrite
{
    private static TinyCsv<Product> Csv() => new(options =>
    {
        options.HasHeaderRecord = true;
        options.Delimiter = ";";
        options.SkipRow = (row, index) => string.IsNullOrWhiteSpace(row); // the comments (#) are skipped by default
        options.TrimData = true;

        options.Columns.AddColumn(m => m.Id);
        options.Columns.AddColumn(m => m.Name);
        options.Columns.AddColumn(m => m.Price);
        options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
        options.Columns.AddColumn(m => m.TextBase64, new Base64Converter());
        options.Columns.AddColumn(m => m.WebSite);
        options.Columns.AddColumn(m => m.RowType);
    });

    public static async Task RunAsync(string folder)
    {
        Console.WriteLine("== Read and write");
        var csv = Csv();

        // read: one record at a time
        var products = csv.LoadFromFile("file.csv").ToList();
        foreach (var product in products.Take(3))
        {
            Console.WriteLine($"{product.Id} {product.Name} {product.Price} {product.CreatedOn:d} {product.TextBase64} {product.RowType}");
        }

        // write
        var path = Path.Combine(folder, "products.csv");
        csv.Save(path, products);
        Console.WriteLine($"{products.Count} products written to {path}");

        // async: stream from one file to another, without loading it in memory
        var copy = Path.Combine(folder, "products-copy.csv");
        await using (var writer = csv.CreateWriter(copy))
        {
            await foreach (var product in csv.LoadFromFileAsync(path))
            {
                product.Name = product.Name.ToUpperInvariant();
                await writer.WriteRecordAsync(product);
            }
        }
        Console.WriteLine($"copied to {copy}");
        Console.WriteLine();
    }
}
