namespace CsvSampleConsoleApp;

using Mafesoft.TinyCsv;
using Mafesoft.TinyCsv.Exceptions;

/// <summary>
/// The rows with bad data are reported with their position, and the read goes on
/// </summary>
public static class BadData
{
    public class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
    }

    public static void Run()
    {
        Console.WriteLine("== Bad data");
        var errors = new List<CsvException>();
        var csv = new TinyCsv<Order>(options =>
        {
            options.HasHeaderRecord = true;
            options.ValidateValues = true;      // "abc" for an int is bad data, not 0
            options.ValidateColumnCount = true; // a row with a wrong column count is bad data
            options.BadDataFound = errors.Add;  // without it, the first bad row throws
            options.Columns.AddColumn(m => m.Id);
            options.Columns.AddColumn(m => m.Total);
        });

        var orders = csv.LoadFromText("Id;Total\r\n1;10.5\r\nabc;3\r\n3\r\n4;7.25").ToList();

        Console.WriteLine($"read: {string.Join(", ", orders.Select(o => o.Id))}");
        foreach (var error in errors)
        {
            Console.WriteLine($"row {error.RowIndex}: {error.Message}");
        }
        Console.WriteLine();
    }
}
