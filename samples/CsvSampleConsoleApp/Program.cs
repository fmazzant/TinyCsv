using System;
using System.IO;
using System.Threading.Tasks;
using TinyCsv;
using TinyCsv.Conversions;

namespace CsvSampleConsoleApp
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TextBase64 { get; set; }
    }

    public class Base64Converter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider) => Base64Encode($"{value}");
        public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => Base64Decode(value);

        private string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        private string Base64Decode(string base64EncodedData)
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
            var csv = new TinyCsv<Model>(options =>
            {
                options.HasHeaderRecord = true;
                options.Delimiter = ";";
                options.RowsToSkip = 0;
                options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
                options.TrimData = true;
                options.SkipEmptyRows = false;
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Price);
                options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
                options.Columns.AddColumn(m => m.TextBase64, new Base64Converter());
            });

            // Sync
            var models_Load = csv.Load("file.csv");
            foreach (var model in models_Load)
            {
                Console.WriteLine($"{model.Id} {model.Name}");
            }

            // Sync with stream
            using (var streamReader = new StreamReader("file.csv"))
            {
                var models = csv.Load(streamReader);
                foreach (var model in models)
                {
                    Console.WriteLine($"{model.Id} {model.Name}");
                }
            }


            // load from file
            var modelsAsync = await csv.LoadAsync("file.csv");

            // write on file
            await csv.SaveAsync("file_export.csv", modelsAsync);
        }
    }
}
