# Examples

Practical recipes. Each one is complete: copy it in a console app or a minimal API with the `Mafe.TinyCsv` package.

## Read an Excel export (Italian, German, French locale)

Excel saves the CSV with the separators of the machine: `;` between the values and `,` as decimal separator.

```csv
Codice;Descrizione;Prezzo;Data
A01;Tastiera;49,90;02/11/2026
A02;"Mouse; wireless";19,50;03/11/2026
```

```c#
public class Article
{
    public string Code { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
}

var csv = new TinyCsv<Article>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.Culture = new CultureInfo("it-IT");
    // Excel "CSV (delimitato dal separatore di elenco)" is saved with the ANSI code page, not UTF-8
    // ("CSV UTF-8" is UTF-8 with a byte order mark, the default TextEncoding reads it)
    options.TextEncoding = Encoding.Latin1;

    options.Columns.AddColumn(m => m.Code);
    options.Columns.AddColumn(m => m.Description);
    options.Columns.AddColumn(m => m.Price);
    options.Columns.AddColumn(m => m.Date, "dd/MM/yyyy");
});

var articles = csv.LoadFromFile("articoli.csv").ToList();
```

## Read a file with a preamble

Some exports have a few lines before the header:

```csv
Report: sales
Generated on: 2026-11-02

Id,Product,Quantity
1,Keyboard,10
2,Mouse,25
```

```c#
var csv = new TinyCsv<Sale>(options =>
{
    options.RowsToSkip = 3; // the preamble and the blank line
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
    options.SkipRow = (row, index) => string.IsNullOrWhiteSpace(row);
});
```

## Read a file whose columns change order

A supplier adds or moves columns: map them by name and ignore the others.

```c#
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
    options.MapColumnsByHeader = true;

    options.Columns.AddColumn("SKU", m => m.Id);
    options.Columns.AddColumn("Product name", m => m.Name);
    options.Columns.AddColumn("Unit price", m => m.Price);
});

try
{
    var products = csv.LoadFromFile("supplier.csv").ToList();
}
catch (MissingColumnException ex)
{
    Console.WriteLine($"The file has no column: {string.Join(", ", ex.MissingColumns)}");
}
```

## Validate an import and report the errors

Import the good rows and give the user the list of the bad ones:

```c#
var errors = new List<CsvException>();
var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
    options.ValidateValues = true;
    options.ValidateColumnCount = true;
    options.BadDataFound = error => errors.Add(error);
});

var products = csv.LoadFromFile("import.csv").ToList();

foreach (var error in errors)
{
    // i.e. "The value 'x' is not a valid Int32. (row 2, column Id)"
    //      "Invalid column count 2, required 3 columns. (row 3)"
    Console.WriteLine(error.Message);
}
```

## Process a large file without loading it in memory

`LoadFromFile` and `LoadFromFileAsync` read one record at a time:

```c#
var csv = new TinyCsv<Transaction>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
});

decimal total = 0;
long count = 0;
await foreach (var transaction in csv.LoadFromFileAsync("transactions.csv", cancellationToken))
{
    total += transaction.Amount;
    count++;
}
```

To insert the rows in a database, read them in batches:

```c#
foreach (var batch in csv.LoadFromFile("transactions.csv").Chunk(5_000))
{
    await db.Transactions.AddRangeAsync(batch);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
}
```

## Upload a CSV in ASP.NET Core

```c#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var csv = new TinyCsv<Product>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
    options.MapColumnsByHeader = true;
    options.ValidateValues = true;
});

app.MapPost("/products/import", async (IFormFile file, CancellationToken cancellationToken) =>
{
    var products = new List<Product>();
    var errors = new List<string>();

    using var stream = file.OpenReadStream();
    try
    {
        await foreach (var product in csv.LoadFromStreamAsync(stream, cancellationToken))
        {
            products.Add(product);
        }
    }
    catch (CsvException ex)
    {
        errors.Add(ex.Message);
    }

    return errors.Count == 0
        ? Results.Ok(new { imported = products.Count })
        : Results.BadRequest(errors);
}).DisableAntiforgery();

app.Run();
```

## Download a CSV from ASP.NET Core

```c#
app.MapGet("/products/export", async (AppDbContext db) =>
{
    var products = await db.Products.AsNoTracking().ToListAsync();
    var text = await csv.GetAllTextAsync(products);
    return Results.File(Encoding.UTF8.GetBytes(text), "text/csv", "products.csv");
});
```

To stream a large export, write directly to the response body:

```c#
app.MapGet("/products/export-stream", async (HttpContext context, AppDbContext db) =>
{
    context.Response.ContentType = "text/csv";
    context.Response.Headers.ContentDisposition = "attachment; filename=products.csv";
    await csv.SaveAsync(context.Response.Body, db.Products.AsNoTracking(), context.RequestAborted);
});
```

## Convert a CSV to another format

Read with a semicolon and the Italian culture, write with a comma and the invariant culture:

```c#
var input = new TinyCsv<Article>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.Culture = new CultureInfo("it-IT");
});

var output = new TinyCsv<Article>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
});

output.Save("articles_en.csv", input.LoadFromFile("articoli.csv"));
```

## Read a CSV without a model

```c#
var csv = new TinyCsv<object>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ",";
});

foreach (string[] fields in csv.GetAllLinesAndFieldsFromFile("unknown.csv"))
{
    Console.WriteLine(string.Join(" | ", fields));
}
```

## Unit test a mapping

Check that a model is written and read back as it is:

```c#
[Fact]
public void Product_RoundTrip()
{
    var csv = new TinyCsv<Product>(options =>
    {
        options.HasHeaderRecord = true;
        options.Delimiter = ",";
    });

    var expected = new[] { new Product { Id = 1, Name = "Mouse, \"wireless\"", Price = 19.5m } };

    var text = csv.GetAllText(expected);
    var actual = csv.LoadFromText(text).ToList();

    Assert.Equal(expected[0].Name, actual[0].Name);
    Assert.Equal(expected[0].Price, actual[0].Price);
}
```
