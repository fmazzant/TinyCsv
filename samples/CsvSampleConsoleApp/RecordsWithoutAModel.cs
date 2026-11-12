namespace CsvSampleConsoleApp;

using System.Data;
using Mafesoft.TinyCsv;

/// <summary>
/// Reads a file whose columns are not known: as dictionaries, dynamic objects or a DataTable
/// </summary>
public static class RecordsWithoutAModel
{
    public static async Task RunAsync(string folder)
    {
        Console.WriteLine("== Records without a model");
        var path = Path.Combine(folder, "customers.csv"); // written by ClassMapSample
        var records = new TinyCsvRecords(options => options.DetectDelimiter = true);

        foreach (var record in records.LoadFromFile(path))
        {
            Console.WriteLine(string.Join(", ", record.Select(field => $"{field.Key}={field.Value}")));
        }

        foreach (var record in records.LoadDynamicFromFile(path))
        {
            Console.WriteLine($"{record.customer_name} lives in {record.Address_City}");
        }

        await foreach (var record in records.LoadFromFileAsync(path))
        {
            Console.WriteLine($"async: {record["customer_name"]}");
        }

        DataTable table = records.LoadDataTableFromFile(path);
        Console.WriteLine($"DataTable: {table.Columns.Count} columns, {table.Rows.Count} rows");
        Console.WriteLine();
    }
}
