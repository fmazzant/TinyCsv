// TinyCsv console samples: one file for each topic.
// Run all of them with: dotnet run --project samples/CsvSampleConsoleApp
using CsvSampleConsoleApp;

var folder = Path.Combine(Path.GetTempPath(), "tinycsv-samples");
Directory.CreateDirectory(folder);

await ReadAndWrite.RunAsync(folder);
ClassMapSample.Run(folder);
await RecordsWithoutAModel.RunAsync(folder);
BadData.Run();

Console.WriteLine($"The files are in {folder}");
