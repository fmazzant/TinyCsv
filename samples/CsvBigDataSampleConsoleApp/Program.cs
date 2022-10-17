// See https://aka.ms/new-console-template for more information

using TinyCsv;
using TinyCsv.Extensions;

List<string> times = new List<string>();
List<double> timesInMilliseconds = new List<double>();

var csv = new TinyCsv<Model>(options =>
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


    options.Handlers.Start += (s, e) =>
    {
        times = new List<string>();
        timesInMilliseconds = new List<double>();
    };

    DateTime d = DateTime.Now;
    options.Handlers.Read.RowReading += (s, e) =>
    {
        d = DateTime.Now;
    };
    options.Handlers.Read.RowRead += (s, e) =>
    {
        times.Add($"{e.Model.Id} => {(DateTime.Now - d).TotalMilliseconds}ms");
        timesInMilliseconds.Add((DateTime.Now - d).TotalMilliseconds);
    };
});

var file = "C:\\Users\\fmazzant\\Desktop\\tabella.csv";
var file_export = "C:\\Users\\fmazzant\\Desktop\\file_export.csv";
string allText = "";
List<Model>? allList = null;

await RunWithTimeAsync("File.ReadAllText", async () =>
{
    allText = File.ReadAllText(file);
});
await RunWithTimeAsync("csv.LoadFromFile", async () =>
{
    allList = await csv.LoadFromTextAsync(allText).ToListAsync();
    Console.WriteLine($"Load Elements: {allList.Count()}");
});
await RunWithTimeAsync("csv.SaveAsync", async () =>
{
    await csv.SaveAsync(file_export, allList);
    Console.WriteLine($"Save Elements: {allList.Count()}");
});

var avg = timesInMilliseconds.Average();
Console.WriteLine($"AVG => {avg}");
Console.WriteLine("....");

async Task RunWithTimeAsync(string name, Func<Task> action)
{
    var start = DateTime.Now;
    await action();
    var end = DateTime.Now;
    Console.WriteLine($"{name} => {(end - start).TotalMilliseconds}");
}


#region [ Models ]

public class Model
{
    public string Event { get; set; }
    public string Operation { get; set; }
    public string Note { get; set; }
    public string IP { get; set; }
    public Guid Id { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string DeletedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public int IsDeleted { get; set; }
}

#endregion