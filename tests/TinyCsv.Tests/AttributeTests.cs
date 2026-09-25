namespace TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using TinyCsv.Attributes;
    using TinyCsv.Exceptions;
    using Xunit;

    public class HashSkipRow : ISkipRow
    {
        public Func<string, int, bool> SkipRow { get; } = (row, index) => row.StartsWith("SKIP");
    }

    public class NotASkipRow
    {
    }

    [Delimiter("|")]
    [AllowBackSlashToEscapeQuote(true)]
    [AllowComment(false)]
    [AllowRowEnclosedInDoubleQuotesValues(false)]
    [Comment('!')]
    [DoubleQuotes('\'')]
    [EndOfLineDelimiterChar(false)]
    [HasHeaderRecord(true)]
    [NewLine("\n")]
    [RowsToSkip(2)]
    [SkipRow(typeof(HashSkipRow))]
    [TextEncoding("utf-16")]
    [TrimData(true)]
    [ValidateColumnCount(true)]
    public class AllOptionsModel
    {
        [Column]
        public int Id { get; set; }
    }

    public class ColumnAttributeModel
    {
        [Column(index: 2)]
        public int Id { get; set; }

        [Column(index: 0)]
        public string Name { get; set; }

        [Column(index: 1, format: "dd.MM.yyyy")]
        public DateTime Date { get; set; }

        [Column(index: 3, converter: typeof(Base64Converter))]
        public string Secret { get; set; }

        [Column(index: 4, formatProvider: typeof(ItalianFormat))]
        public decimal Amount { get; set; }

        public string NotAColumn { get; set; }
    }

    public class ItalianFormat : IFormatProvider
    {
        private static readonly CultureInfo Italian = new CultureInfo("it-IT");
        public object GetFormat(Type formatType) => Italian.GetFormat(formatType);
    }

    [HasHeaderRecord(true)]
    public class NamedColumnModel
    {
        [Column(name: "Nome")]
        public string Name { get; set; }
    }

    [Delimiter(";")]
    public class DefaultAttributeModel
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }
    }

    [SkipRow(typeof(NotASkipRow))]
    public class InvalidSkipRowModel
    {
        [Column]
        public int Id { get; set; }
    }

    public class AttributeTests
    {
        [Fact]
        public void ClassAttributes_SetOptions()
        {
            var options = new CsvOptions<AllOptionsModel>(typeof(AllOptionsModel));

            Assert.Equal("|", options.Delimiter);
            Assert.True(options.AllowBackSlashToEscapeQuote);
            Assert.False(options.AllowComment);
            Assert.False(options.AllowRowEnclosedInDoubleQuotesValues);
            Assert.Equal('!', options.Comment);
            Assert.Equal('\'', options.DoubleQuotes);
            Assert.False(options.EndOfLineDelimiterChar);
            Assert.True(options.HasHeaderRecord);
            Assert.Equal("\n", options.NewLine);
            Assert.Equal(2u, options.RowsToSkip);
            Assert.True(options.SkipRow("SKIP me", 0));
            Assert.False(options.SkipRow("keep me", 0));
            Assert.Equal(Encoding.Unicode, options.TextEncoding);
            Assert.True(options.TrimData);
            Assert.True(options.ValidateColumnCount);
        }

        [Fact]
        public void WithoutAttributes_DefaultOptions()
        {
            var options = new CsvOptions<DefaultAttributeModel>(typeof(DefaultAttributeModel));

            Assert.Equal(";", options.Delimiter);
            Assert.False(options.AllowBackSlashToEscapeQuote);
            Assert.True(options.AllowComment);
            Assert.True(options.AllowRowEnclosedInDoubleQuotesValues);
            Assert.Equal('#', options.Comment);
            Assert.Equal('"', options.DoubleQuotes);
            Assert.True(options.EndOfLineDelimiterChar);
            Assert.False(options.HasHeaderRecord);
            Assert.Equal(Environment.NewLine, options.NewLine);
            Assert.Equal(0u, options.RowsToSkip);
            Assert.False(options.SkipRow("anything", 0));
            Assert.False(options.TrimData);
            Assert.False(options.ValidateColumnCount);
            Assert.False(options.EnableHandlers);
        }

        [Fact]
        public void AttributeValues()
        {
            Assert.Equal(Encoding.ASCII, new TextEncodingAttribute(Encoding.ASCII).TextEncoding);
            Assert.Equal(Encoding.UTF8, new TextEncodingAttribute().TextEncoding);
            Assert.Equal(Encoding.Unicode, new TextEncodingAttribute("utf-16").TextEncoding);
            Assert.Equal(Encoding.UTF8, new TextEncodingAttribute(string.Empty).TextEncoding);
            Assert.Equal(Encoding.Unicode, new TextEncodingAttribute(1200).TextEncoding);
            Assert.Equal(Environment.NewLine, new NewLineAttribute().NewLine);
            Assert.Equal(typeof(HashSkipRow), new SkipRowAttribute(typeof(HashSkipRow)).SkipRowType);
        }

        [Fact]
        public void SkipRowAttribute_InvalidType_Throws()
        {
            Assert.Throws<UnsupportedTypeException>(() => new SkipRowAttribute(typeof(NotASkipRow)));
        }

        [Fact]
        public void AllOptionsModel_LoadFromText()
        {
            var csv = new TinyCsv<AllOptionsModel>();

            var result = csv.LoadFromText("skip 1\nskip 2\nId\n 1 \nSKIP 99\n2").ToList();

            Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id));
        }

        [Fact]
        public void ColumnAttribute_IndexFormatConverterAndProvider()
        {
            var csv = new TinyCsv<ColumnAttributeModel>();

            var result = csv.LoadFromText("Mario;31.01.2024;7;TWFyaW8=;1234,5").Single();

            Assert.Equal("Mario", result.Name);
            Assert.Equal(new DateTime(2024, 1, 31), result.Date);
            Assert.Equal(7, result.Id);
            Assert.Equal("Mario", result.Secret);
            Assert.Equal(1234.5m, result.Amount);
            Assert.Null(result.NotAColumn);
        }

        [Fact]
        public void ColumnAttribute_Metadata()
        {
            var options = new CsvOptions<ColumnAttributeModel>(typeof(ColumnAttributeModel));
            var columns = options.Columns.ToDictionary(c => c.ColumnIndex);

            Assert.Equal(5, options.Columns.Count);
            Assert.Equal("Id", columns[2].ColumnName);
            Assert.Equal("Name", columns[0].ColumnName);
            Assert.Equal("dd.MM.yyyy", columns[1].ColumnFormat);
            Assert.IsType<Base64Converter>(columns[3].Converter);
            Assert.IsType<ItalianFormat>(columns[4].ColumnFormatProvider);
        }

        [Fact]
        public void ColumnAttribute_WithName_WritesHeaderAndReads()
        {
            var csv = new TinyCsv<NamedColumnModel>();

            var lines = csv.GetAllLines(new[] { new NamedColumnModel { Name = "Mario" } });
            var result = csv.LoadFromText("Nome\nLuigi").Single();

            Assert.Equal(new[] { "Nome", "Mario" }, lines);
            Assert.Equal("Luigi", result.Name);
        }

        [Fact]
        public void ColumnAttribute_WithoutIndex_UsesPropertyOrder()
        {
            var csv = new TinyCsv<DefaultAttributeModel>();

            var result = csv.LoadFromText("1;Mario").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void InvalidSkipRowAttribute_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new TinyCsv<InvalidSkipRowModel>());
        }
    }
}
