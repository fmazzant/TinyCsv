// TinyCsv in a minimal API: a typed singleton, named (keyed) instances and ITinyCsvFactory.
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

var app = builder.Build();

app.MapGet("/", () => "TinyCsv sample: /model1, /model2, /model2-it, /model2.csv");

// the records are streamed to the response (IAsyncEnumerable<T>)
app.MapGet("/model1", (ITinyCsv<Model1> csv) => csv.LoadFromFileAsync("model1.csv"));

app.MapGet("/model2", (ITinyCsvFactory factory) => factory.Get<Model2>("model2").LoadFromFileAsync("model2.csv"));

app.MapGet("/model2-it", ([FromKeyedServices("model2-it")] ITinyCsv<Model2> csv) => csv.LoadFromFileAsync("model2.csv"));

// writes a CSV file as response
app.MapGet("/model2.csv", async (HttpContext context, ITinyCsvFactory factory) =>
{
    var csv = factory.Get<Model2>("model2");
    context.Response.ContentType = "text/csv; charset=utf-8";
    context.Response.Headers.ContentDisposition = "attachment; filename=model2.csv";
    await csv.SaveAsync(context.Response.Body, csv.LoadFromFileAsync("model2.csv"), context.RequestAborted);
});

app.Run();
