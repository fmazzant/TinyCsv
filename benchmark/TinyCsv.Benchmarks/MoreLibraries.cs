using System.Globalization;
using FileHelpers;
using Microsoft.VisualBasic.FileIO;
using RecordParser.Builders.Reader;
using RecordParser.Builders.Writer;
using RecordParser.Extensions;
using RecordParser.Parsers;

namespace TinyCsv.Benchmarks;

/// <summary>
/// The other libraries: FileHelpers, TinyCsvParser and RecordParser map the columns to the properties (or fields),
/// while Csv, NReco.Csv, LumenWorks and TextFieldParser return the fields as strings, mapped by hand by <see cref="Parse"/>.
/// </summary>
public static partial class Libraries
{
    // FileHelpers: the record is a class with attributes and fields, copied to the model

    public static IEnumerable<Model> ReadFileHelpers(string text)
    {
        using var engine = new FileHelperAsyncEngine<FileHelpersModel>();
        using (engine.BeginReadString(text))
        {
            foreach (var record in engine)
            {
                yield return record.ToModel();
            }
        }
    }

    public static void WriteFileHelpers(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        using var engine = new FileHelperAsyncEngine<FileHelpersModel> { HeaderText = string.Join(Data.Delimiter, Data.Header) };
        using (engine.BeginWriteStream(writer))
        {
            foreach (var model in models)
            {
                engine.WriteNext(FileHelpersModel.FromModel(model));
            }
        }
    }

    // TinyCsvParser

    private static readonly global::TinyCsvParser.CsvParser<Model> TinyCsvParserReader =
        new(global::TinyCsvParser.Models.CsvOptions.Rfc4180 with { Delimiter = Data.Delimiter, SkipHeader = true }, new TinyCsvParserMapping());

    public static IEnumerable<Model> ReadTinyCsvParser(string text)
    {
        foreach (var result in TinyCsvParserReader.ReadFromString(text))
        {
            if (result.TryGetResult(out var model))
            {
                yield return model!;
            }
        }
    }

    // RecordParser

    private static readonly IVariableLengthReader<Model> RecordParserReader = new VariableLengthReaderBuilder<Model>()
        .Map(m => m.Id, indexColumn: 0)
        .Map(m => m.Name, indexColumn: 1)
        .Map(m => m.Email, indexColumn: 2)
        .Map(m => m.Price, indexColumn: 3)
        .Map(m => m.Quantity, indexColumn: 4)
        .Map(m => m.CreatedOn, indexColumn: 5, span => DateTime.ParseExact(span, DateFormat, Invariant))
        .Map(m => m.Active, indexColumn: 6)
        .Map(m => m.Notes, indexColumn: 7)
        .Build(";", Invariant);

    private static readonly IVariableLengthWriter<Model> RecordParserWriter = new VariableLengthWriterSequentialBuilder<Model>()
        .Map(m => m.Id)
        .Map(m => m.Name)
        .Map(m => m.Email)
        .Map(m => m.Price)
        .Map(m => m.Quantity)
        .Map(m => m.CreatedOn, DateFormat)
        .Map(m => m.Active)
        .Map(m => m.Notes)
        .Build(";", Invariant);

    private static readonly VariableLengthReaderOptions RecordParserOptions = new()
    {
        HasHeader = true,
        ContainsQuotedFields = true,
        ParallelismOptions = new() { Enabled = false },
    };

    public static IEnumerable<Model> ReadRecordParser(string text)
    {
        using var reader = new StringReader(text);
        foreach (var model in reader.ReadRecords(RecordParserReader, RecordParserOptions))
        {
            yield return model;
        }
    }

    public static void WriteRecordParser(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        writer.WriteLine(string.Join(Data.Delimiter, Data.Header));
        writer.WriteRecords(models, RecordParserWriter.TryFormat, new ParallelismOptions { Enabled = false });
    }

    // Csv

    private static readonly Csv.CsvOptions CsvOptions = new() { Separator = Data.Delimiter, AllowNewLineInEnclosedFieldValues = true };

    public static IEnumerable<Model> ReadCsv(string text)
    {
        foreach (var line in Csv.CsvReader.ReadFromText(text, CsvOptions))
        {
            yield return Parse(line.Values);
        }
    }

    public static void WriteCsv(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        Csv.CsvWriter.Write(writer, Data.Header, models.Select(Format), Data.Delimiter);
    }

    // NReco.Csv

