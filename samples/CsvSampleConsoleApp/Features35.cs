namespace CsvSampleConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv;

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public Address Address { get; set; }

        public override string ToString() => $"{Id} - {Name} - {Balance} - {Address?.Street}, {Address?.City}";
    }

    /// <summary>
    /// The columns and the options of the customers, outside the model: the model has no attributes.
    /// </summary>
    public sealed class CustomerMap : CsvClassMap<Customer>
    {
        public CustomerMap()
        {
            Options(o =>
            {
                o.HasHeaderRecord = true;
                o.Delimiter = ";";
                o.DetectDelimiter = true; // the files to read may use another delimiter
            });
            Map(m => m.Id);
            Map(m => m.Name).Name("customer_name");
            Map(m => m.Balance).Format("0.00");
            Map(m => m.Address.Street); // the column Address_Street
            Map(m => m.Address.City);   // the column Address_City
        }
    }

    /// <summary>
    /// The examples of the features of TinyCsv 3.5
    /// </summary>
    internal static class Features35
    {
        // a file exported with the pipe as delimiter
        private const string PipeText =
            "Id|customer_name|Balance|Address_Street|Address_City\n" +
            "1|Mario Rossi|10.50|Via Roma 1|Firenze\n" +
            "2|\"Bianchi | Luigi\"|20.00|Via Milano 2|Milano\n" +
            "3|Anna Verdi|0.75||";

        public static async Task RunAsync()
        {
            DetectTheDelimiter();
            NestedPropertiesWithAutomaticColumns();
            ClassMap();
            await WriteOneRecordAtATimeAsync();
            RecordsWithoutAModel();
        }

        /// <summary>
        /// The delimiter is detected from the first records: the same options read comma, semicolon, tab and pipe files.
        /// </summary>
        private static void DetectTheDelimiter()
        {
            Console.WriteLine("--- Detect the delimiter");
            var csv = new TinyCsv<ModelOnlyName>(options =>
            {
                options.HasHeaderRecord = true;
                options.DetectDelimiter = true;
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Price);
            });

            foreach (var text in new[]
            {
                "Id,Name,Price\n1,Comma,1.5",
                "Id;Name;Price\n2;Semicolon;2.5",
                "Id\tName\tPrice\n3\tTab\t3.5",
                "Id|Name|Price\n4|Pipe|4.5",
            })
            {
                Console.WriteLine(csv.LoadFromText(text).Single());
            }
        }

        /// <summary>
        /// Without columns, the properties of Address are columns too: Address_Street and Address_City.
        /// </summary>
        private static void NestedPropertiesWithAutomaticColumns()
        {
            Console.WriteLine("--- Nested properties");
            var csv = new TinyCsv<Customer>(options =>
            {
                options.HasHeaderRecord = true;
                options.Delimiter = ",";
            });

            var customers = new[]
            {
                new Customer { Id = 1, Name = "Mario Rossi", Balance = 10.5m, Address = new Address { Street = "Via Roma 1", City = "Firenze" } },
                new Customer { Id = 2, Name = "Anna Verdi", Balance = 0.75m }, // a null address is written as empty values
            };

            var text = csv.GetAllText(customers);
            Console.WriteLine(text);

            // when reading, the address is created
            foreach (var customer in csv.LoadFromText(text))
            {
                Console.WriteLine(customer);
            }
        }

        /// <summary>
        /// The class map defines the columns and the options; the pipe file is read because the map detects the delimiter.
        /// </summary>
        private static void ClassMap()
        {
            Console.WriteLine("--- Class map");
            var csv = new TinyCsv<Customer>(new CustomerMap());

            foreach (var customer in csv.LoadFromText(PipeText))
            {
                Console.WriteLine(customer);
            }

            // the map and other options: the written file uses the comma
            var commaCsv = new TinyCsv<Customer>(options =>
            {
                options.UseClassMap<CustomerMap>();
                options.Delimiter = ",";
            });
            Console.WriteLine(commaCsv.GetAllText(csv.LoadFromText(PipeText)));
        }

        /// <summary>
        /// The records are written one at a time, so a large file is never in memory: here a pipe file is converted to a semicolon file.
        /// </summary>
        private static async Task WriteOneRecordAtATimeAsync()
        {
            Console.WriteLine("--- Write one record at a time");
            var csv = new TinyCsv<Customer>(new CustomerMap());
            var path = "customers_export.csv";

            await using (var writer = csv.CreateWriter(path))
            {
                await foreach (var customer in csv.LoadFromTextAsync(PipeText))
                {
                    customer.Name = customer.Name.ToUpperInvariant();
                    await writer.WriteRecordAsync(customer);
                }
                Console.WriteLine($"Rows written (header included): {writer.Count}");
            }

            // a generated sequence, with the synchronous writer on a stream
            using (var stream = new MemoryStream())
            {
                using (var writer = csv.CreateWriter(stream))
                {
                    foreach (var customer in Generate(100_000))
                    {
                        writer.WriteRecord(customer);
                    }
                }
                Console.WriteLine($"Generated file: {stream.Length:N0} bytes");
            }

            Console.WriteLine(File.ReadAllText(path));
        }

        private static IEnumerable<Customer> Generate(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                yield return new Customer { Id = i, Name = $"Customer {i}", Balance = i / 100m, Address = new Address { City = "Firenze" } };
            }
        }

        /// <summary>
        /// A file whose columns are not known: as dictionaries, as dynamic objects and into a DataTable.
        /// </summary>
        private static void RecordsWithoutAModel()
        {
            Console.WriteLine("--- Records without a model");
            var csv = new TinyCsvRecords(options => options.DetectDelimiter = true);

            foreach (var record in csv.LoadFromText(PipeText))
            {
                Console.WriteLine(string.Join(", ", record.Select(field => $"{field.Key}={field.Value}")));
            }

            foreach (var record in csv.LoadDynamicFromText(PipeText))
            {
                Console.WriteLine($"{record.customer_name} lives in {record.Address_City}");
            }

            DataTable table = csv.LoadDataTableFromText(PipeText);
            Console.WriteLine($"DataTable: {table.Columns.Count} columns, {table.Rows.Count} rows");
            foreach (DataRow row in table.Rows)
            {
                Console.WriteLine($"{row["Id"]} - {row["customer_name"]}");
            }
        }
    }
}
