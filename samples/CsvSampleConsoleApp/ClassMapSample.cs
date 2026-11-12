namespace CsvSampleConsoleApp;

using Mafesoft.TinyCsv;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Balance { get; set; }
    public Address Address { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

/// <summary>
/// The columns and the options outside the model: the model has no attributes
/// </summary>
public sealed class CustomerMap : CsvClassMap<Customer>
{
    public CustomerMap()
    {
        Options(o =>
        {
            o.HasHeaderRecord = true;
            o.DetectDelimiter = true; // the files to read may use another delimiter
        });
        Map(m => m.Id);
        Map(m => m.Name).Name("customer_name");
        Map(m => m.Balance).Format("0.00");
        Map(m => m.Address.Street); // nested property: the column Address_Street
        Map(m => m.Address.City);   // the column Address_City
    }
}

/// <summary>
/// Class map, nested properties, delimiter detection and automatic columns
/// </summary>
public static class ClassMapSample
{
    public static void Run(string folder)
    {
        Console.WriteLine("== Class map");

        // a file with the pipe: the delimiter is detected
        var path = Path.Combine(folder, "customers.csv");
        File.WriteAllText(path,
            "Id|customer_name|Balance|Address_Street|Address_City\r\n" +
            "1|Mario Rossi|10.50|Via Roma 1|Firenze\r\n" +
            "2|\"Bianchi | Luigi\"|20.00|Via Milano 2|Milano\r\n");

        var csv = new TinyCsv<Customer>(new CustomerMap());
        var customers = csv.LoadFromFile(path).ToList();
        foreach (var customer in customers)
        {
            Console.WriteLine($"{customer.Id} {customer.Name} {customer.Balance} {customer.Address.City}");
        }

        // written with the default delimiter, the semicolon
        Console.WriteLine(csv.GetAllText(customers));

        // automatic columns: the public properties, nested ones included
        var automatic = new TinyCsv<Customer>(o => o.HasHeaderRecord = true);
        Console.WriteLine(automatic.GetAllText(customers));
        Console.WriteLine();
    }
}
