using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
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
    }

    public class MyIdConvert : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider)
        {
            var number = value as int? ?? 0;
            return $"{number + 10}";
        }

        public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        {
            var number = System.Convert.ToInt32(value);
            return number - 10;
        }
    }

    public class Program
    {
        static async Task Main(string[] args)
        {
            // definitions
            var csv = new TinyCsv<Model>(options =>
            {
                options.HasHeaderRecord = true;
                options.Delimiter = ";";
                options.Columns.AddColumn(m => m.Id, new MyIdConvert());
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Price);
                options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
            });

            // load from file
            var models = await csv.LoadAsync("file.csv");

            // write on file
            await csv.SaveAsync("file_export.csv", models);
        }
    }
}
