namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// RFC 4180 conformance: every case is read as models and as fields, from a string and from a stream.
    /// </summary>
    public class Rfc4180Tests
    {
        private static TinyCsv<Person> Create(Action<CsvOptions<Person>> configure = null)
        {
            return Csv.Person(o =>
            {
                o.Delimiter = ",";
                configure?.Invoke(o);
            });
        }

        /// <summary>
        /// Reads the text with LoadFromText, LoadFromStream, LoadFromTextAsync and GetAllLinesAndFieldsFromText,
        /// checks that they return the same values and returns them as (Id, Name, City)
        /// </summary>
        private static async Task<(int Id, string Name, string City)[]> ReadAsync(string text, TinyCsv<Person> csv = null)
        {
            csv ??= Create();

            var fromText = csv.LoadFromText(text).Select(Values).ToArray();
            var fromStream = csv.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(text))).Select(Values).ToArray();
            var fromTextAsync = new List<(int, string, string)>();
            await foreach (var person in csv.LoadFromTextAsync(text))
            {
                fromTextAsync.Add(Values(person));
            }
            var fields = csv.GetAllLinesAndFieldsFromText(text)
                .Skip(csv.Options.HasHeaderRecord ? 1 : 0)
                // GetAllLinesAndFields does not return an empty field after the last delimiter (EndOfLineDelimiterChar):
                // a missing field is an empty field, as for the models
                .Select(f => (int.Parse(f[0]), f.Length > 1 ? f[1] : string.Empty, f.Length > 2 ? f[2] : string.Empty))
                .ToArray();

            Assert.Equal(fromText, fromStream);
            Assert.Equal(fromText, fromTextAsync);
            Assert.Equal(fromText, fields);
            return fromText;
        }

        private static (int Id, string Name, string City) Values(Person person) => (person.Id, person.Name, person.City);

        [Fact]
        public async Task QuotedFieldWithTheDelimiter()
        {
            var rows = await ReadAsync("1,\"Rossi, Mario\",Roma\r\n2,\",\",\",,\"");

            Assert.Equal(new[] { (1, "Rossi, Mario", "Roma"), (2, ",", ",,") }, rows);
        }

        [Fact]
        public async Task DoubledQuotes_AreOneQuote()
        {
            var rows = await ReadAsync("1,\"Mario \"\"Super\"\" Rossi\",\"\"\"Roma\"\"\"\r\n2,\"\"\"\"\"\",\"\"");

            Assert.Equal(new[] { (1, "Mario \"Super\" Rossi", "\"Roma\""), (2, "\"\"", "") }, rows);
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("\r")]
        public async Task NewLinesInsideQuotes_BelongToTheField(string newLine)
        {
            var text = $"1,\"line 1{newLine}line 2\",\"{newLine}\"{newLine}2,b,\"{newLine}{newLine}c{newLine}\"";

            var rows = await ReadAsync(text);

            Assert.Equal(new[] { (1, $"line 1{newLine}line 2", newLine), (2, "b", $"{newLine}{newLine}c{newLine}") }, rows);
        }

        [Theory]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("\r")]
        public async Task RecordSeparators(string newLine)
        {
            var rows = await ReadAsync($"1,a,b{newLine}2,c,d{newLine}3,e,f");

            Assert.Equal(new[] { (1, "a", "b"), (2, "c", "d"), (3, "e", "f") }, rows);
        }

        [Fact]
        public async Task MixedRecordSeparators()
        {
            var rows = await ReadAsync("1,a,b\r\n2,c,d\n3,e,f\r4,g,h");

            Assert.Equal(new[] { 1, 2, 3, 4 }, rows.Select(r => r.Id));
        }

        [Theory]
        [InlineData("1,a,\r\n2,b,")]
        [InlineData("1,a,\"\"\r\n2,b,\"\"")]
        public async Task EmptyLastField(string text)
        {
            var rows = await ReadAsync(text);

            Assert.Equal(new[] { (1, "a", ""), (2, "b", "") }, rows);
        }

        [Fact]
        public async Task EmptyFields()
        {
            var rows = await ReadAsync("1,,\"\"\r\n2,\"\",\"\"");

            Assert.Equal(new[] { (1, "", ""), (2, "", "") }, rows);
        }

        [Theory]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("\r")]
        public async Task FinalNewLine_IsOptional(string end)
        {
            var rows = await ReadAsync("1,a,b\r\n2,\"c\r\nd\",e" + end);

            Assert.Equal(new[] { (1, "a", "b"), (2, "c\r\nd", "e") }, rows);
        }

        [Fact]
        public async Task QuotedFieldAtTheEnd_WithoutFinalNewLine()
        {
            var rows = await ReadAsync("1,a,\"b,\"\"c\"\"\"");

            Assert.Equal(new[] { (1, "a", "b,\"c\"") }, rows);
        }

        [Fact]
        public async Task SpacesAreKept_WithoutTrimData()
        {
            var rows = await ReadAsync("1, a , \"b\" ");

            // RFC 4180: the spaces are part of a field; a quote that is not at the start of a field is a literal char
            Assert.Equal(new[] { (1, " a ", " \"b\" ") }, rows);
        }

        [Fact]
        public async Task SpacesAroundQuotes_WithTrimData()
        {
            var rows = await ReadAsync("1,  \"a, b\"  ,\t\" c \"\t", Create(o => o.TrimData = true));

            // the spaces around a quoted field are removed, the spaces inside the quotes are kept
            Assert.Equal(new[] { (1, "a, b", " c ") }, rows);
        }

        [Fact]
        public async Task QuoteInsideAnUnquotedField_IsALiteralChar()
        {
            var rows = await ReadAsync("1,Mario \"Super\",5\" disk");

            Assert.Equal(new[] { (1, "Mario \"Super\"", "5\" disk") }, rows);
        }

        [Theory]
        [InlineData("utf-8")]
        [InlineData("utf-16")]
        [InlineData("utf-32")]
        public void ByteOrderMark_IsNotPartOfTheFirstField(string encoding)
        {
            var csv = Create(o => o.HasHeaderRecord = true);
            var text = "Id,Name,City\r\n1,\"a\",b";
            var e = Encoding.GetEncoding(encoding);
            var bytes = e.GetPreamble().Concat(e.GetBytes(text)).ToArray();

            var models = csv.LoadFromStream(new MemoryStream(bytes)).ToArray();
            var fields = csv.GetAllLinesAndFieldsFromStream(new MemoryStream(bytes)).ToArray();

            Assert.Equal(new[] { (1, "a", "b") }, models.Select(Values));
            Assert.Equal(new[] { "Id", "Name", "City" }, fields[0]);
        }

        [Fact]
        public async Task Header_WithQuotedNames()
        {
            var csv = Create(o => o.HasHeaderRecord = true);

            var rows = await ReadAsync("\"Id\",\"Name, full\",\"City\"\r\n1,a,b", csv);

            Assert.Equal(new[] { (1, "a", "b") }, rows);
        }

        [Fact]
        public void Write_QuotesOnlyTheFieldsThatNeedIt()
        {
            var csv = Create(o => o.NewLine = "\r\n");
            var people = new[]
            {
                new Person { Id = 1, Name = "a,b", City = "plain" },
                new Person { Id = 2, Name = "say \"hi\"", City = "x\r\ny" },
                new Person { Id = 3, Name = "", City = null },
                new Person { Id = 4, Name = " spaces ", City = "\n" },
            };

            var text = csv.GetAllText(people);

            Assert.Equal("1,\"a,b\",plain\r\n2,\"say \"\"hi\"\"\",\"x\r\ny\"\r\n3,,\r\n4, spaces ,\"\n\"", text);
        }
    }
}
