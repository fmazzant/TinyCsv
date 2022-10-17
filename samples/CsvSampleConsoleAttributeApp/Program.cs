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

namespace CsvSampleConsoleAttributeApp
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TinyCsv;
    using TinyCsv.Attributes;
    using TinyCsv.Conversions;
    using TinyCsv.Extensions;

    [Delimiter(";")]
    [RowsToSkip(0)]
    [SkipRow(typeof(CustomSkipRow))]
    [TrimData(true)]
    [ValidateColumnCount(true)]
    [HasHeaderRecord(true)]
    public class AttributeModel
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public decimal Price { get; set; }

        [Column(format: "dd/MM/yyyy")]
        public DateTime CreatedOn { get; set; }

        [Column(converter: typeof(Base64Converter))]
        public string TextBase64 { get; set; }

        public override string ToString()
        {
            return $"ToString: {Id}, {Name}, {Price}, {CreatedOn}, {TextBase64}";
        }
    }

    class CustomSkipRow : ISkipRow
    {
        public Func<string, int, bool> SkipRow { get; } = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
    }

    class Base64Converter : IValueConverter
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


    internal class Program
    {
        static async Task Main(string[] args)
        {
            // definitions
            var csv = new TinyCsv<AttributeModel>();

            // read from memory stream
            using (var memoryStream = Memory.CreateMemoryStream(Environment.NewLine))
            {
                var memoryModels = csv.LoadFromStream(memoryStream);
                Console.WriteLine($"Count:{memoryModels.Count()}");
            }

            // read from text
            var planText = $"Id;Name;Price;CreatedOn;TextBase64;{Environment.NewLine}\"1\";\"   Name 1   \";\"1.12\";02/04/2022;\"aGVsbG8sIHdvcmxkIQ == \";";
            var textModels = csv.LoadFromText(planText);
            Console.WriteLine($"Count:{textModels.Count()}");

            // read from file sync
            var syncModels = csv.LoadFromFile("file.csv");
            Console.WriteLine($"Count:{syncModels.Count()}");

            // read from file async
            var asyncModels = await csv.LoadFromFileAsync("file.csv");
            Console.WriteLine($"Count:{asyncModels.Count()}");

            // returns IAsyncEnumerable
            await foreach (var model in csv.LoadFromStreamAsync(new StreamReader("file.csv")))
            {
                Console.WriteLine($"{model.Id} - {model.Name} - {model.Price} - {model.CreatedOn} - {model.TextBase64}");
            }

            // load IAsyncEnumerable into a list
            var models = await csv.LoadFromStreamAsync(new StreamReader("file.csv")).ToListAsync();
            Console.WriteLine($"Count:{models.Count}");

            // write on file async
            await csv.SaveAsync("file_export.csv", models);

        }

        static class Memory
        {
            public static MemoryStream CreateMemoryStream(string newline)
            {
                var planText = $"Id;Name;Price;CreatedOn;TextBase64;{newline}\"1\";\"   Name 1   \";\"1.12\";02/04/2022;\"aGVsbG8sIHdvcmxkIQ == \";";
                var bytes = Encoding.ASCII.GetBytes(planText);
                var memoryStream = new MemoryStream(bytes);
                return memoryStream;
            }
        }
    }
}
