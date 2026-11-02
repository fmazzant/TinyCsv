# Dependency injection

The `Mafe.TinyCsv.Extensions` package registers the `TinyCsv<T>` instances in an `IServiceCollection`:

```shell
dotnet add package Mafe.TinyCsv.Extensions
```

```c#
using TinyCsv.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTinyCsv<Product>("Products", options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});

var app = builder.Build();

app.MapGet("/products", (ITinyCsvFactory factory) =>
{
    var csv = factory.Get<Product>("Products");
    return csv.LoadFromFile("products.csv");
});

app.Run();
```

- `AddTinyCsv<T>(name, options)` creates a `TinyCsv<T>` and registers it with a name, which is a unique key;
- `ITinyCsvFactory` is a singleton: `Get<T>(name)` returns the registered instance, `Create<T>(options)` creates a new one.

`Mafe.TinyCsv.AspNetCore` has the same API: `AddTinyCsv` is in the `TinyCsv.AspNetCore.Extensions` namespace and `ITinyCsvFactory` in the `TinyCsv` namespace.
Since 2.4.0 it depends only on `Microsoft.Extensions.DependencyInjection`, like `Mafe.TinyCsv.Extensions`.

See the [examples](examples.md) for an upload and a download endpoint.
