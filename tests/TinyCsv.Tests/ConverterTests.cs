namespace TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.Numerics;
    using TinyCsv.Conversions;
    using TinyCsv.Factory;
    using Xunit;

    public enum Color { Red, Green, Blue }

    public class ConverterTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static TheoryData<Type, Type> FactoryMappings => new TheoryData<Type, Type>
        {
            { typeof(DateTime), typeof(DateTimeConverter) },
            { typeof(DateTime?), typeof(DateTimeConverter) },
            { typeof(DateTimeOffset), typeof(DateTimeOffsetConverter) },
            { typeof(TimeSpan), typeof(TimeSpanConverter) },
            { typeof(ushort), typeof(UInt16Converter) },
            { typeof(uint), typeof(UInt32Converter) },
            { typeof(ulong), typeof(UInt64Converter) },
            { typeof(short), typeof(Int16Converter) },
            { typeof(int), typeof(Int32Converter) },
            { typeof(int?), typeof(Int32Converter) },
            { typeof(long), typeof(Int64Converter) },
            { typeof(BigInteger), typeof(BigIntegerConverter) },
            { typeof(decimal), typeof(DecimalConverter) },
            { typeof(float), typeof(FloatConverter) },
            { typeof(double), typeof(DoubleConverter) },
            { typeof(bool), typeof(BooleanConverter) },
            { typeof(Uri), typeof(UriConverter) },
            { typeof(Color), typeof(EnumConverter) },
            { typeof(Color?), typeof(EnumConverter) },
            { typeof(sbyte), typeof(SByteConverter) },
            { typeof(byte), typeof(ByteConverter) },
            { typeof(byte[]), typeof(ByteArrayConverter) },
            { typeof(char), typeof(CharConverter) },
            { typeof(Guid), typeof(GuidConverter) },
            { typeof(string), typeof(StringConverter) },
#if NET6_0_OR_GREATER
            { typeof(DateOnly), typeof(DateOnlyConverter) },
            { typeof(TimeOnly), typeof(TimeOnlyConverter) },
#endif
            { typeof(object), typeof(DefaultValueConverter) },
        };

        [Theory]
        [MemberData(nameof(FactoryMappings))]
        public void Factory_CreatesConverterByType(Type valueType, Type converterType)
        {
            var converter = ValueConverterFactory.CreateValueConverterByType(valueType);

            Assert.IsType(converterType, converter);
        }

        public static TheoryData<IValueConverter, string, Type, object> ValidValues => new TheoryData<IValueConverter, string, Type, object>
        {
            { new Int16Converter(), "-12", typeof(short), (short)-12 },
            { new Int32Converter(), "123456", typeof(int), 123456 },
            { new Int64Converter(), "9876543210", typeof(long), 9876543210L },
            { new UInt16Converter(), "65535", typeof(ushort), (ushort)65535 },
            { new UInt32Converter(), "4000000000", typeof(uint), 4000000000U },
            { new UInt64Converter(), "18000000000000000000", typeof(ulong), 18000000000000000000UL },
            { new ByteConverter(), "255", typeof(byte), (byte)255 },
            { new SByteConverter(), "-128", typeof(sbyte), (sbyte)-128 },
            { new DecimalConverter(), "1234.56", typeof(decimal), 1234.56m },
            { new DoubleConverter(), "0.5", typeof(double), 0.5d },
            { new FloatConverter(), "2.25", typeof(float), 2.25f },
            { new BigIntegerConverter(), "123456789012345678901234567890", typeof(BigInteger), BigInteger.Parse("123456789012345678901234567890") },
            { new BooleanConverter(), "true", typeof(bool), true },
            { new BooleanConverter(), "False", typeof(bool), false },
            { new CharConverter(), "x", typeof(char), 'x' },
            { new GuidConverter(), "8c2d56f0-2b8b-4d8e-9a3a-3c1c0f0f7a11", typeof(Guid), Guid.Parse("8c2d56f0-2b8b-4d8e-9a3a-3c1c0f0f7a11") },
            { new EnumConverter(), "Green", typeof(Color), Color.Green },
            { new StringConverter(), "text", typeof(string), "text" },
            { new UriConverter(), "https://github.com/fmazzant/TinyCsv", typeof(Uri), new Uri("https://github.com/fmazzant/TinyCsv") },
            { new UriConverter(), "relative/path", typeof(Uri), new Uri("relative/path", UriKind.Relative) },
            { new TimeSpanConverter(), "01:02:03", typeof(TimeSpan), new TimeSpan(1, 2, 3) },
            { new DateTimeConverter(), "2024-01-31", typeof(DateTime), new DateTime(2024, 1, 31) },
            { new DateTimeOffsetConverter(), "2024-01-31T10:00:00+02:00", typeof(DateTimeOffset), new DateTimeOffset(2024, 1, 31, 10, 0, 0, TimeSpan.FromHours(2)) },
            { new DefaultValueConverter(), "42", typeof(int), 42 },
        };

        [Theory]
        [MemberData(nameof(ValidValues))]
        public void ConvertBack_ValidValue(IValueConverter converter, string value, Type targetType, object expected)
        {
            var result = converter.ConvertBack(value, targetType, null, Invariant);

            Assert.Equal(expected, result);
        }

        public static TheoryData<IValueConverter, Type, object> InvalidValues => new TheoryData<IValueConverter, Type, object>
        {
            { new Int16Converter(), typeof(short), (short)0 },
            { new Int32Converter(), typeof(int), 0 },
            { new Int64Converter(), typeof(long), 0L },
            { new UInt16Converter(), typeof(ushort), (ushort)0 },
            { new UInt32Converter(), typeof(uint), 0U },
            { new UInt64Converter(), typeof(ulong), 0UL },
            { new ByteConverter(), typeof(byte), (byte)0 },
            { new SByteConverter(), typeof(sbyte), (sbyte)0 },
            { new DecimalConverter(), typeof(decimal), 0m },
            { new DoubleConverter(), typeof(double), 0d },
            { new FloatConverter(), typeof(float), 0f },
            { new BigIntegerConverter(), typeof(BigInteger), BigInteger.Zero },
            { new BooleanConverter(), typeof(bool), false },
            { new CharConverter(), typeof(char), '\0' },
            { new GuidConverter(), typeof(Guid), Guid.Empty },
            { new TimeSpanConverter(), typeof(TimeSpan), TimeSpan.Zero },
            { new DateTimeConverter(), typeof(DateTime), default(DateTime) },
            { new DateTimeOffsetConverter(), typeof(DateTimeOffset), default(DateTimeOffset) },
        };

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void ConvertBack_InvalidValue_ReturnsDefault(IValueConverter converter, Type targetType, object expected)
        {
            var result = converter.ConvertBack("not a value", targetType, null, Invariant);

            Assert.Equal(expected, result);
        }

        public static TheoryData<IValueConverter, Type> NullableTypes => new TheoryData<IValueConverter, Type>
        {
            { new Int32Converter(), typeof(int?) },
            { new Int64Converter(), typeof(long?) },
            { new DecimalConverter(), typeof(decimal?) },
            { new DoubleConverter(), typeof(double?) },
            { new BooleanConverter(), typeof(bool?) },
            { new CharConverter(), typeof(char?) },
            { new GuidConverter(), typeof(Guid?) },
            { new TimeSpanConverter(), typeof(TimeSpan?) },
            { new DateTimeConverter(), typeof(DateTime?) },
            { new DateTimeOffsetConverter(), typeof(DateTimeOffset?) },
        };

        [Theory]
        [MemberData(nameof(NullableTypes))]
        public void ConvertBack_EmptyValue_NullableType_ReturnsNull(IValueConverter converter, Type targetType)
        {
            var result = converter.ConvertBack(string.Empty, targetType, null, Invariant);

            Assert.Null(result);
        }

        [Fact(Skip = KnownBug.NullableEnum)]
        public void EnumConverter_NullableType_EmptyValue_ReturnsNull()
        {
            Assert.Null(new EnumConverter().ConvertBack(string.Empty, typeof(Color?), null, Invariant));
        }

        [Fact(Skip = KnownBug.NullableEnum)]
        public void EnumConverter_NullableType_ParsesValue()
        {
            var result = new EnumConverter().ConvertBack("Blue", typeof(Color?), null, Invariant);

            Assert.Equal(Color.Blue, result);
        }

        [Fact(Skip = KnownBug.EnumUnknownValue)]
        public void EnumConverter_UnknownValue_ReturnsDefaultEnumValue()
        {
            var result = new EnumConverter().ConvertBack("Purple", typeof(Color), null, Invariant);

            Assert.Equal(Color.Red, result);
        }

        [Fact]
        public void StringConverter_Null_ReturnsNull()
        {
            Assert.Null(new StringConverter().ConvertBack(null, typeof(string), null, Invariant));
        }

        [Fact]
        public void ByteArrayConverter_RoundTrip()
        {
            var converter = new ByteArrayConverter();
            var bytes = new byte[] { 1, 2, 3, 250 };

            var text = converter.Convert(bytes, null, Invariant);
            var result = converter.ConvertBack(text, typeof(byte[]), null, Invariant);

            Assert.Equal(Convert.ToBase64String(bytes), text);
            Assert.Equal(bytes, result);
        }

        [Fact]
        public void ByteArrayConverter_EmptyOrNull()
        {
            var converter = new ByteArrayConverter();

            Assert.Null(converter.Convert(null, null, Invariant));
            Assert.Null(converter.ConvertBack("", typeof(byte[]), null, Invariant));
        }

        [Fact]
        public void DefaultValueConverter_Convert_UsesProvider()
        {
            var converter = new DefaultValueConverter();

            Assert.Equal("1234.5", converter.Convert(1234.5m, null, Invariant));
            Assert.Equal("1234,5", converter.Convert(1234.5m, null, new CultureInfo("it-IT")));
            Assert.Equal(string.Empty, converter.Convert(null, null, Invariant));
        }

        [Fact]
        public void DecimalConverter_UsesProvider()
        {
            var result = new DecimalConverter().ConvertBack("1.234,5", typeof(decimal), null, new CultureInfo("it-IT"));

            Assert.Equal(1234.5m, result);
        }

        [Fact]
        public void DateTimeConverter_WithCustomFormat()
        {
            var provider = new CsvColumnFormat("dd/MM/yyyy").Provider;

            var result = new DateTimeConverter().ConvertBack("31/01/2024", typeof(DateTime), null, provider);

            Assert.Equal(new DateTime(2024, 1, 31), result);
        }

        [Fact]
        public void DateTimeConverter_WithCustomFormat_WrongValue_ReturnsDefault()
        {
            var provider = new CsvColumnFormat("dd/MM/yyyy").Provider;

            var result = new DateTimeConverter().ConvertBack("2024-01-31", typeof(DateTime), null, provider);

            Assert.Equal(default(DateTime), result);
        }

        [Fact]
        public void TimeSpanConverter_WithCustomFormat()
        {
            var provider = new CsvColumnFormat(@"hh\-mm").Provider;

            var result = new TimeSpanConverter().ConvertBack("10-30", typeof(TimeSpan), null, provider);

            Assert.Equal(new TimeSpan(10, 30, 0), result);
        }

        [Fact]
        public void DateTimeOffsetConverter_WithCustomFormat()
        {
            var provider = new CsvColumnFormat("yyyyMMddzzz").Provider;

            var result = new DateTimeOffsetConverter().ConvertBack("20240131+01:00", typeof(DateTimeOffset), null, provider);

            Assert.Equal(new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.FromHours(1)), result);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void DateOnlyConverter()
        {
            var converter = new DateOnlyConverter();

            Assert.Equal(new DateOnly(2024, 1, 31), converter.ConvertBack("2024-01-31", typeof(DateOnly), null, Invariant));
            Assert.Equal(new DateOnly(2024, 1, 31), converter.ConvertBack("31.01.2024", typeof(DateOnly), null, new CsvColumnFormat("dd.MM.yyyy").Provider));
            Assert.Equal(default(DateOnly), converter.ConvertBack("x", typeof(DateOnly), null, Invariant));
            Assert.Null(converter.ConvertBack("", typeof(DateOnly?), null, Invariant));
        }

        [Fact]
        public void TimeOnlyConverter()
        {
            var converter = new TimeOnlyConverter();

            Assert.Equal(new TimeOnly(10, 30), converter.ConvertBack("10:30", typeof(TimeOnly), null, Invariant));
            Assert.Equal(new TimeOnly(10, 30), converter.ConvertBack("10.30", typeof(TimeOnly), null, new CsvColumnFormat("HH.mm").Provider));
            Assert.Equal(default(TimeOnly), converter.ConvertBack("x", typeof(TimeOnly), null, Invariant));
            Assert.Null(converter.ConvertBack("", typeof(TimeOnly?), null, Invariant));
        }
#endif

        /// <summary>
        /// Gets the format provider that TinyCsv creates for a column with a custom format
        /// </summary>
        private sealed class CsvColumnFormat
        {
            public CsvColumnFormat(string format)
            {
                var options = new CsvOptions<Person>();
                options.Columns.AddColumn(m => m.Name, format);
                foreach (var column in options.Columns)
                {
                    Provider = column.ColumnFormatProvider;
                }
            }

            public IFormatProvider Provider { get; }
        }
    }
}
