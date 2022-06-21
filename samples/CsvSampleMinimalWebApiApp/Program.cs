/// <summary>
/// 
/// The MIT License (MIT)
/// 
/// Copyright (c) 2022 Federico Mazzanti
/// 
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
/// 
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
/// 
/// </summary>

using Microsoft.AspNetCore.Authorization;
using Models;
using TinyCsv.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddTinyCsv<Model1>("Model1", options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
});
builder.Services.AddTinyCsv<Model2>("Model2", options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.CreatedOn);
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/", [AllowAnonymous] (HttpContext http) =>
{
    return "Hello, world!";
});

app.MapGet("/csv1", [AllowAnonymous] async (HttpContext http, ITinyCsvFactory tinyCsvFactory) =>
{
    var tinyCsv = tinyCsvFactory.Get<Model1>("Model1");
    var result = await tinyCsv.LoadFromFileAsync("model1.csv");
    Console.WriteLine($"{result?.Count}");
    return result;
});

app.MapGet("/csv2", [AllowAnonymous] async (HttpContext http, ITinyCsvFactory tinyCsvFactory) =>
{
    var tinyCsv = tinyCsvFactory.Get<Model2>("Model2");
    var result = await tinyCsv.LoadFromFileAsync("model2.csv");
    Console.WriteLine($"{result?.Count}");
    return result;
});

app.Run();