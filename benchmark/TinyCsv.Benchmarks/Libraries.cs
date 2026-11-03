using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using nietras.SeparatedValues;
using Sylvan.Data;
using Sylvan.Data.Csv;

namespace TinyCsv.Benchmarks;

/// <summary>
/// Reads and writes the models with each library, in the way suggested by its documentation.
/// TinyCsv and CsvHelper map the columns to the properties, Sylvan and Sep are used with a hand written mapping.
/// </summary>
public static partial class Libraries
{
    private const string DateFormat = "yyyy-MM-dd";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    // TinyCsv

    public static readonly TinyCsv<Model> TinyCsv = new(options =>
    {
        options.HasHeaderRecord = true;
        options.Delimiter = ";";
        options.Columns.AddColumn(m => m.Id);
        options.Columns.AddColumn(m => m.Name);
        options.Columns.AddColumn(m => m.Email);
        options.Columns.AddColumn(m => m.Price);
        options.Columns.AddColumn(m => m.Quantity);
        options.Columns.AddColumn(m => m.CreatedOn, DateFormat);
        options.Columns.AddColumn(m => m.Active);
        options.Columns.AddColumn(m => m.Notes);
    });

    public static IEnumerable<Model> ReadTinyCsv(string text) => TinyCsv.LoadFromText(text);

    public static void WriteTinyCsv(Stream stream, Model[] models) => TinyCsv.Save(stream, models);

    // CsvHelper

    private static readonly CsvConfiguration CsvHelperConfiguration = new(Invariant) { Delimiter = ";" };

    public static IEnumerable<Model> ReadCsvHelper(string text)
    {
        using var csv = new CsvReader(new StringReader(text), CsvHelperConfiguration);
        foreach (var model in csv.GetRecords<Model>())
        {
            yield return model;
        }
    }

    public static void WriteCsvHelper(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        using var csv = new CsvWriter(writer, CsvHelperConfiguration);
        csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = [DateFormat];
        csv.WriteRecords(models);
    }

    // Sylvan

    private static readonly CsvDataReaderOptions SylvanReaderOptions = new() { Delimiter = ';' };
    private static readonly CsvDataWriterOptions SylvanWriterOptions = new() { Delimiter = ';', DateTimeFormat = DateFormat };

    public static IEnumerable<Model> ReadSylvan(string text)
    {
        using var csv = Sylvan.Data.Csv.CsvDataReader.Create(new StringReader(text), SylvanReaderOptions);
        while (csv.Read())
        {
            yield return new Model
            {
                Id = csv.GetInt32(0),
                Name = csv.GetString(1),
                Email = csv.GetString(2),
                Price = csv.GetDecimal(3),
                Quantity = csv.GetInt32(4),
                CreatedOn = csv.GetDateTime(5),
                Active = csv.GetBoolean(6),
                Notes = csv.GetString(7),
            };
        }
    }

    public static void WriteSylvan(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        using var csv = Sylvan.Data.Csv.CsvDataWriter.Create(writer, SylvanWriterOptions);
        csv.Write(models.AsDataReader());
    }

    // Sep

    public static IEnumerable<Model> ReadSep(string text)
    {
        using var reader = Sep.Reader(o => o with { Sep = new Sep(';'), Unescape = true }).FromText(text);
        foreach (var row in reader)
        {
            yield return new Model
            {
                Id = row[0].Parse<int>(),
                Name = row[1].ToString(),
                Email = row[2].ToString(),
                Price = row[3].Parse<decimal>(),
                Quantity = row[4].Parse<int>(),
                CreatedOn = DateTime.ParseExact(row[5].Span, DateFormat, Invariant),
                Active = bool.Parse(row[6].Span),
                Notes = row[7].ToString(),
            };
        }
    }

    public static void WriteSep(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        using var csv = Sep.Writer(o => o with { Sep = new Sep(';'), Escape = true }).To(writer, leaveOpen: true);
        Span<char> date = stackalloc char[16];
        foreach (var m in models)
        {
            using var row = csv.NewRow();
            row["Id"].Format(m.Id);
            row["Name"].Set(m.Name);
            row["Email"].Set(m.Email);
            row["Price"].Format(m.Price);
            row["Quantity"].Format(m.Quantity);
            m.CreatedOn.TryFormat(date, out var written, DateFormat, Invariant);
            row["CreatedOn"].Set(date.Slice(0, written));
            row["Active"].Set(m.Active ? "True" : "False");
            row["Notes"].Set(m.Notes);
        }
    }
}
