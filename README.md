# TinyCsv

TinyCsv is a .NET library to read and write CSV data in an easy way. 

To use it in your project, add the Mafe.TinyCsv NuGet package to your project.

[![Nuget](https://img.shields.io/nuget/v/Mafe.TinyCsv)](https://www.nuget.org/packages/Mafe.TinyCsv/1.2.0)

Define the model that you want to use, like this:

```c#
public class Model
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public string TextBase64 { get; set; }
}
```

Now, is necessary to define the TinyCSV options, like this:

```c#
var csv = new TinyCsv<Model>(options =>
{
    // Options
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.RowsToSkip = 0;
    options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
    options.TrimData = true;
    options.SkipEmptyRows = false;
    options.AllowRowEnclosedInDoubleQuotesValues = true;

    // Columns
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
    options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
    options.Columns.AddColumn(m => m.TextBase64, new Base64Converter());
});
```
The options defines that the file has the header in first row and the delimier char is ";", furthermore there are defined some columns.
RowsToSkip and SkipRow are used to skip the first rows of the file.
TrimData is used to remove the white spaces from the data.

## Custom Value Converter

It is possible to define a custom converter for a column, like this:

```c#
public class Base64Converter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider) 
        => Base64Encode($"{value}");

    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) 
        => Base64Decode(value);

    private string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    private string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }
}
```

Base64Converter is used to convert the data column to a Base64 string and vice versa for TextBase64 column.

A column is defined with the model's property only. The library get or set the value in automatically.

## Read

To read a csv file is only necessary invoke the load method, like this:

```c#
var models = csv.Load("file.csv");
```

or you can to use the asynchronously method, like this:

```c#
var models = await csv.LoadAsync("file.csv");
```

The load method returns a collection of Model type items and takes as argument the file's path.

## Write

To write a csv file is only necessary invoke the Save method, like this:

```c#
csv.Save("file_export.csv", models);
```

or you can to use the asynchronously method, like this:

```c#
await csv.SaveAsync("file_export.csv", models);
```

The save method takes the file's path and a collection of Model type. 
The method saves the file.

## Event handlers 

It's possible to define event handlers for the events of the library, like this:

```c#
options.Handlers.Read.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
options.Handlers.Read.RowReading += (s, e) => Console.WriteLine($"{e.Index}-{e.Row}");
options.Handlers.Read.RowRead += (s, e) => Console.WriteLine($"{e.Index}-{e.Model}");
```

and the write handlers are:

```c#
options.Handlers.Write.RowHeader += (s, e) => Console.WriteLine($"Row header: {e.RowHeader}");
options.Handlers.Write.RowWriting += (s, e) => Console.WriteLine($"{e.Index} - {e.Model}");
options.Handlers.Write.RowWrittin += (s, e) => Console.WriteLine($"{e.Index} - {e.Row}");```
```

The library is very very simple to use.


