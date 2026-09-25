namespace TinyCsv.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TinyCsv.Data;
    using TinyCsv.Streams;
    using Xunit;

    /// <summary>
    /// Covers every public load/save entry point of TinyCsv
    /// </summary>
    public class ApiTests : IDisposable
    {
        private const string Text = "Id;Name;City\n1;Mario;Roma\n2;\"Rossi; Luigi\";Milano";

        private static readonly Person[] People =
        {
            new Person { Id = 1, Name = "Mario", City = "Roma" },
            new Person { Id = 2, Name = "Rossi; Luigi", City = "Milano" },
        };

        private readonly string path = Path.Combine(Path.GetTempPath(), $"tinycsv-{Guid.NewGuid():N}.csv");

        public void Dispose()
        {
            File.Delete(path);
        }

        private static TinyCsv<Person> Csv() => Tests.Csv.Person(o =>
        {
            o.HasHeaderRecord = true;
            o.NewLine = "\n";
        });

        private static void AssertPeople(System.Collections.Generic.IEnumerable<Person> result)
        {
            Assert.Equal(People.Select(p => (p.Id, p.Name, p.City)), result.Select(p => (p.Id, p.Name, p.City)));
        }

        private static MemoryStream Stream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

        private static string ReadText(MemoryStream stream) => Encoding.UTF8.GetString(stream.ToArray()).TrimStart('﻿');

        // ---------- load ----------

        [Fact]
        public void LoadFromStream_StreamReader()
        {
            AssertPeople(Csv().LoadFromStream(new StreamReader(Stream(Text))).ToList());
        }

        [Fact]
        public void LoadFromStream_Stream()
        {
            AssertPeople(Csv().LoadFromStream(Stream(Text)).ToList());
        }

        [Fact]
        public void LoadFromText_WithEncoding()
        {
            var csv = Tests.Csv.Person(o => o.HasHeaderRecord = true);

            AssertPeople(csv.LoadFromText(Text, Encoding.Unicode).ToList());
        }

        [Fact]
        public void LoadFromText_UsesOptionsEncoding()
        {
            var csv = Tests.Csv.Person(o => o.TextEncoding = Encoding.Unicode);

            Assert.Equal("Niccolò", csv.LoadFromText("1;Niccolò;Forlì").Single().Name);
        }

        [Fact]
        public void LoadFromFile_IsLazyAndReleasesFile()
        {
            File.WriteAllText(path, Text);

            var result = Csv().LoadFromFile(path);
            AssertPeople(result.ToList());

            File.Delete(path);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void LoadFromFile_BreakEarly_ReleasesFile()
        {
            File.WriteAllText(path, Text);

            var first = Csv().LoadFromFile(path).First();

            Assert.Equal("Mario", first.Name);
            File.Delete(path);
        }

        [Fact]
        public void LoadFromFile_MissingFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => Csv().LoadFromFile(path));
        }

#pragma warning disable CS0618 // obsolete API are still supported
        [Fact]
        public void Obsolete_Load()
        {
            File.WriteAllText(path, Text);

            AssertPeople(Csv().Load(path).ToList());
            AssertPeople(Csv().Load(new StreamReader(Stream(Text))).ToList());
        }
#pragma warning restore CS0618

        // ---------- lines and fields ----------

        [Fact]
        public void GetAllLines_FromFileStreamAndText()
        {
            File.WriteAllText(path, Text);
            var expected = new[] { "Id;Name;City", "1;Mario;Roma", "2;\"Rossi; Luigi\";Milano" };
            var csv = Csv();

            Assert.Equal(expected, csv.GetAllLinesFromFile(path).ToList());
            Assert.Equal(expected, csv.GetAllLinesFromStream(Stream(Text)).ToList());
            Assert.Equal(expected, csv.GetAllLinesFromStream(new StreamReader(Stream(Text))).ToList());
            Assert.Equal(expected, csv.GetAllLinesFromText(Text).ToList());
        }

        [Fact]
        public void GetAllLinesAndFields_FromFileStreamAndText()
        {
            File.WriteAllText(path, Text);
            var expected = new[]
            {
                new[] { "Id", "Name", "City" },
                new[] { "1", "Mario", "Roma" },
                new[] { "2", "Rossi; Luigi", "Milano" },
            };
            var csv = Csv();

            Assert.Equal(expected, csv.GetAllLinesAndFieldsFromFile(path).ToList());
            Assert.Equal(expected, csv.GetAllLinesAndFieldsFromStream(Stream(Text)).ToList());
            Assert.Equal(expected, csv.GetAllLinesAndFieldsFromStream(new StreamReader(Stream(Text))).ToList());
            Assert.Equal(expected, csv.GetAlGetAllLinesAndFieldslLinesFromText(Text).ToList());
        }

        // ---------- save ----------

        [Fact]
        public void Save_ToFile_ReleasesFile()
        {
            Csv().Save(path, People);

            Assert.Equal(Text + "\n", File.ReadAllText(path).Replace("\r\n", "\n"));
            File.Delete(path);
        }

        [Fact]
        public void Save_ToStreamWriter()
        {
            var stream = new MemoryStream();

            Csv().Save(new StreamWriter(stream), People);

            AssertPeople(Csv().LoadFromText(ReadText(stream)).ToList());
        }

        [Fact]
        public async Task SaveAsync_ToFile_ReleasesFile()
        {
            await Csv().SaveAsync(path, People);

            AssertPeople(Csv().LoadFromText(File.ReadAllText(path)).ToList());
            File.Delete(path);
        }

        [Fact]
        public async Task SaveAsync_ToStreamAndStreamWriter()
        {
            var stream1 = new MemoryStream();
            var stream2 = new MemoryStream();

            await Csv().SaveAsync(stream1, People);
            await Csv().SaveAsync(new StreamWriter(stream2), People);

            AssertPeople(Csv().LoadFromText(ReadText(stream1)).ToList());
            AssertPeople(Csv().LoadFromText(ReadText(stream2)).ToList());
        }

        [Fact]
        public async Task SaveAsync_Cancelled_Throws()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Csv().SaveAsync(new StreamWriter(new MemoryStream()), People, cts.Token));
            }
        }

        [Fact]
        public async Task GetAllLinesAndTextAsync()
        {
            var csv = Csv();

            var lines = await csv.GetAllLinesAsync(People);
            var text = await csv.GetAllTextAsync(People);

            Assert.Equal(csv.GetAllLines(People), lines);
            Assert.Equal(Text, text);
        }

        [Fact]
        public async Task GetAllLinesAsync_Cancelled_Throws()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Csv().GetAllLinesAsync(People, cts.Token));
            }
        }

        [Fact]
        public void GetAllLines_Empty()
        {
            Assert.Equal(new[] { "Id;Name;City" }, Csv().GetAllLines(new Person[0]));
            Assert.Empty(Tests.Csv.Person().GetAllLines(new Person[0]));
        }

        // ---------- async load (framework: Task<IEnumerable<T>>) ----------

