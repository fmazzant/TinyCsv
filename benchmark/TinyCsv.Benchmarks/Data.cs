using System.Globalization;
using System.Text;

namespace Mafesoft.TinyCsv.Benchmarks;

/// <summary>
/// The model used by every library: 8 columns of different types.
/// </summary>
public sealed class Model
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool Active { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// The data set: the header and N rows separated by ';', with the invariant culture.
/// One row out of 10 has a quoted Notes value, with the delimiter and an escaped quote inside.
/// </summary>
public static class Data
{
    public const char Delimiter = ';';

    public static readonly string[] Header = ["Id", "Name", "Email", "Price", "Quantity", "CreatedOn", "Active", "Notes"];

    public static Model[] CreateModels(int count)
    {
        var models = new Model[count];
        var date = new DateTime(2022, 4, 2, 0, 0, 0, DateTimeKind.Unspecified);
        for (int i = 0; i < count; i++)
        {
            models[i] = new Model
            {
                Id = i + 1,
                Name = $"Name {i + 1}",
                Email = $"user{i + 1}@mywebsite.it",
                Price = Math.Round(1.12m + i % 1000 * 0.37m, 2),
                Quantity = i % 50,
                CreatedOn = date.AddDays(i % 365),
                Active = i % 3 != 0,
                Notes = i % 10 == 0 ? $"note; with \"delimiter\" {i}" : $"note {i}",
            };
        }
        return models;
    }

    /// <summary>
    /// The CSV text of the models, written by hand with RFC 4180 escaping (independent from the libraries).
    /// </summary>
    public static string CreateText(Model[] models)
    {
        var builder = new StringBuilder(models.Length * 96);
        builder.Append(string.Join(Delimiter, Header)).Append("\r\n");
        foreach (var m in models)
        {
            builder.Append(m.Id.ToString(CultureInfo.InvariantCulture)).Append(Delimiter)
                .Append(m.Name).Append(Delimiter)
                .Append(m.Email).Append(Delimiter)
                .Append(m.Price.ToString(CultureInfo.InvariantCulture)).Append(Delimiter)
                .Append(m.Quantity.ToString(CultureInfo.InvariantCulture)).Append(Delimiter)
                .Append(m.CreatedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(Delimiter)
                .Append(m.Active ? "True" : "False").Append(Delimiter)
                .Append(Escape(m.Notes!))
                .Append("\r\n");
        }
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.IndexOfAny([Delimiter, '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
