// TinyCsv in a minimal API: a typed singleton, named (keyed) instances and ITinyCsvFactory,
// CSV downloads and uploads, the MVC formatters and the ProblemDetails of a bad upload.
using Mafesoft.TinyCsv;
using Mafesoft.TinyCsv.AspNetCore;
using Mafesoft.TinyCsv.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Models;

var builder = WebApplication.CreateBuilder(args);

// one instance per model: inject ITinyCsv<Model1>
builder.Services.AddTinyCsv<Model1>(options =>
{
    options.HasHeaderRecord = true;
    options.SkipRow = (row, index) => string.IsNullOrWhiteSpace(row); // the comments (#) are skipped by default
    options.ValidateValues = true; // a value that cannot be converted (i.e. "x" for Id) throws, instead of the default value
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});

// named instances: keyed services on .NET 8+, ITinyCsvFactory.Get everywhere
builder.Services
    .AddTinyCsv<Model2>("model2", ConfigureModel2)
    .AddTinyCsv<Model2>("model2-it", options =>
    {
        ConfigureModel2(options);
        options.Culture = new System.Globalization.CultureInfo("it-IT");
    });

static void ConfigureModel2(CsvOptions<Model2> options)
{
    options.HasHeaderRecord = true;
    options.SkipRow = (row, index) => string.IsNullOrWhiteSpace(row);
    options.Columns.AddColumn(0, m => m.Id);
    options.Columns.AddColumn(1, m => m.Name);
    options.Columns.AddColumn(3, m => m.CreatedOn, "dd/MM/yyyy");
}

// MVC: text/csv input and output formatters (see Controllers/Model1Controller.cs)
builder.Services.AddControllers().AddTinyCsvFormatters();

// a CsvException not handled by an endpoint (i.e. a bad upload) becomes a 400 ProblemDetails with the row and the column
builder.Services.AddTinyCsvProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", () => "TinyCsv sample: /model1, /model2, /model2-it, /model2.csv, /model1.csv, POST /model1/import, POST /model1/import-body, /api/model1");

// the records are streamed to the response (IAsyncEnumerable<T>)
app.MapGet("/model1", (ITinyCsv<Model1> csv) => csv.LoadFromFileAsync("model1.csv"));

app.MapGet("/model2", (ITinyCsvFactory factory) => factory.Get<Model2>("model2").LoadFromFileAsync("model2.csv"));

app.MapGet("/model2-it", LoadModel2It);

// writes a CSV file as response, by hand
app.MapGet("/model2.csv", async (HttpContext context, ITinyCsvFactory factory) =>
{
    var csv = factory.Get<Model2>("model2");
    context.Response.ContentType = "text/csv; charset=utf-8";
    context.Response.Headers.ContentDisposition = "attachment; filename=model2.csv";
    await csv.SaveAsync(context.Response.Body, csv.LoadFromFileAsync("model2.csv"), context.RequestAborted);
});

// the same with the CSV result: text/csv, Content-Disposition and the records streamed one at a time
app.MapGet("/model1.csv", (ITinyCsv<Model1> csv) =>
    Results.Extensions.Csv(csv, csv.LoadFromFileAsync("model1.csv"), fileDownloadName: "model1.csv"));

// upload of a file (multipart/form-data): the records are read one at a time;
// a bad row throws a CsvException, that AddTinyCsvProblemDetails turns into a 400 ProblemDetails
app.MapPost("/model1/import", async (IFormFile file, ITinyCsv<Model1> csv, CancellationToken cancellationToken) =>
{
    var count = 0;
    decimal total = 0;
    await foreach (var model in csv.LoadFromFormFileAsync(file, cancellationToken))
    {
        count++;
        total += model.Price;
    }
    return Results.Ok(new { count, total });
}).DisableAntiforgery();

// a text/csv request body; the bad data is handled here, with Results.Extensions.CsvProblem
app.MapPost("/model1/import-body", async (HttpRequest request, ITinyCsv<Model1> csv) =>
{
    try
    {
        var models = new List<Model1>();
        await foreach (var model in csv.LoadFromRequestAsync(request))
        {
            models.Add(model);
        }
        return Results.Ok(models);
    }
    catch (Mafesoft.TinyCsv.Exceptions.CsvException exception)
    {
        return Results.Extensions.CsvProblem(exception);
    }
});

app.MapControllers();

app.Run();

// a local function: the keyed service is resolved through the parameter attribute
static IAsyncEnumerable<Model2> LoadModel2It([FromKeyedServices("model2-it")] ITinyCsv<Model2> csv)
    => csv.LoadFromFileAsync("model2.csv");
