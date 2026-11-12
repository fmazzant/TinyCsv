namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Mafesoft.TinyCsv.Data;
    using Xunit;

    /// <summary>
    /// The delimiters found while reading the records (see CsvRecordReader.HasDelimiters) give the same records
    /// and the same fields of the scan of each field
    /// </summary>
    public class DelimiterScanTests
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

        /// <summary>
        /// The records and their fields (or the exception), with or without the delimiters found by the record reader
        /// </summary>
        private static List<string> Read(string text, int chunkSize, CsvOptions<Person> options, int columnsCount, bool findDelimiters)
        {
            var records = new CsvRecordReader(new ChunkedReader(text, chunkSize), options, findDelimiters);
            var fieldsReader = new TinyCsvDataReader<Person>(options, new StringReader(string.Empty));
            var result = new List<string>();
            CsvField[] fields = null;
            while (records.Read())
            {
                var record = records.CurrentToString();
                try
                {
                    var count = findDelimiters && records.HasDelimiters
                        ? fieldsReader.ReadFields(records, columnsCount, ref fields)
                        : fieldsReader.ReadFields(records.Buffer, records.CurrentStart, records.CurrentLength, columnsCount, ref fields);
                    result.Add(record + " => [" + string.Join("|", (fields ?? new CsvField[0]).Take(count).Select(f => f.ToString())) + "]");
                }
                catch (Exception exception)
                {
                    result.Add(record + " => " + exception.GetType().Name);
                }
            }
            return result;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(33)]
        [InlineData(4096)]
        public void Fields_AreTheSameOfTheFieldsScan(int chunkSize)
        {
            // mostly plain chars, so many records have no quotes and long runs fill the blocks of 32 chars
            var chars = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;;;;;;\"  \t\n\r#".ToCharArray();
            var random = new Random(7);
            for (int n = 0; n < 3000; n++)
            {
                var options = new CsvOptions<Person>
                {
                    Delimiter = ";",
                    TrimData = random.Next(2) == 0,
                    AllowRowEnclosedInDoubleQuotesValues = random.Next(4) != 0,
                    EndOfLineDelimiterChar = random.Next(2) == 0,
                    AllowComment = random.Next(2) == 0,
                    Comment = '#',
                };
                var columnsCount = random.Next(1, 6);
                var text = new string(Enumerable.Range(0, random.Next(1, 300)).Select(_ => chars[random.Next(chars.Length)]).ToArray());

                Assert.Equal(Read(text, chunkSize, options, columnsCount, false), Read(text, chunkSize, options, columnsCount, true));
            }
        }

        [Fact]
        public void QuotedFields_AreReadFromTheRecord()
        {
            var csv = new TinyCsv<Person>(options =>
            {
                options.HasHeaderRecord = false;
                options.Delimiter = ";";
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.City);
            });
            var padding = new string('x', 40);
            var text = "1;\"a \"\"quoted\"\" name\";\"multi\r\nline\"\r\n"
                + "2;\"\";\"" + padding + "\"\n"
                + "3;\"\"\"\";  \"spaced\"\n"
                + "4;\"after\"x;\"in\"side\"";

            var people = csv.LoadFromText(text).ToList();

            Assert.Equal(new[] { 1, 2, 3, 4 }, people.Select(p => p.Id));
            Assert.Equal(new[] { "a \"quoted\" name", "", "\"", "afterx" }, people.Select(p => p.Name));
            Assert.Equal(new[] { "multi\r\nline", padding, "  \"spaced\"", "inside\"" }, people.Select(p => p.City));
        }

        [Fact]
        public void LongRecords_AreReadWithTheirFields()
        {
            var csv = new TinyCsv<Person>(options =>
            {
                options.HasHeaderRecord = false;
                options.Delimiter = ",";
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.City);
            });
            var name = new string('n', 100);
            var surname = new string('s', 70);
            var text = string.Concat(Enumerable.Repeat($"{name},{surname}\r\n\"{name}\",\"{surname}\"\n", 100));

            var people = csv.LoadFromText(text).ToList();

            Assert.Equal(200, people.Count);
            Assert.All(people, p => Assert.Equal(name, p.Name));
            Assert.All(people, p => Assert.Equal(surname, p.City));
        }
    }
}
