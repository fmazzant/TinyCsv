# Mafesoft.TinyCsv.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/Mafesoft.TinyCsv.AspNetCore?style=flat-square&label=nuget)](https://www.nuget.org/packages/Mafesoft.TinyCsv.AspNetCore)
[![Downloads](https://img.shields.io/nuget/dt/Mafesoft.TinyCsv.AspNetCore?style=flat-square&label=downloads)](https://www.nuget.org/packages/Mafesoft.TinyCsv.AspNetCore)
[![Build](https://img.shields.io/github/actions/workflow/status/fmazzant/TinyCsv/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/fmazzant/TinyCsv/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/fmazzant/TinyCsv/blob/main/LICENSE.md)

[![Mafe.TinyCsv.AspNetCore downloads](https://img.shields.io/nuget/dt/Mafe.TinyCsv.AspNetCore?style=flat-square&label=Mafe.TinyCsv.AspNetCore%20downloads)](https://www.nuget.org/packages/Mafe.TinyCsv.AspNetCore)

Registers [Mafesoft.TinyCsv](https://www.nuget.org/packages/Mafesoft.TinyCsv) in the ASP.NET Core dependency injection.
It depends only on `Microsoft.Extensions.DependencyInjection.Abstractions`.

Target frameworks: .NET 10, .NET 8, .NET Standard 2.0.

## Quick start

```shell
dotnet add package Mafesoft.TinyCsv.AspNetCore
```

```c#
using Mafesoft.TinyCsv;
using Mafesoft.TinyCsv.AspNetCore;
using Mafesoft.TinyCsv.AspNetCore.Extensions;

// one instance per model: inject ITinyCsv<Order>
builder.Services.AddTinyCsv<Order>(o => o.HasHeaderRecord = true);

// named instances: keyed services on .NET 8+, ITinyCsvFactory everywhere
builder.Services
    .AddTinyCsv<Order>("orders-it", o => o.Delimiter = ";")
    .AddTinyCsv<Order>("orders-us", o => o.Delimiter = ",");

app.MapGet("/orders/it", ([FromKeyedServices("orders-it")] ITinyCsv<Order> csv) => csv.LoadFromFileAsync("orders-it.csv"));
app.MapGet("/orders/us", (ITinyCsvFactory factory) => factory.Get<Order>("orders-us").LoadFromFileAsync("orders-us.csv"));
```

The instances are singletons of the service provider: two service providers do not share them.

## Support

If Mafesoft.TinyCsv.AspNetCore saves you time, you can say thanks with a small donation. It is never required, but it helps me keep the project maintained and up to date.

[![Donate](https://img.shields.io/badge/donate-PayPal-00457C?style=flat-square&logo=paypal&logoColor=white)](https://paypal.me/fmazzant)

## Links

- [Documentation](https://github.com/fmazzant/TinyCsv#aspnet-core)
- [Migrating to 4.0](https://github.com/fmazzant/TinyCsv#migrating-to-40) (from `Mafe.TinyCsv.AspNetCore` and `Mafe.TinyCsv.Extensions`)
- [Changelog](https://github.com/fmazzant/TinyCsv/blob/main/CHANGELOG.md)
- [Issues](https://github.com/fmazzant/TinyCsv/issues)

Licensed under the [MIT License](https://github.com/fmazzant/TinyCsv/blob/main/LICENSE.md).
