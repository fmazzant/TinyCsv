namespace TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TinyCsv.Data;
    using TinyCsv.Exceptions;
    using Xunit;

    public class RecordReaderTests
    {
        /// <summary>
        /// A TextReader returning at most chunkSize chars for each Read, to exercise the buffer boundaries.
        /// </summary>
        private sealed class ChunkedReader : TextReader
        {
            private readonly string text;
            private readonly int chunkSize;
            private int position;

            public ChunkedReader(string text, int chunkSize)
            {
                this.text = text;
                this.chunkSize = chunkSize;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var n = Math.Min(Math.Min(count, chunkSize), text.Length - position);
                text.CopyTo(position, buffer, index, n);
                position += n;
                return n;
            }
        }

        private static CsvOptions<Person> Options(Action<CsvOptions<Person>> configure = null)
        {
            var options = new CsvOptions<Person> { Delimiter = ";" };
            configure?.Invoke(options);
            return options;
        }

        private static List<string> ReadAll(string text, int chunkSize, CsvOptions<Person> options = null)
        {
            var reader = new CsvRecordReader(new ChunkedReader(text, chunkSize), options ?? Options());
            var records = new List<string>();
            string record;
            while ((record = reader.ReadRecord()) != null)
            {
                records.Add(record);
            }
            return records;
        }

        private static async Task<List<string>> ReadAllAsync(string text, int chunkSize, CsvOptions<Person> options = null)
        {
            var reader = new CsvRecordReader(new ChunkedReader(text, chunkSize), options ?? Options());
            var records = new List<string>();
            string record;
            while ((record = await reader.ReadRecordAsync()) != null)
            {
                records.Add(record);
            }
            return records;
        }

        public static IEnumerable<object[]> ChunkSizes => new[] { 1, 2, 3, 7, 4096 }.Select(x => new object[] { x });

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_AllLineEndings(int chunkSize)
        {
            var records = ReadAll("a;b\nc;d\r\ne;f\rg;h", chunkSize);

            Assert.Equal(new[] { "a;b", "c;d", "e;f", "g;h" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_EmptyLinesAreEmptyRecords(int chunkSize)
        {
            var records = ReadAll("a\n\n\r\nb\n", chunkSize);

            Assert.Equal(new[] { "a", "", "", "b" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_QuotedNewLinesAndDoubledQuotes(int chunkSize)
        {
            var records = ReadAll("1;\"a\r\n\"\"b\"\"\nc\";x\r\n2;\"\";y", chunkSize);

            Assert.Equal(new[] { "1;\"a\r\n\"\"b\"\"\nc\";x", "2;\"\";y" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_QuoteInsideUnquotedFieldIsLiteral(int chunkSize)
        {
            var records = ReadAll("1;5\" pipe;x\n2;b;y", chunkSize);

            Assert.Equal(new[] { "1;5\" pipe;x", "2;b;y" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_BackSlashEscapesNewLine(int chunkSize)
        {
            var records = ReadAll("1;a\\\nb;x\n2;\\\"\nc;y", chunkSize, Options(o => o.AllowBackSlashToEscapeQuote = true));

            Assert.Equal(new[] { "1;a\\\nb;x", "2;\\\"", "c;y" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_MultiCharDelimiter(int chunkSize)
        {
            var records = ReadAll("1||\"a\nb\"||x\n2||c||y", chunkSize, Options(o => o.Delimiter = "||"));

            Assert.Equal(new[] { "1||\"a\nb\"||x", "2||c||y" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_QuotedFieldAfterSpacesWithTrimData(int chunkSize)
        {
            var records = ReadAll("1; \"a\nb\" ;x\n2;c;y", chunkSize, Options(o => o.TrimData = true));

            Assert.Equal(new[] { "1; \"a\nb\" ;x", "2;c;y" }, records);
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public void Records_QuotingDisabled(int chunkSize)
        {
            var records = ReadAll("1;\"a\nb\";x", chunkSize, Options(o => o.AllowRowEnclosedInDoubleQuotesValues = false));

            Assert.Equal(new[] { "1;\"a", "b\";x" }, records);
        }

        [Fact]
        public void Records_UnterminatedQuoteReadsToEnd()
        {
            var records = ReadAll("1;a;x\n2;\"b\nc;y", 3);

            Assert.Equal(new[] { "1;a;x", "2;\"b\nc;y" }, records);
        }

        [Fact]
        public void Records_LargerThanBuffer()
        {
            var big = new string('x', 10000) + "\n" + new string('y', 5000);
            var text = $"1;\"{big}\";z\n2;{new string('w', 9000)};k";

            var records = ReadAll(text, 4096);

            Assert.Equal(2, records.Count);
            Assert.Equal($"1;\"{big}\";z", records[0]);
            Assert.Equal($"2;{new string('w', 9000)};k", records[1]);
        }

        [Fact]
        public void Records_EmptyText()
        {
            Assert.Empty(ReadAll(string.Empty, 10));
        }

        [Theory]
        [MemberData(nameof(ChunkSizes))]
        public async Task Records_Async(int chunkSize)
        {
            var records = await ReadAllAsync("1;\"a\r\nb\";x\r\n2;c;y\r\n", chunkSize);

            Assert.Equal(new[] { "1;\"a\r\nb\";x", "2;c;y" }, records);
        }

        [Fact]
        public void Fields_MultiCharDelimiter()
        {
            var csv = Csv.Person(o => o.Delimiter = "||");

            var result = csv.LoadFromText("1||\"a||b\"||Roma").Single();

            Assert.Equal("a||b", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Fields_QuotedFieldKeepsInnerSpacesWithTrimData()
        {
            var csv = Csv.Person(o => o.TrimData = true);

            var result = csv.LoadFromText("1;  \"  Mario  \"  ;Roma").Single();

            Assert.Equal("  Mario  ", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Fields_EndOfLineDelimiterCharFalse_KeepsLastField()
        {
            var csv = Csv.Person(o => o.EndOfLineDelimiterChar = false);

            var result = csv.LoadFromText("1;Mario;Roma").Single();

            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Fields_EndOfLineDelimiterCharFalse_TrailingDelimiterThrows()
        {
            var csv = Csv.Person(o => o.EndOfLineDelimiterChar = false);

            Assert.Throws<EndOfLineDelimiterCharException>(() => csv.LoadFromText("1;Mario;Roma;").ToList());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RoundTrip_RandomValues(bool backslash)
        {
            var random = new Random(42);
            var alphabet = "ab ;,\"'\\\r\n\tàè";
            string RandomValue() => new string(Enumerable.Range(0, random.Next(0, 12)).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());

            var people = Enumerable.Range(0, 500)
                .Select(i => new Person { Id = i, Name = RandomValue(), City = RandomValue() })
                .ToArray();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.AllowBackSlashToEscapeQuote = backslash;
            });

            var text = csv.GetAllText(people);
            var result = csv.LoadFromStream(new ChunkedStream(text)).ToList();

            Assert.Equal(people.Length, result.Count);
            for (int i = 0; i < people.Length; i++)
            {
                Assert.Equal(people[i].Id, result[i].Id);
                Assert.Equal(people[i].Name, result[i].Name);
                Assert.Equal(people[i].City, result[i].City);
            }
        }

        /// <summary>
        /// A stream returning few bytes for each Read
        /// </summary>
        private sealed class ChunkedStream : MemoryStream
        {
            public ChunkedStream(string text)
                : base(Encoding.UTF8.GetBytes(text))
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, 5));
            }
        }
    }
}
