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
    using System.Threading.Tasks;
    using TinyCsv;
    using TinyCsv.Conversions;
    using TinyCsv.Extensions;

    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TextBase64 { get; set; }

        public override string ToString()
        {
            return $"ToString: {Id}, {Name}, {Price}, {CreatedOn}, {TextBase64}";
        }
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
            // definitions
            var csv = new TinyCsv<Model>(options =>
            {
                // Options
                options.HasHeaderRecord = true;
                options.Delimiter = ";";
                options.RowsToSkip = 0;
                options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
                options.TrimData = true;
                options.SkipEmptyRows = false;

                // Columns
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Price);
                options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
                options.Columns.AddColumn(m => m.TextBase64, new Base64Converter());

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
            var syncModels = csv.Load("file.csv");
            Console.WriteLine($"Count:{syncModels.Count}");

            // read from file async
            var asyncModels = await csv.LoadAsync("file.csv");
            Console.WriteLine($"Count:{asyncModels.Count}");

            // returns IAsyncEnumerable
            await foreach (var model in csv.LoadAsync(new StreamReader("file.csv")))
            {
                Console.WriteLine($"{model.Id}");
            }

            // load IAsyncEnumerable into a list
            var models = await csv.LoadAsync(new StreamReader("file.csv")).ToListAsync();
            Console.WriteLine($"Count:{models.Count}");
            
            // write on file async
            await csv.SaveAsync("file_export.csv", models);
        }
    }
}