#if !NET5_0_OR_GREATER
        [Fact]
        public async Task Framework_LoadAsync()
        {
            File.WriteAllText(path, Text);
            var csv = Csv();

            AssertPeople(await csv.LoadFromTextAsync(Text));
            AssertPeople(await csv.LoadFromStreamAsync(Stream(Text)));
            AssertPeople(await csv.LoadFromStreamAsync(new StreamReader(Stream(Text))));
            AssertPeople(await csv.LoadFromFileAsync(path));
#pragma warning disable CS0618
            AssertPeople(await csv.LoadAsync(path));
            AssertPeople(await csv.LoadAsync(new StreamReader(Stream(Text))));
#pragma warning restore CS0618
            File.Delete(path);
        }
#endif

        // ---------- data reader / writer ----------

        [Fact]
        public void DataReader_Direct()
        {
            var options = Tests.Csv.Person().Options;
            var reader = new TinyCsvDataReader<Person>(options, new StreamReader(Stream("1;a;b\n2;\"c\nd\";e")));

            Assert.Equal(new[] { "1;a;b", "2;\"c\nd\";e" }, reader.ReadLines().ToList());
            Assert.Equal(new[] { "2", "c\nd", "e" }, reader.GetFieldsByLine("2;\"c\nd\";e", 3));
            Assert.Equal(new[] { "x", "y" }, reader.GetFieldsByLine("x;y"));
            Assert.Empty(reader.GetFieldsByLine(string.Empty, 3));
        }

        [Fact]
        public void DataReader_ReadLinesAndFields_SkipsBlankLines()
        {
            var options = Tests.Csv.Person().Options;
            var reader = new TinyCsvDataReader<Person>(options, new StreamReader(Stream("1;a\n  \n2;b")));

            Assert.Equal(new[] { new[] { "1", "a" }, new[] { "2", "b" } }, reader.ReadLinesAndFields().ToList());
        }

        [Fact]
        public void DataWriter_WriteLinesAndFields()
        {
            var options = Tests.Csv.Person().Options;
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream) { NewLine = "\n" };
            var writer = new CsvDataWriter<Person>(options, streamWriter);

            Assert.Equal(1, writer.WriteLine("a;b"));
            Assert.Equal(2, writer.WriteLines(new[] { "c;d", "e;f" }));
            Assert.Equal(1, writer.WriteLinesAndFields(new[] { new[] { "g", "h" } }));
            writer.Flush();

            Assert.Equal("a;b\nc;d\ne;f\ng;h\n", ReadText(stream));
        }

        [Fact]
        public async Task DataWriter_WriteAsync()
        {
            var options = Tests.Csv.Person().Options;
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream) { NewLine = "\n" };
            var writer = new CsvDataWriter<Person>(options, streamWriter);

            Assert.Equal(1, await writer.WriteLineAsync("a;b"));
            Assert.Equal(2, await writer.WriteLinesAsync(new[] { "c;d", "e;f" }));
            Assert.Equal(1, await writer.WriteLinesAndFieldsAsync(new[] { new[] { "g", "h" } }));
            await writer.FlushAsync();

            Assert.Equal("a;b\nc;d\ne;f\ng;h\n", ReadText(stream));
        }

        [Fact]
        public async Task DataWriter_Cancelled_Throws()
        {
            var writer = new CsvDataWriter<Person>(Tests.Csv.Person().Options, new StreamWriter(new MemoryStream()));
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteLineAsync("a", cts.Token));
            }
        }

        [Fact]
        public void TextMemoryStream_EncodesText()
        {
            using (var stream = new TextMemoryStream("àè"))
            using (var unicode = new TextMemoryStream("àè", Encoding.Unicode))
            {
                Assert.Equal(Encoding.UTF8.GetBytes("àè"), stream.ToArray());
                Assert.Equal(Encoding.Unicode.GetBytes("àè"), unicode.ToArray());
            }
        }
    }
}
