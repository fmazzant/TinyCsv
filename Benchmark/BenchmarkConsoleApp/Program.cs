using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Text;
using TinyCsv;

namespace BenchmarkConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LoadFromFileBenchmarker>();
        }
    }

    public class LoadFromFileBenchmarker: Benchmarker
    {
        [Params("C:\\Users\\fmazzant\\Desktop\\tabella.csv", "C:\\Users\\fmazzant\\Desktop\\tabella2.csv")]
        public string? fileName;

        [Benchmark]
        public object LoadFromFile() => Csv().LoadFromFile(fileName);
    }


    public abstract class Benchmarker
    {
        protected TinyCsv<Model> Csv()
        {
            return new TinyCsv<Model>(options =>
            {
                // Options
                options.HasHeaderRecord = true;
                options.Delimiter = ",";
                options.RowsToSkip = 0;
                options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
                options.TrimData = true;
                options.ValidateColumnCount = true;

                // Columns
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Operation);
                options.Columns.AddColumn(m => m.Event);
                options.Columns.AddColumn(m => m.IP);
                options.Columns.AddColumn(m => m.Note);
                options.Columns.AddColumn(m => m.CreatedBy);
                options.Columns.AddColumn(m => m.CreatedOn);
                options.Columns.AddColumn(m => m.ModifiedBy);
                options.Columns.AddColumn(m => m.ModifiedOn);
                options.Columns.AddColumn(m => m.DeletedBy);
                options.Columns.AddColumn(m => m.DeletedOn);
                options.Columns.AddColumn(m => m.IsDeleted);
            });
        }
    }

    public class Model
    {
        public string? Event { get; set; }
        public string? Operation { get; set; }
        public string? Note { get; set; }
        public string? IP { get; set; }
        public Guid Id { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? IsDeleted { get; set; }
    }
}