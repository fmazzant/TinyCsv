namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Mafesoft.TinyCsv.Conversions;
    using Xunit;

    /// <summary>
    /// The fast parsers of the values return the same result of the converters, or leave the value to the converter
    /// </summary>
    public class ValueParserTests
    {
        private static readonly string[] Cultures = { "", "it-IT", "de-DE", "fr-FR", "en-US", "ar-SA" };

        private static readonly string[] Values =
        {
            "0", "7", "-7", "007", "-0", "+5", " 5", "5 ", "", " ", "-", "--1", "1-",
            "123456789", "-123456789", "999999999", "1000000000", "2147483647", "-2147483648", "2147483648",
            "123456789012345678", "-123456789012345678", "1234567890123456789", "9223372036854775807",
            "1.5", "1,5", "-1.5", "-1,5", "1.10", "1,10", "0.000", "0,000", "-0.0", "-0,0", ".5", ",5", "5.", "5,",
            "1..2", "1,,2", "1.2.3", "1,234.5", "1.234,5", "1e3", "0x10", "٣", "1 234,5",
            "12345678901234567.8", "1234567890123456.78", "0.123456789012345678", "0.1234567890123456789",
            "99999999999999999.9", "-99999999999999999,9",
        };

        private static CultureInfo GetCulture(string name) => CultureInfo.GetCultureInfo(name);

        [Fact]
        public void Int32ValueParser_IsTheConverter()
        {
            var converter = new Int32Converter();
            foreach (var culture in Cultures.Select(GetCulture))
            {
                var parser = converter.GetParser(culture);
                Assert.NotNull(parser);
                foreach (var value in Values)
                {
                    if (parser.TryParse(value.AsSpan(), out var fast))
                    {
                        Assert.True(converter.TryParseValue(value, culture, out var expected), $"{culture.Name} '{value}'");
                        Assert.Equal(expected, fast);
                    }
                }
            }
        }

        [Fact]
        public void Int64ValueParser_IsTheConverter()
        {
            var converter = new Int64Converter();
            foreach (var culture in Cultures.Select(GetCulture))
            {
                var parser = converter.GetParser(culture);
                Assert.NotNull(parser);
                foreach (var value in Values)
                {
                    if (parser.TryParse(value.AsSpan(), out var fast))
                    {
                        Assert.True(converter.TryParseValue(value, culture, out var expected), $"{culture.Name} '{value}'");
                        Assert.Equal(expected, fast);
                    }
                }
            }
        }

        [Fact]
        public void DecimalValueParser_IsTheConverter_WithTheScale()
        {
            var converter = new DecimalConverter();
            foreach (var culture in Cultures.Select(GetCulture))
            {
                var parser = converter.GetParser(culture);
                if (parser == null)
                {
                    continue;
                }
                foreach (var value in Values)
                {
                    if (parser.TryParse(value.AsSpan(), out var fast))
                    {
                        Assert.True(converter.TryParseValue(value, culture, out var expected), $"{culture.Name} '{value}'");
                        Assert.Equal(decimal.GetBits(expected), decimal.GetBits(fast));
                    }
                }
            }
        }

        [Fact]
        public void DecimalValueParser_ParsesTheCommonValues()
        {
            var parser = new DecimalConverter().GetParser(CultureInfo.InvariantCulture);

            Assert.True(parser.TryParse("1.10".AsSpan(), out var value));
            Assert.Equal(new[] { 110, 0, 0, 2 << 16 }, decimal.GetBits(value));
            Assert.True(parser.TryParse("-12345678901234567.8".AsSpan(), out value));
            Assert.Equal(-12345678901234567.8m, value);
            Assert.False(parser.TryParse("1234567890123456789".AsSpan(), out _));
        }

        [Fact]
        public void Parsers_AreNotUsed_WhenTheCultureMayChange()
        {
            var culture = new CultureInfo("it-IT");

            Assert.Null(new Int32Converter().GetParser(culture));
            Assert.Null(new DecimalConverter().GetParser(culture));
            Assert.Null(new DateTimeConverter().GetParser(new CsvColumn.DefaultFormatProvider("dd/MM/yyyy", culture)));
        }

        [Fact]
        public void DateTimeValueParser_IsCreated_WithASupportedFormat()
        {
            var culture = CultureInfo.InvariantCulture;
            var converter = new DateTimeConverter();

            Assert.Null(converter.GetParser(culture));
            Assert.Null(converter.GetParser(new CsvColumn.DefaultFormatProvider("ddd dd MMM", culture)));

            var parser = converter.GetParser(new CsvColumn.DefaultFormatProvider("yyyy-MM-dd", culture));
            Assert.True(parser.TryParse("2022-04-02".AsSpan(), out var date));
            Assert.Equal(new DateTime(2022, 4, 2), date);
        }

        [Fact]
        public void Load_WithAChangedCulture_UsesTheConverter()
        {
            var culture = new CultureInfo("it-IT");
            culture.NumberFormat.NumberDecimalSeparator = "#";
            culture.NumberFormat.CurrencyDecimalSeparator = "#";

            var csv = new TinyCsv<FastPathModel>(options =>
            {
                options.HasHeaderRecord = false;
                options.Delimiter = ";";
                options.Culture = culture;
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Price);
            });

            var model = csv.LoadFromText("-12;3#25\r\n").Single();
            Assert.Equal(-12, model.Id);
            Assert.Equal(3.25m, model.Price);
        }
    }
}
