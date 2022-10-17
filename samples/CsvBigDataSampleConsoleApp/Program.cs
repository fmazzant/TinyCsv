// See https://aka.ms/new-console-template for more information

using TinyCsv;
using TinyCsv.Extensions;

List<double> timesReadInMilliseconds = new List<double>();
List<double> timesWriteInMilliseconds = new List<double>();

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
        timesReadInMilliseconds = new List<double>();
        timesWriteInMilliseconds = new List<double>();
    };

    DateTime readDateTime = DateTime.Now;
    options.Handlers.Read.RowReading += (s, e) =>
    {
        readDateTime = DateTime.Now;
    };
    options.Handlers.Read.RowRead += (s, e) =>
    {
        timesReadInMilliseconds.Add((DateTime.Now - readDateTime).TotalMilliseconds);
    };

    DateTime writeDateTime = DateTime.Now;
    options.Handlers.Write.RowWriting += (s, e) =>
    {
        writeDateTime = DateTime.Now;
    };
    options.Handlers.Write.RowWrittin += (s, e) =>
    {
        timesWriteInMilliseconds.Add((DateTime.Now - writeDateTime).TotalMilliseconds);
    };
});

var file = "C:\\Users\\fmazzant\\Desktop\\tabella.csv";
//var file = "C:\\Users\\fmazzant\\Desktop\\file_export_2.csv";
var file_export = "C:\\Users\\fmazzant\\Desktop\\file_export.csv";
string allText = "";
List<Model>? allList = null;

await RunWithTimeAsync("File.ReadAllText", async () =>
{
    allText = await File.ReadAllTextAsync(file).ConfigureAwait(false);
});
await RunWithTimeAsync("csv.LoadFromTextAsync", async () =>
{
    allList = await csv.LoadFromTextAsync(allText).ToListAsync().ConfigureAwait(false);
    Console.WriteLine($"Load Elements: {allList?.Count()}");
});

var avgRead = timesReadInMilliseconds.Average();
Console.WriteLine($"AVG READ => {avgRead}");

await RunWithTimeAsync("csv.SaveAsync", async () =>
{
    await csv.SaveAsync(file_export, allList);
    Console.WriteLine($"Save Elements: {allList?.Count()}");
});

var avgWrite = timesWriteInMilliseconds.Average();
Console.WriteLine($"AVG WRITE => {avgWrite}");


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

#endregion