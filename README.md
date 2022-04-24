# TinyCsv

TinyCsv is a .NET library to read and write CSV data in an easy way. 

[![Nuget](https://img.shields.io/nuget/v/Mafe.TinyCvs?style=flat-square)](https://www.nuget.org/packages/Mafe.TinyCsv/1.0.0-preview1)
[![Nuget](https://img.shields.io/nuget/vpre/Mafe.TinyCvs?style=flat-square)](https://www.nuget.org/packages/Mafe.TinyCsv/1.0.0-preview1)

Define the model if you want to use, like this:

```c#
public class Model
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedOn { get; set; }
}
```

Now, is necessary to define the TinyCSV options, like this:

```c#
var csv = new TinyCsv<Model>(options =>
{
    options.HasHeaderRecord = true;
    options.Delimiter = ";";
    options.Columns.AddColumn(m => m.Id);
    options.Columns.AddColumn(m => m.Name);
    options.Columns.AddColumn(m => m.Price);
    options.Columns.AddColumn(m => m.CreatedOn, "dd/MM/yyyy");
});
```
The options defines that the file has the header in first row and the delimier char is ";", furthermore there are defined three columns.

A column is defined with the model's property only. The library get or set the value in automatically.

To read a csv file is only necessary invoke the load method, like this:

```c#
var models = csv.Load("file.csv");
```

The load method returns a collection of Model type items and takes as argument the file's path.

To write a csv file is only necessary invoke the Save method, like this:

```c#
csv.Save("file_export.csv", models);
```
The save method takes the file's path and a collection of Model type. 
The method saves the file.

The library is very very simple to use.

The library is in preview version and in the future days it will be upgrade to expand its functionality.
