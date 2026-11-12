namespace Mafesoft.TinyCsv.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Attributes;
    using Xunit;

    /// <summary>
    /// Pins the defaults of 4.0: NewLine CRLF on every operating system, Delimiter ";" and UTF-8 without BOM
    /// </summary>
    public class DefaultsTests
    {
        private static readonly Person[] People =
        {
            new Person { Id = 1, Name = "Mario", City = "Roma" },
            new Person { Id = 2, Name = "Luigi", City = "Milano" },
        };

        private static TinyCsv<Person> Csv() => new TinyCsv<Person>(o =>
        {
            o.HasHeaderRecord = true;
            o.Columns.AddColumn(m => m.Id);
            o.Columns.AddColumn(m => m.Name);
            o.Columns.AddColumn(m => m.City);
        });

        private const string Expected = "Id;Name;City\r\n1;Mario;Roma\r\n2;Luigi;Milano\r\n";

        [Fact]
        public void DefaultValues()
        {
            var options = new CsvOptions<Person>();

            Assert.Equal("\r\n", CsvOptions.DefaultNewLine);
            Assert.Equal(";", CsvOptions.DefaultDelimiter);
            Assert.Equal("\r\n", options.NewLine);
            Assert.Equal(";", options.Delimiter);
            Assert.False(options.DetectDelimiter);
            Assert.Same(Encoding.UTF8, options.TextEncoding);
        }

        [Fact]
        public void NewLineAttribute_Default_IsCrLf()
        {
            Assert.Equal("\r\n", new NewLineAttribute().NewLine);
        }

        [Fact]
        public void Save_WritesTheSameBytesOnEveryOperatingSystem()
        {
            // the bytes do not depend on Environment.NewLine
            var stream = new MemoryStream();
            Csv().Save(stream, People);

            Assert.Equal(Encoding.UTF8.GetBytes(Expected), stream.ToArray());
        }

        [Fact]
        public async Task SaveAsync_WritesTheSameBytesOnEveryOperatingSystem()
        {
            var stream = new MemoryStream();
            await Csv().SaveAsync(stream, People);

            Assert.Equal(Encoding.UTF8.GetBytes(Expected), stream.ToArray());
        }

        [Fact]
        public void Writer_AndGetAllText_UseTheSameNewLine()
        {
            var stream = new MemoryStream();
            using (var writer = Csv().CreateWriter(stream))
            {
                writer.WriteRecords(People);
            }

            Assert.Equal(Expected, Encoding.UTF8.GetString(stream.ToArray()));
            Assert.Equal(Expected.TrimEnd('\r', '\n'), Csv().GetAllText(People));
        }

        [Fact]
        public void Save_UsesTheNewLineOption()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.NewLine = "\n";
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Name);
            });
            var stream = new MemoryStream();
            csv.Save(stream, People);

            Assert.Equal("1;Mario\n2;Luigi\n", Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public void Save_Utf8_WithoutBom_ByDefault()
        {
            var stream = new MemoryStream();
            Csv().Save(stream, People);

            Assert.NotEqual(0xEF, stream.ToArray()[0]);
        }

        [Fact]
        public void Save_Utf8_WithBom_WhenRequested()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.TextEncoding = new UTF8Encoding(true);
                o.Columns.AddColumn(m => m.Id);
            });
            var stream = new MemoryStream();
            csv.Save(stream, People);

            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, stream.ToArray().Take(3));
        }

        [Theory]
        [InlineData("1;Mario;Roma\r\n2;Luigi;Milano\r\n")]
        [InlineData("1;Mario;Roma\n2;Luigi;Milano\n")]
        public void Read_AcceptsCrLfAndLf_WhateverTheNewLineOption(string text)
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.NewLine = "\r\n";
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Name);
                o.Columns.AddColumn(m => m.City);
            });

            var result = csv.LoadFromText(text).ToList();

            Assert.Equal(new[] { "Roma", "Milano" }, result.Select(p => p.City));
        }
    }
}