    public static IEnumerable<Model> ReadNReco(string text)
    {
        var csv = new NReco.Csv.CsvReader(new StringReader(text), ";");
        var fields = new string[Data.Header.Length];
        csv.Read(); // the header
        while (csv.Read())
        {
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = csv[i];
            }
            yield return Parse(fields);
        }
    }

    public static void WriteNReco(Stream stream, Model[] models)
    {
        using var writer = new StreamWriter(stream, Utf8, 4096, leaveOpen: true);
        var csv = new NReco.Csv.CsvWriter(writer, ";");
        WriteFields(csv, Data.Header);
        foreach (var model in models)
        {
            WriteFields(csv, Format(model));
        }
    }

    private static void WriteFields(NReco.Csv.CsvWriter csv, string[] fields)
    {
        foreach (var field in fields)
        {
            csv.WriteField(field);
        }
        csv.NextRecord();
    }

    // LumenWorks

    public static IEnumerable<Model> ReadLumenWorks(string text)
    {
        using var csv = new LumenWorks.Framework.IO.Csv.CsvReader(new StringReader(text), true, Data.Delimiter);
        var fields = new string[Data.Header.Length];
        while (csv.ReadNextRecord())
        {
            csv.CopyCurrentRecordTo(fields);
            yield return Parse(fields);
        }
    }

    // TextFieldParser, in the .NET runtime (Microsoft.VisualBasic.FileIO)

    public static IEnumerable<Model> ReadTextFieldParser(string text)
    {
        using var parser = new TextFieldParser(new StringReader(text))
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = [";"],
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
        };
        parser.ReadFields(); // the header
        while (parser.ReadFields() is { } fields)
        {
            yield return Parse(fields);
        }
    }

    // The hand written mapping of the libraries that return strings

    private static Model Parse(string[] fields) => new()
    {
        Id = int.Parse(fields[0], Invariant),
        Name = fields[1],
        Email = fields[2],
        Price = decimal.Parse(fields[3], Invariant),
        Quantity = int.Parse(fields[4], Invariant),
        CreatedOn = DateTime.ParseExact(fields[5], DateFormat, Invariant),
        Active = bool.Parse(fields[6]),
        Notes = fields[7],
    };

    private static string[] Format(Model m) =>
    [
        m.Id.ToString(Invariant),
        m.Name ?? "",
        m.Email ?? "",
        m.Price.ToString(Invariant),
        m.Quantity.ToString(Invariant),
        m.CreatedOn.ToString(DateFormat, Invariant),
        m.Active ? "True" : "False",
        m.Notes ?? "",
    ];
}

/// <summary>
/// The mapping of TinyCsvParser
/// </summary>
public sealed class TinyCsvParserMapping : global::TinyCsvParser.CsvMapping<Model>
{
    public TinyCsvParserMapping()
    {
        MapProperty(0, m => m.Id);
        MapProperty(1, m => m.Name);
        MapProperty(2, m => m.Email);
        MapProperty(3, m => m.Price);
        MapProperty(4, m => m.Quantity);
        MapProperty(5, m => m.CreatedOn, new global::TinyCsvParser.TypeConverters.DateTimeConverter("yyyy-MM-dd", CultureInfo.InvariantCulture));
        MapProperty(6, m => m.Active);
        // the string converter does not unescape the double quotes of a quoted value, CsvRow.GetString does
        MapUsing((Model model, ref global::CsvRow row) =>
        {
            model.Notes = row.GetString(7);
            return global::TinyCsvParser.Models.MapUsingResult.Success();
        });
    }
}

/// <summary>
/// The record of FileHelpers: FileHelpers maps the fields of a class with attributes
/// </summary>
[DelimitedRecord(";")]
[IgnoreFirst(1)]
public sealed class FileHelpersModel
{
    public int Id;

    [FieldQuoted('"', QuoteMode.OptionalForBoth, MultilineMode.AllowForBoth)]
    public string? Name;

    [FieldQuoted('"', QuoteMode.OptionalForBoth, MultilineMode.AllowForBoth)]
    public string? Email;

    [FieldConverter(ConverterKind.Decimal, ".")]
    public decimal Price;

    public int Quantity;

    [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime CreatedOn;

    [FieldConverter(ConverterKind.Boolean, "True", "False")]
    public bool Active;

    [FieldQuoted('"', QuoteMode.OptionalForBoth, MultilineMode.AllowForBoth)]
    public string? Notes;

    public Model ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Email = Email,
        Price = Price,
        Quantity = Quantity,
        CreatedOn = CreatedOn,
        Active = Active,
        Notes = Notes,
    };

    public static FileHelpersModel FromModel(Model m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Email = m.Email,
        Price = m.Price,
        Quantity = m.Quantity,
        CreatedOn = m.CreatedOn,
        Active = m.Active,
        Notes = m.Notes,
    };
}
