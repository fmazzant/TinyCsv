/// <summary>
/// 
/// The MIT License (MIT)
/// 
/// Copyright (c) 2022 Federico Mazzanti
/// 
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
/// 
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
/// 
/// </summary>

namespace CsvSampleConsoleApp
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TinyCsv;
    using TinyCsv.Conversions;
    using TinyCsv.Extensions;

    public enum RowType { A, B }

    public class Model
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TextBase64 { get; set; }
        public Uri WebSite { get; set; }
        public RowType RowType { get; set; }
        public override string ToString()
        {
            return $"Object -> {Id}, {Name}, {Price}, {CreatedOn}, {TextBase64}, {WebSite}, {RowType}";
        }
    }

    public class BigModel
    {
        public string Year { get; set; }
        public string Age { get; set; }
        public string Ethnic { get; set; }
        public string Sex { get; set; }
        public string Area { get; set; }
        public string Count { get; set; }

    }

    public class Base64Converter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider) => Base64Encode($"{value}");
        public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => Base64Decode(value);

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }

    internal static class Program
    {
        static async Task Main()
        {
            var bigFileRun = true;
            if (bigFileRun)
            {
                // definitions
                var csvBig = new TinyCsv<BigModel>(bigOptions =>
                {
                    // Options
                    bigOptions.HasHeaderRecord = true;
                    bigOptions.Delimiter = ",";
                    bigOptions.RowsToSkip = 0;
                    bigOptions.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
                    bigOptions.TrimData = true;
                    bigOptions.ValidateColumnCount = false;
                    bigOptions.EnableHandlers = false;

                    //Year,Age,Ethnic,Sex,Area,count
                    //2018,000,1,1,01,795
                    bigOptions.Columns.AddColumn(m => m.Year);
                    bigOptions.Columns.AddColumn(m => m.Age);
                    bigOptions.Columns.AddColumn(m => m.Ethnic);
                    bigOptions.Columns.AddColumn(m => m.Sex);
                    bigOptions.Columns.AddColumn(m => m.Area);
                    bigOptions.Columns.AddColumn(m => m.Count);

                    // Event Handlers Read
                    bigOptions.Handlers.Read.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
                    bigOptions.Handlers.Read.RowReading += (s, e) => Console.WriteLine($"{e.Index}-{e.Row}");
                    bigOptions.Handlers.Read.RowRead += (s, e) => Console.WriteLine($"{e.Index}-{e.Model}");

                    // Event Handlers Write
                    bigOptions.Handlers.Write.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
                    bigOptions.Handlers.Write.RowWriting += (s, e) => Console.WriteLine($"{e.Index} - {e.Model}");
                    bigOptions.Handlers.Write.RowWrittin += (s, e) => Console.WriteLine($"{e.Index} - {e.Row}");
                });

                int index = 0;
                var now = DateTime.Now;
                var tdt = now;
                var pdt = now;
                var temporary = csvBig.LoadFromFile("../../../../../../../../Data8277.csv");
                foreach (var t in temporary)
                {
                    index++;
                    if (index % 1000000 == 0)
                    {
                        Console.WriteLine($"-> {index} in {(DateTime.Now - pdt).TotalMilliseconds} ms");
                        pdt = DateTime.Now;
                    }
                }
                Console.WriteLine($"-> {(DateTime.Now - tdt).TotalSeconds}");

                index = 0;
                now = DateTime.Now;
                tdt = now;
                pdt = now;
                await foreach (var r in csvBig.LoadFromFileAsync("../../../../../../../../Data8277.csv"))
                {
                    index++;
                    if (index % 1000000 == 0)
                    {
                        Console.WriteLine($"-> {index} in {(DateTime.Now - pdt).TotalMilliseconds} ms");
                        pdt = DateTime.Now;
                    }
                }
                Console.WriteLine($"-> {(DateTime.Now - tdt).TotalSeconds}");
            }

            var csv = new TinyCsv<Model>(options =>
            {
                // Options
                options.HasHeaderRecord = true;
                options.Delimiter = ";";
                options.RowsToSkip = 0;
                options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
                options.TrimData = true;
                options.ValidateColumnCount = false;
                options.EnableHandlers = false;

                // Columns
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Price);
                options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
                options.Columns.AddColumn(m => m.TextBase64, new Base64Converter());
                options.Columns.AddColumn(m => m.WebSite);
                options.Columns.AddColumn(m => m.RowType);

                // Event Handlers Read
                options.Handlers.Read.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
                options.Handlers.Read.RowReading += (s, e) => Console.WriteLine($"{e.Index}-{e.Row}");
                options.Handlers.Read.RowRead += (s, e) => Console.WriteLine($"{e.Index}-{e.Model}");

                // Event Handlers Write
                options.Handlers.Write.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
                options.Handlers.Write.RowWriting += (s, e) => Console.WriteLine($"{e.Index} - {e.Model}");
                options.Handlers.Write.RowWrittin += (s, e) => Console.WriteLine($"{e.Index} - {e.Row}");
            });

            // read from file sync
            var syncModels = csv.LoadFromFile("file.csv");
            Console.WriteLine($"Count:{syncModels.Count()}");

            // read from memory stream
            using (var memoryStream = Memory.CreateMemoryStream(Environment.NewLine))
            {
                var memoryModels = csv.LoadFromStream(memoryStream);
                var list = memoryModels.ToList();
                Console.WriteLine($"Count:{list.Count()}");
            }

            // read from text
            var planText = $"Id;Name;Price;CreatedOn;TextBase64;{Environment.NewLine}\"1\";\"   Name 1   \";\"1.12\";02/04/2022;\"aGVsbG8sIHdvcmxkIQ == \";https://wwww.google.it;A;";
            var textModels = csv.LoadFromText(planText);
            Console.WriteLine($"Count:{textModels.Count()}");

            // read from file async
            var asyncModels = await csv.LoadFromFileAsync("file.csv").ToListAsync();
            Console.WriteLine($"Count:{asyncModels.Count()}");

            // returns IAsyncEnumerable
            await foreach (var model in csv.LoadFromStreamAsync(new StreamReader("file.csv")))
            {
                Console.WriteLine($"{model.Id} - {model.Name} - {model.Price} - {model.CreatedOn} - {model.TextBase64}");
            }

            // load IAsyncEnumerable into a list
            var models = await csv.LoadFromStreamAsync(new StreamReader("file.csv")).ToListAsync();
            Console.WriteLine($"Count:{models.Count()}");

            // get all lines from file
            var lines = csv.GetAllLinesFromFile("file.csv");
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            // load to fix 
            var allText = File.ReadAllText("file.csv");
            var loadAsList = await csv.LoadFromTextAsync(allText).ToListAsync();

            // write on file async
            await csv.SaveAsync("file_export.csv", models);
        }

        public static class Memory
        {
            public static MemoryStream CreateMemoryStream(string newline)
            {
                var planText = $"Id;Name;Price;CreatedOn;TextBase64;{newline}\"1\";\"   Name 1   \";\"1.12\";02/04/2022;\"aGVsbG8sIHdvcmxkIQ == \";https://www.google.it;B;";
                var bytes = Encoding.ASCII.GetBytes(planText);
                var memoryStream = new MemoryStream(bytes);
                return memoryStream;
            }
        }
    }
}
