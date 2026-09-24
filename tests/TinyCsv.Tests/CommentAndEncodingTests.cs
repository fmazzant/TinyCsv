namespace TinyCsv.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using TinyCsv.Exceptions;
    using Xunit;

    public class CommentAndEncodingTests : IDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), $"tinycsv-{Guid.NewGuid():N}.csv");

        public void Dispose()
        {
            File.Delete(path);
        }

        // ---------- comments ----------

        [Fact]
        public void Comments_AreSkipped()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("# first comment\n1;Mario;Roma\n#second\n2;Luigi;Milano\n#").ToList();

            Assert.Equal(new[] { "Mario", "Luigi" }, result.Select(x => x.Name));
        }

        [Fact]
        public void Comments_BeforeHeader_AreSkipped()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var result = csv.LoadFromText("# exported by TinyCsv\nId;Name;City\n1;Mario;Roma").ToList();

            Assert.Single(result);
            Assert.Equal("Mario", result[0].Name);
        }

        [Fact]
        public void Comments_CustomCommentChar()
        {
            var csv = Csv.Person(o => o.Comment = '!');

            var result = csv.LoadFromText("! comment\n#1;Mario;Roma").Single();

            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void Comments_NotSkipped_WhenCommentCharIsInsideARecord()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"Mario\n# not a comment\";Roma").Single();

            Assert.Equal("Mario\n# not a comment", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Comments_AreSkippedByGetAllLines()
        {
            var csv = Csv.Person();

            var lines = csv.GetAllLinesFromText("# comment\n1;Mario;Roma").ToList();

            Assert.Equal(new[] { "1;Mario;Roma" }, lines);
        }

        [Fact]
        public void Comments_NotAllowed_Throws()
        {
            var csv = Csv.Person(o => o.AllowComment = false);

            Assert.Throws<NotAllowCommentException>(() => csv.LoadFromText("1;Mario;Roma\n# comment").ToList());
        }

        [Fact]
        public void Comments_SkippedRecordsKeepTheirIndex()
        {
            var indexes = new System.Collections.Generic.List<int>();
            var csv = Csv.Person(o => o.SkipRow = (row, idx) => { indexes.Add(idx); return false; });

            csv.LoadFromText("1;a;b\n# comment\n2;c;d").ToList();

            Assert.Equal(new[] { 0, 2 }, indexes);
        }

        // ---------- encoding ----------

        [Fact]
        public void TextEncoding_ReadsFileWithoutByteOrderMark()
        {
            File.WriteAllBytes(path, Encoding.GetEncoding("iso-8859-1").GetBytes("1;Niccolò;Forlì"));
            var csv = Csv.Person(o => o.TextEncoding = Encoding.GetEncoding("iso-8859-1"));

            var result = csv.LoadFromFile(path).Single();

            Assert.Equal("Niccolò", result.Name);
            Assert.Equal("Forlì", result.City);
        }

        [Fact]
        public void TextEncoding_ByteOrderMarkTakesPrecedence()
        {
            File.WriteAllText(path, "1;Niccolò;Forlì", Encoding.Unicode);
            var csv = Csv.Person();

            Assert.Equal("Niccolò", csv.LoadFromFile(path).Single().Name);
        }

        [Fact]
        public void TextEncoding_Stream()
        {
            var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes("1;Niccolò;Forlì");
            var csv = Csv.Person(o => o.TextEncoding = Encoding.GetEncoding("iso-8859-1"));

            Assert.Equal("Forlì", csv.LoadFromStream(new MemoryStream(bytes)).Single().City);
        }

        [Fact]
        public void TextEncoding_Save()
        {
            var csv = Csv.Person(o => o.TextEncoding = Encoding.Unicode);

            csv.Save(path, new[] { new Person { Id = 1, Name = "Niccolò", City = "Forlì" } });

            Assert.Equal("1;Niccolò;Forlì", File.ReadAllText(path, Encoding.Unicode).Trim());
            Assert.Equal("Niccolò", csv.LoadFromFile(path).Single().Name);
        }

        [Fact]
        public void TextEncoding_DefaultUtf8_WrittenWithoutByteOrderMark()
        {
            var csv = Csv.Person();
            var stream = new MemoryStream();

            csv.Save(path, new[] { new Person { Id = 1, Name = "à" } });
            csv.Save(stream, new[] { new Person { Id = 1, Name = "à" } });

            Assert.Equal((byte)'1', File.ReadAllBytes(path)[0]);
            Assert.Equal((byte)'1', stream.ToArray()[0]);
        }
    }
}
