namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Extensions;
    using Xunit;

    public class FormattedModel
    {
        public int Int { get; set; }
        public long Long { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public DateTime DateTime { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public uint UInt { get; set; }
        public ulong ULong { get; set; }
        public ushort UShort { get; set; }
        public sbyte SByte { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public Guid Guid { get; set; }
        public bool Bool { get; set; }
        public int? NullableInt { get; set; }
        public FastPathKind Kind { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly DateOnly { get; set; }
        public TimeOnly TimeOnly { get; set; }
#endif
    }

    /// <summary>
    /// The writer builds the lines in a buffer (escaping and formatting the values without creating strings):
    /// the text must be the same of the fields escaped one by one with EnclosedInQuotesIfNecessary (GetLineFromGenericType)
    /// </summary>
    public class WriterOutputTests
    {
        private const string Alphabet = "ab ,;|\t\"'\\\r\nè€";

        private static Person[] RandomPeople(int seed)
        {
            var random = new Random(seed);
            string RandomValue()
            {
                var length = random.Next(-1, 10);
                return length < 0 ? null : new string(Enumerable.Range(0, length).Select(_ => Alphabet[random.Next(Alphabet.Length)]).ToArray());
            }
            return Enumerable.Range(0, 300).Select(i => new Person { Id = i, Name = RandomValue(), City = RandomValue() }).ToArray();
        }

        private static string Expected<T>(TinyCsv<T> csv, T[] models, string newLine)
            where T : class, new()
        {
            return string.Concat(models.Select(m => m.GetLineFromGenericType(csv.Options) + newLine));
        }

        private static string Save<T>(TinyCsv<T> csv, T[] models)
            where T : class, new()
        {
            var stream = new MemoryStream();
            csv.Save(stream, models);
            return new UTF8Encoding(false).GetString(stream.ToArray());
        }

        [Theory]
        [InlineData(",")]
        [InlineData(";")]
        [InlineData("\t")]
        [InlineData("||")]
        [InlineData(";;;")]
        [InlineData("")]
        public async Task Strings_AreEscapedAsEnclosedInQuotesIfNecessary(string delimiter)
        {
            foreach (var quoting in new[] { true, false })
            {
                foreach (var backslash in new[] { false, true })
                {
                    foreach (var quote in new[] { '"', '\'' })
                    {
                        var csv = Csv.Person(o =>
                        {
                            o.Delimiter = delimiter;
                            o.NewLine = "\r\n";
                            o.AllowRowEnclosedInDoubleQuotesValues = quoting;
                            o.AllowBackSlashToEscapeQuote = backslash;
                            o.DoubleQuotes = quote;
                        });
                        var people = RandomPeople(delimiter.Length * 8 + (quoting ? 4 : 0) + (backslash ? 2 : 0) + (quote == '"' ? 1 : 0));
                        var expected = Expected(csv, people, "\r\n");

                        Assert.Equal(expected, Save(csv, people));
                        Assert.Equal(expected, string.Concat(csv.GetAllLines(people).Select(line => line + "\r\n")));

                        var stream = new MemoryStream();
                        await csv.SaveAsync(stream, people);
                        Assert.Equal(expected, new UTF8Encoding(false).GetString(stream.ToArray()));
                    }
                }
            }
        }

        [Theory]
        [InlineData(",", "", null)]
        [InlineData(";", "it-IT", null)]
        [InlineData(",", "it-IT", null)]
        [InlineData(",", "fr-FR", null)]
        [InlineData(".", "en-US", null)]
        [InlineData(":", "en-US", null)]
        [InlineData(";", "", "N2")]
        [InlineData(",", "de-DE", "N2")]
        public void FormattedValues_AreTheSameOfTheConverters(string delimiter, string culture, string numberFormat)
        {
            var random = new Random(culture.Length + delimiter[0]);
            var models = Enumerable.Range(0, 300).Select(i => new FormattedModel
            {
                Int = random.Next(int.MinValue, int.MaxValue),
                Long = (long)random.Next() * random.Next(-100000, 100000),
                Decimal = random.Next(-10_000_000, 10_000_000) / 1000m,
                Double = random.NextDouble() * random.Next(-100000, 100000),
                Float = (float)random.NextDouble() * 1000,
                DateTime = new DateTime(2000, 1, 1).AddTicks((long)(random.NextDouble() * TimeSpan.TicksPerDay * 20000)),
                DateTimeOffset = new DateTimeOffset(new DateTime(2000, 1, 1).AddMinutes(random.Next(0, 10_000_000)), TimeSpan.FromHours(random.Next(-12, 13))),
                Short = (short)random.Next(short.MinValue, short.MaxValue),
                Byte = (byte)random.Next(256),
                UInt = (uint)random.Next() * 2,
                ULong = (ulong)random.Next() * (ulong)random.Next(),
                UShort = (ushort)random.Next(ushort.MaxValue),
                SByte = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue),
                TimeSpan = TimeSpan.FromTicks((long)(random.NextDouble() * TimeSpan.TicksPerDay * 3) * (random.Next(2) == 0 ? -1 : 1)),
                Guid = new Guid(Enumerable.Range(0, 16).Select(_ => (byte)random.Next(256)).ToArray()),
                Bool = random.Next(2) == 0,
                NullableInt = random.Next(3) == 0 ? (int?)null : random.Next(),
                Kind = (FastPathKind)random.Next(2),
#if NET6_0_OR_GREATER
                DateOnly = DateOnly.FromDayNumber(random.Next(700_000, 800_000)),
                TimeOnly = new TimeOnly(random.Next(0, 24), random.Next(0, 60), random.Next(0, 60)),
#endif
            }).ToArray();
            var csv = new TinyCsv<FormattedModel>(o =>
            {
                o.Delimiter = delimiter;
                o.NewLine = "\n";
                o.Culture = new CultureInfo(culture);
                o.Columns.AddColumn(m => m.Int, numberFormat);
                o.Columns.AddColumn(m => m.Long, numberFormat);
                o.Columns.AddColumn(m => m.Decimal, numberFormat);
                o.Columns.AddColumn(m => m.Double, numberFormat);
                o.Columns.AddColumn(m => m.Float, numberFormat);
                o.Columns.AddColumn(m => m.DateTime);
                o.Columns.AddColumn(m => m.DateTime, "yyyy-MM-dd HH:mm:ss.fff");
                o.Columns.AddColumn(m => m.DateTimeOffset);
                o.Columns.AddColumn(m => m.Short, numberFormat);
                o.Columns.AddColumn(m => m.Byte, numberFormat);
                o.Columns.AddColumn(m => m.UInt, numberFormat);
                o.Columns.AddColumn(m => m.ULong, numberFormat);
                o.Columns.AddColumn(m => m.UShort, numberFormat);
                o.Columns.AddColumn(m => m.SByte, numberFormat);
                o.Columns.AddColumn(m => m.TimeSpan);
                o.Columns.AddColumn(m => m.Guid);
                o.Columns.AddColumn(m => m.Bool);
                o.Columns.AddColumn(m => m.NullableInt);
                o.Columns.AddColumn(m => m.Kind);
#if NET6_0_OR_GREATER
                o.Columns.AddColumn(m => m.DateOnly);
                o.Columns.AddColumn(m => m.TimeOnly);
#endif
            });

            Assert.Equal(Expected(csv, models, "\n"), Save(csv, models));
        }
    }
}
