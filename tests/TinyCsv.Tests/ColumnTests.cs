namespace TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Threading;
    using TinyCsv.Conversions;
    using Xunit;

    public class AllTypes
    {
        public short Int16 { get; set; }
        public int Int32 { get; set; }
        public long Int64 { get; set; }
        public ushort UInt16 { get; set; }
        public uint UInt32 { get; set; }
        public ulong UInt64 { get; set; }
        public byte Byte { get; set; }
        public sbyte SByte { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public BigInteger BigInteger { get; set; }
        public bool Boolean { get; set; }
        public char Char { get; set; }
        public Guid Guid { get; set; }
        public Color Color { get; set; }
        public string String { get; set; }
        public Uri Uri { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public DateTime DateTime { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public byte[] Bytes { get; set; }
        public int? NullableInt { get; set; }
    }

    public class Base64Converter : IValueConverter
    {
        public string Convert(object value, object parameter, IFormatProvider provider) => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{value}"));
        public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(value));
    }

    public class ColumnTests
    {
        private static TinyCsv<AllTypes> AllTypesCsv()
        {
            return new TinyCsv<AllTypes>(o =>
            {
                o.Delimiter = ";";
                o.HasHeaderRecord = true;
                o.Columns.AddColumn(m => m.Int16);
                o.Columns.AddColumn(m => m.Int32);
                o.Columns.AddColumn(m => m.Int64);
                o.Columns.AddColumn(m => m.UInt16);
                o.Columns.AddColumn(m => m.UInt32);
                o.Columns.AddColumn(m => m.UInt64);
                o.Columns.AddColumn(m => m.Byte);
                o.Columns.AddColumn(m => m.SByte);
                o.Columns.AddColumn(m => m.Decimal);
                o.Columns.AddColumn(m => m.Double);
                o.Columns.AddColumn(m => m.Float);
                o.Columns.AddColumn(m => m.BigInteger);
                o.Columns.AddColumn(m => m.Boolean);
                o.Columns.AddColumn(m => m.Char);
                o.Columns.AddColumn(m => m.Guid);
                o.Columns.AddColumn(m => m.Color);
                o.Columns.AddColumn(m => m.String);
                o.Columns.AddColumn(m => m.Uri);
                o.Columns.AddColumn(m => m.TimeSpan);
                o.Columns.AddColumn(m => m.DateTime, "yyyy-MM-dd HH:mm:ss");
                o.Columns.AddColumn(m => m.DateTimeOffset, "yyyy-MM-ddTHH:mm:sszzz");
                o.Columns.AddColumn(m => m.Bytes);
                o.Columns.AddColumn(m => m.NullableInt);
            });
        }

        private static AllTypes Sample() => new AllTypes
        {
            Int16 = -12,
            Int32 = 123456,
            Int64 = 9876543210,
            UInt16 = 65535,
            UInt32 = 4000000000,
            UInt64 = 18000000000000000000,
            Byte = 255,
            SByte = -128,
            Decimal = 1234.56m,
            Double = 0.5,
            Float = 2.25f,
            BigInteger = BigInteger.Parse("123456789012345678901234567890"),
            Boolean = true,
            Char = 'x',
            Guid = Guid.Parse("8c2d56f0-2b8b-4d8e-9a3a-3c1c0f0f7a11"),
            Color = Color.Blue,
            String = "Mario; \"Super\"",
            Uri = new Uri("https://github.com/fmazzant/TinyCsv"),
            TimeSpan = new TimeSpan(1, 2, 3),
            DateTime = new DateTime(2024, 1, 31, 10, 20, 30),
            DateTimeOffset = new DateTimeOffset(2024, 1, 31, 10, 20, 30, TimeSpan.FromHours(2)),
            Bytes = new byte[] { 1, 2, 3 },
            NullableInt = null,
        };

        private static void AssertEqual(AllTypes expected, AllTypes actual)
        {
            foreach (var property in typeof(AllTypes).GetProperties())
            {
                Assert.Equal(property.GetValue(expected), property.GetValue(actual));
            }
        }

        [Fact]
        public void AllTypes_RoundTrip()
        {
            var csv = AllTypesCsv();
            var sample = Sample();

            var result = csv.LoadFromText(csv.GetAllText(new[] { sample })).Single();

            AssertEqual(sample, result);
        }

        [Fact]
        public void AllTypes_RoundTrip_IsCultureIndependent()
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("it-IT");
                var csv = AllTypesCsv();
                var sample = Sample();

                var text = csv.GetAllText(new[] { sample });
                var result = csv.LoadFromText(text).Single();

                Assert.Contains("1234.56", text);
                AssertEqual(sample, result);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Fact]
        public void AllTypes_Header()
        {
            var csv = AllTypesCsv();

            var header = csv.GetAllLines(new AllTypes[0]).Single();

            Assert.Equal(string.Join(";", typeof(AllTypes).GetProperties().Select(p => p.Name)), header);
        }

        [Fact]
        public void AddColumn_WithName_UsedInHeader()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.HasHeaderRecord = true;
                o.Columns.AddColumn("Codice", m => m.Id);
                o.Columns.AddColumn("Nome", m => m.Name);
            });

            var lines = csv.GetAllLines(new[] { new Person { Id = 1, Name = "Mario" } });

            Assert.Equal(new[] { "Codice;Nome", "1;Mario" }, lines);
        }

        [Fact]
        public void AddColumn_WithIndex_ReadsFromThatPosition()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.Columns.AddColumn(2, m => m.Name);
                o.Columns.AddColumn(0, m => m.Id);
            });

            var result = csv.LoadFromText("7;ignored;Mario").Single();

            Assert.Equal(7, result.Id);
            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void AddColumn_WithFormat_WritesAndReads()
        {
            var csv = new TinyCsv<AttributePerson>(o =>
            {
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.BirthDate, "dd/MM/yyyy");
            });
            var people = new[] { new AttributePerson { Id = 1, BirthDate = new DateTime(1980, 5, 12) } };

            var line = csv.GetAllLines(people).Single();
            var result = csv.LoadFromText(line).Single();

            Assert.Equal("1;12/05/1980", line);
            Assert.Equal(new DateTime(1980, 5, 12), result.BirthDate);
        }

        [Fact]
        public void AddColumn_WithFormatProvider()
        {
            var italian = new CultureInfo("it-IT");
            var csv = new TinyCsv<AllTypes>(o =>
            {
                o.Columns.AddColumn(m => m.Int32);
                o.Columns.AddColumn(m => m.Decimal, italian);
            });

            var line = csv.GetAllLines(new[] { new AllTypes { Int32 = 1, Decimal = 1234.5m } }).Single();
            var result = csv.LoadFromText("1;\"1.234,5\"").Single();

            Assert.Equal("1;1234,5", line);
            Assert.Equal(1234.5m, result.Decimal);
        }

        [Fact]
        public void AddColumn_WithIndexNameAndFormatProvider()
        {
            var csv = new TinyCsv<AllTypes>(o =>
            {
                o.HasHeaderRecord = true;
                o.Columns.AddColumn(0, "Importo", m => m.Decimal, new CultureInfo("it-IT"));
            });

            var lines = csv.GetAllLines(new[] { new AllTypes { Decimal = 0.5m } });

            Assert.Equal(new[] { "Importo", "0,5" }, lines);
        }

        [Fact]
        public void AddColumn_WithConverter()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Name, new Base64Converter());
            });

            var line = csv.GetAllLines(new[] { new Person { Id = 1, Name = "Mario" } }).Single();
            var result = csv.LoadFromText(line).Single();

            Assert.Equal("1;TWFyaW8=", line);
            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void AddColumn_WithNameAndConverter()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.HasHeaderRecord = true;
                o.Columns.AddColumn("Encoded", m => m.Name, new Base64Converter());
            });

            Assert.Equal(new[] { "Encoded", "TWFyaW8=" }, csv.GetAllLines(new[] { new Person { Name = "Mario" } }));
        }

        [Fact]
        public void AddColumn_WithIndexNameAndConverter()
        {
            var csv = new TinyCsv<Person>(o => o.Columns.AddColumn(1, "Encoded", m => m.Name, new Base64Converter()));

            var result = csv.LoadFromText("x;TWFyaW8=").Single();

            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void Columns_ExposeMetadata()
        {
            var options = new CsvOptions<AttributePerson>();
            options.Columns.AddColumn(m => m.Id);
            options.Columns.AddColumn("Birth", m => m.BirthDate, "yyyy");

            var columns = options.Columns.ToList();

            Assert.Equal(2, options.Columns.Count);
            Assert.Equal(0, columns[0].ColumnIndex);
            Assert.Equal("Id", columns[0].ColumnName);
            Assert.Equal(typeof(int), columns[0].ColumnType);
            Assert.IsType<Int32Converter>(columns[0].Converter);
            Assert.Same(CultureInfo.InvariantCulture, columns[0].ColumnFormatProvider);
            Assert.Equal(1, columns[1].ColumnIndex);
            Assert.Equal("Birth", columns[1].ColumnName);
            Assert.Equal("yyyy", columns[1].ColumnFormat);
            Assert.NotNull(columns[1].ColumnExpression);
            Assert.IsNotType<CultureInfo>(columns[1].ColumnFormatProvider);
        }

        [Fact]
        public void AsColumnsHeaderLine_UsesDelimiter()
        {
            var options = new CsvOptions<Person> { Delimiter = "|" };
            options.Columns.AddColumn(m => m.Id);
            options.Columns.AddColumn(m => m.Name);

            Assert.Equal("Id|Name", options.AsColumnsHeaderLine());
        }

        [Fact]
        public void NullableValue_WrittenAsEmpty()
        {
            var csv = new TinyCsv<AllTypes>(o =>
            {
                o.Columns.AddColumn(m => m.Int32);
                o.Columns.AddColumn(m => m.NullableInt);
            });

            var line = csv.GetAllLines(new[] { new AllTypes { Int32 = 1, NullableInt = null } }).Single();
            var result = csv.LoadFromText("1;").Single();

            Assert.Equal("1;", line);
            Assert.Null(result.NullableInt);
        }
    }
}
