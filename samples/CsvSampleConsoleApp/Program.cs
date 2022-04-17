using System;
using TinyCsv;

namespace CsvSampleConsoleApp
{
    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // definitions
            var csv = new TinyCsv<Model>(options =>
            {
                options.HasHeaderRecord = true;
                options.Delimiter = ";";
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Price);
            });

            // load from file
            var models = csv.Load("file.csv");

            // write on file
            csv.Save("file_export.csv", models);

        }
    }
}
