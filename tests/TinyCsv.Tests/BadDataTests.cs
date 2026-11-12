namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Attributes;
    using Mafesoft.TinyCsv.Conversions;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public enum Status
    {
        Active,
        Disabled
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal? Amount { get; set; }
        public DateTime Date { get; set; }
        public bool Paid { get; set; }
        public Status Status { get; set; }
        public byte[] Data { get; set; }
    }

    [ValidateValues]
    public class ValidatedModel
    {
        [Column]
        public int Id { get; set; }
    }

    /// <summary>
    /// A custom converter that throws on invalid values
    /// </summary>
    public class StrictInt32Converter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider) => value?.ToString();

        public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => int.Parse(value);
    }

    public class BadDataTests
    {
        private static TinyCsv<Order> Orders(Action<CsvOptions<Order>> configure = null)
        {
            return new TinyCsv<Order>(o =>
            {
                o.HasHeaderRecord = true;
                o.ValidateValues = true;
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Amount);
                o.Columns.AddColumn(m => m.Date, "yyyy-MM-dd");
                o.Columns.AddColumn(m => m.Paid);
                o.Columns.AddColumn(m => m.Status);
                o.Columns.AddColumn(m => m.Data);
                configure?.Invoke(o);
            });
        }

        private const string Header = "Id;Amount;Date;Paid;Status;Data";

        // ---------- ValidateValues ----------

        [Fact]
        public void WithoutValidateValues_InvalidValuesAreDefault()
        {
            var csv = Orders(o => o.ValidateValues = false);

            var result = csv.LoadFromText($"{Header}\nabc;xyz;02/11/2026;yes;Unknown;").Single();

            Assert.Equal((0, (decimal?)null, default(DateTime), false, Status.Active), (result.Id, result.Amount, result.Date, result.Paid, result.Status));
        }

        [Fact]
        public void ValidateValues_ValidRow()
        {
            var result = Orders().LoadFromText($"{Header}\n1;10.5;2026-11-02;true;Disabled;AQI=").Single();

            Assert.Equal((1, (decimal?)10.5m, new DateTime(2026, 11, 2), true, Status.Disabled), (result.Id, result.Amount, result.Date, result.Paid, result.Status));
            Assert.Equal(new byte[] { 1, 2 }, result.Data);
        }

        [Fact]
        public void ValidateValues_EmptyValuesAreValid()
        {
            var result = Orders().LoadFromText($"{Header}\n1; ;;;;").Single();

            Assert.Equal((1, (decimal?)null, default(DateTime), false, Status.Active), (result.Id, result.Amount, result.Date, result.Paid, result.Status));
            Assert.Null(result.Data);
        }

        [Theory]
        [InlineData("abc;10;2026-11-02;true;Active;", "Id", "abc")]
        [InlineData("1;ten;2026-11-02;true;Active;", "Amount", "ten")]
        [InlineData("1;10;02/11/2026;true;Active;", "Date", "02/11/2026")]
        [InlineData("1;10;2026-11-02;yes;Active;", "Paid", "yes")]
        [InlineData("1;10;2026-11-02;true;Unknown;", "Status", "Unknown")]
        [InlineData("1;10;2026-11-02;true;Active;not base64!", "Data", "not base64!")]
        public void ValidateValues_InvalidValue_Throws(string row, string column, string value)
        {
            var exception = Assert.Throws<InvalidColumnValueException>(() => Orders().LoadFromText($"{Header}\n{row}").ToList());

            Assert.Equal(1, exception.RowIndex);
            Assert.Equal(row, exception.Row);
            Assert.Equal(column, exception.ColumnName);
            Assert.Equal(value, exception.Value);
            Assert.Contains($"(row 1, column {column})", exception.Message);
        }

        [Fact]
        public void ValidateValues_Attribute()
        {
            var csv = new TinyCsv<ValidatedModel>();

            Assert.True(csv.Options.ValidateValues);
            Assert.Throws<InvalidColumnValueException>(() => csv.LoadFromText("abc").ToList());
        }

        // ---------- BadDataFound ----------

        [Fact]
        public void BadDataFound_SkipsTheRowsAndGoesOn()
        {
            var errors = new List<CsvException>();
            var csv = Orders(o => o.BadDataFound = errors.Add);

            var result = csv.LoadFromText($"{Header}\n1;10;2026-11-02;true;Active;\nabc;10;2026-11-02;true;Active;\n3;30;2026-11-03;false;Disabled;\n4;40;2026-11-04;maybe;Active;").ToList();

            Assert.Equal(new[] { 1, 3 }, result.Select(r => r.Id));
            Assert.Equal(new int?[] { 2, 4 }, errors.Select(e => e.RowIndex));
            Assert.Equal(new[] { "Id", "Paid" }, errors.Select(e => e.ColumnName));
            Assert.Equal(new[] { "abc", "maybe" }, errors.Select(e => e.Value));
            Assert.Equal("abc;10;2026-11-02;true;Active;", errors[0].Row);
        }

        [Fact]
        public void BadDataFound_InvalidColumnCount()
        {
            var errors = new List<CsvException>();
            var csv = Csv.Person(o =>
            {
                o.ValidateColumnCount = true;
                o.BadDataFound = errors.Add;
            });

            var result = csv.LoadFromText("1;Mario;Roma\n2;Luigi\n3;Anna;Milano").ToList();

            Assert.Equal(new[] { 1, 3 }, result.Select(r => r.Id));
            var error = Assert.IsType<InvalidColumnCountException>(Assert.Single(errors));
            Assert.Equal(1, error.RowIndex);
            Assert.Equal("2;Luigi", error.Row);
            Assert.Null(error.ColumnName);
        }

        [Fact]
        public void BadDataFound_CommentNotAllowed()
        {
            var errors = new List<CsvException>();
            var csv = Csv.Person(o =>
            {
                o.AllowComment = false;
                o.BadDataFound = errors.Add;
            });

            var result = csv.LoadFromText("1;Mario;Roma\n# comment\n2;Luigi;Milano").ToList();

            Assert.Equal(2, result.Count);
            Assert.IsType<NotAllowCommentException>(Assert.Single(errors));
        }

        [Fact]
        public void BadDataFound_NotCalledForAMissingColumnInTheHeader()
        {
            var errors = new List<CsvException>();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.MapColumnsByHeader = true;
                o.BadDataFound = errors.Add;
            });

            Assert.Throws<MissingColumnException>(() => csv.LoadFromText("Id;Name\n1;Mario").ToList());
            Assert.Empty(errors);
        }

        [Fact]
        public void BadDataFound_RowReadEventIsNotRaisedForTheSkippedRows()
        {
            var read = new List<int>();
            var csv = Orders(o =>
            {
                o.EnableHandlers = true;
                o.Handlers.Read.RowRead += (s, e) => read.Add(e.Index);
                o.BadDataFound = e => { };
            });

            csv.LoadFromText($"{Header}\n1;10;2026-11-02;true;Active;\nabc;;;;;\n3;;;;;").ToList();

            Assert.Equal(new[] { 1, 3 }, read);
        }

        // ---------- converter exceptions ----------

        [Fact]
        public void ConverterException_IsWrappedWithThePosition()
        {
            var csv = new TinyCsv<Person>(o => o.Columns.AddColumn(m => m.Id, new StrictInt32Converter()));

            var exception = Assert.Throws<InvalidColumnValueException>(() => csv.LoadFromText("1\nx").ToList());

            Assert.Equal((1, "Id", "x"), (exception.RowIndex, exception.ColumnName, exception.Value));
            Assert.IsType<FormatException>(exception.InnerException);
        }

        [Fact]
        public void ConverterException_WithBadDataFound()
        {
            var errors = new List<CsvException>();
            var csv = new TinyCsv<Person>(o =>
            {
                o.Columns.AddColumn(m => m.Id, new StrictInt32Converter());
                o.BadDataFound = errors.Add;
            });

            var result = csv.LoadFromText("1\nx\n3").ToList();

            Assert.Equal(new[] { 1, 3 }, result.Select(r => r.Id));
            Assert.Equal("x", Assert.Single(errors).Value);
        }

        [Fact]
        public void Base64_InvalidValue_IsWrappedWithoutValidateValues()
        {
            var csv = Orders(o => o.ValidateValues = false);

            var exception = Assert.Throws<InvalidColumnValueException>(() => csv.LoadFromText($"{Header}\n1;;;;;not base64!").ToList());

            Assert.Equal("Data", exception.ColumnName);
            Assert.IsType<FormatException>(exception.InnerException);
        }

        [Fact]
        public void Exceptions_DeriveFromCsvException()
        {
            Assert.True(typeof(CsvException).IsAssignableFrom(typeof(InvalidColumnCountException)));
            Assert.True(typeof(CsvException).IsAssignableFrom(typeof(InvalidColumnValueException)));
            Assert.True(typeof(CsvException).IsAssignableFrom(typeof(NotAllowCommentException)));
            Assert.True(typeof(CsvException).IsAssignableFrom(typeof(EndOfLineDelimiterCharException)));
            Assert.True(typeof(CsvException).IsAssignableFrom(typeof(MissingColumnException)));
        }

        [Fact]
        public void TryConvertBack_BuiltInConverters()
        {
            Assert.False(new Int32Converter().TryConvertBack("abc", typeof(int), null, null, out var invalid));
            Assert.Equal(0, invalid);
            Assert.True(new Int32Converter().TryConvertBack("", typeof(int?), null, null, out var empty));
            Assert.Null(empty);
            Assert.True(new Int32Converter().TryConvertBack("12", typeof(int), null, null, out var valid));
            Assert.Equal(12, valid);
            Assert.True(new StringConverter().TryConvertBack("anything", typeof(string), null, null, out var text));
            Assert.Equal("anything", text);
        }

        // ---------- async ----------

        [Fact]
        public async Task BadDataFound_Async()
        {
            var errors = new List<CsvException>();
            var result = new List<Order>();
            await foreach (var order in Orders(o => o.BadDataFound = errors.Add).LoadFromTextAsync($"{Header}\n1;;;;;\nabc;;;;;\n3;;;;;"))
            {
                result.Add(order);
            }

            Assert.Equal(new[] { 1, 3 }, result.Select(r => r.Id));
            Assert.Equal(2, Assert.Single(errors).RowIndex);
        }
    }
}
