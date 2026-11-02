namespace TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TinyCsv.Conversions;
    using TinyCsv.Exceptions;
    using TinyCsv.Extensions;
    using Xunit;

    public enum FastPathKind
    {
        None,
        Some,
    }

    public class FastPathModel
    {
        public int Id { get; set; }
        public long Long { get; set; }
        public uint UInt { get; set; }
        public decimal Price { get; set; }
        public double Ratio { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }
        public byte Byte { get; set; }
        public Guid Guid { get; set; }
        public int? NullableInt { get; set; }
        public FastPathKind Kind { get; set; }
        public string Text { get; set; }
        public string Encoded { get; set; }
    }

    /// <summary>
    /// The typed accessors, the line builder and the span parsing (the fast paths)
    /// return the same result of the converters
    /// </summary>
    public class FastPathTests
    {
        private static readonly FastPathModel[] Models =
        {
            new FastPathModel
            {
                Id = 1, Long = -5_000_000_000, UInt = 7, Price = 1234.5m, Ratio = 0.25, Date = new DateTime(2026, 11, 2, 10, 30, 0),
                Active = true, Byte = 200, Guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), NullableInt = 3,
                Kind = FastPathKind.Some, Text = "plain", Encoded = "hello",
            },
            new FastPathModel
            {
                Id = -2, Price = -0.01m, Date = new DateTime(2000, 1, 1), Text = "with; \"quotes\"\r\nand new line", Encoded = null,
            },
            new FastPathModel { Text = null, Encoded = "" },
        };

        private static TinyCsv<FastPathModel> Create(Action<CsvOptions<FastPathModel>> configure = null)
        {
            return new TinyCsv<FastPathModel>(options =>
            {
                options.HasHeaderRecord = true;
                options.Delimiter = ";";
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Long);
                options.Columns.AddColumn(m => m.UInt);
                options.Columns.AddColumn(m => m.Price);
                options.Columns.AddColumn(m => m.Ratio);
                options.Columns.AddColumn(m => m.Date, "yyyy-MM-dd HH:mm");
                options.Columns.AddColumn(m => m.Active);
                options.Columns.AddColumn(m => m.Byte);
                options.Columns.AddColumn(m => m.Guid);
                options.Columns.AddColumn(m => m.NullableInt);
                options.Columns.AddColumn(m => m.Kind);
                options.Columns.AddColumn(m => m.Text);
                options.Columns.AddColumn(m => m.Encoded, new Base64Converter());
                configure?.Invoke(options);
            });
        }

        [Theory]
        [InlineData(";", "")]
        [InlineData(",", "it-IT")]
        [InlineData("||", "it-IT")]
        public void GetAllLines_IsTheSameOfGetLineFromGenericType(string delimiter, string culture)
        {
            var csv = Create(o =>
            {
                o.Delimiter = delimiter;
                o.Culture = new CultureInfo(culture);
            });

            var lines = csv.GetAllLines(Models).Skip(1);

            Assert.Equal(Models.Select(m => m.GetLineFromGenericType(csv.Options)), lines);
        }

        [Fact]
        public void Save_EscapesAFormattedValueWithTheDelimiter()
        {
            var csv = Create(o =>
            {
                o.Delimiter = ",";
                o.Culture = new CultureInfo("it-IT");
            });

            var lines = csv.GetAllLines(Models);

            Assert.StartsWith("1,-5000000000,7,\"1234,5\",\"0,25\",2026-11-02 10:30,True,", lines[1]);
            AssertModels(Models, csv.LoadFromText(csv.GetAllText(Models)).ToArray());
        }

        [Fact]
        public async Task Save_SaveAsync_AndGetAllText_WriteTheSameText()
        {
            var csv = Create();

            var saved = SaveToString(stream => csv.Save(stream, Models));
            var savedAsync = await SaveToStringAsync(stream => csv.SaveAsync(stream, Models));

            Assert.Equal(csv.GetAllText(Models).Replace("\r\n", "\n"), saved.Replace("\r\n", "\n").TrimEnd('\n'));
            Assert.Equal(saved, savedAsync);
        }

        [Fact]
        public void Save_WithHandlers_RaisesRowWrittenWithTheLine()
        {
            var csv = Create(o => o.EnableHandlers = true);
            var rows = new System.Collections.Generic.List<string>();
            csv.Options.Handlers.Write.RowWritten += (sender, e) => rows.Add(e.Row);

            var saved = SaveToString(stream => csv.Save(stream, Models));

            Assert.Equal(Models.Select(m => m.GetLineFromGenericType(csv.Options)), rows);
            Assert.Equal(saved.Replace("\r\n", "\n").TrimEnd('\n'), csv.GetAllText(Models).Replace("\r\n", "\n"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadFromText_IsTheSameOfGetModelFromStringArray(bool trimData)
        {
            var csv = Create(o => o.TrimData = trimData);
            var text = csv.GetAllText(Models);

            var models = csv.LoadFromText(text).ToArray();
            var expected = csv.GetAllLinesAndFieldsFromText(text).Skip(1)
                .Select(fields => fields.GetModelFromStringArray(csv.Options))
                .ToArray();

            AssertModels(expected, models);
            AssertModels(Models, models);
        }

        [Theory]
        [InlineData("1,000", 1000)]
        [InlineData(" 42 ", 42)]
        [InlineData("-7", -7)]
        [InlineData("(5)", -5)]
        [InlineData("1e3", 1000)]
        public void LoadFromText_ParsesTheIntegersWithNumberStylesAny(string value, int expected)
        {
            var csv = new TinyCsv<FastPathModel>(options =>
            {
                options.HasHeaderRecord = false;
                options.Delimiter = ";";
                options.Columns.AddColumn(m => m.Id);
            });

            var model = csv.LoadFromText($"\"{value}\"").Single();

            Assert.Equal(expected, model.Id);
            Assert.Equal(int.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture), model.Id);
        }

        [Theory]
        [InlineData("  ")]
        [InlineData("abc")]
        public void LoadFromText_NotValidValue_IsTheDefaultValue(string value)
        {
            var csv = Create(o => o.HasHeaderRecord = false);

            var model = csv.LoadFromText($"{value};{value};;;;;;;;{value}").Single();

            Assert.Equal(0, model.Id);
            Assert.Equal(0, model.Long);
            Assert.Null(model.NullableInt);
        }

        [Fact]
        public void LoadFromText_ValidateValues_ThrowsWithTheValue()
        {
            var csv = Create(o =>
            {
                o.HasHeaderRecord = false;
                o.ValidateValues = true;
            });

            var exception = Assert.Throws<InvalidColumnValueException>(() => csv.LoadFromText("1;x2").ToArray());

            Assert.Equal("Long", exception.ColumnName);
            Assert.Equal("x2", exception.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("utf-8")]
        [InlineData("utf-16")]
        public void LoadFromText_WithAByteOrderMark_IgnoresIt(string encoding)
        {
            var csv = Create(o => o.HasHeaderRecord = false);

            var model = csv.LoadFromText("﻿12;3", encoding == null ? null : Encoding.GetEncoding(encoding)).Single();

            Assert.Equal(12, model.Id);
            Assert.Equal(3, model.Long);
        }

        [Fact]
        public void LoadFromText_NotUtf8Encoding_ReadsTheEncodedText()
        {
            var csv = Create(o => o.HasHeaderRecord = false);

            // "é" is not an ASCII char, so it is read as "?"
            var model = csv.LoadFromText("1" + new string(';', 11) + "é", Encoding.ASCII).Single();

            Assert.Equal("?", model.Text);
        }

        /// <summary>
        /// Many records, some with multi-line quoted values and one longer than the buffer of the reader,
        /// so the fields that refer to the buffer are read across the moves and the growth of the buffer
        /// </summary>
        private static Person[] ManyPeople()
        {
            return Enumerable.Range(0, 3000).Select(i => new Person
            {
                Id = i,
                Name = i == 1500 ? new string('x', 10_000) : i % 7 == 0 ? $"Name; {i}\r\nsecond \"line\"" : $"Name {i}",
                City = i % 5 == 0 ? "" : $"City {i}",
            }).ToArray();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LoadFromText_ManyRecords_AcrossTheBuffer(bool fromStream)
        {
            var people = ManyPeople();
            var csv = Csv.Person(o => o.HasHeaderRecord = true);
            var text = csv.GetAllText(people);

            var read = fromStream
                ? csv.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(text))).ToArray()
                : csv.LoadFromText(text).ToArray();

            Assert.Equal(people.Select(p => (p.Id, p.Name, p.City)), read.Select(p => (p.Id, p.Name, p.City ?? "")));
        }

#if NET5_0_OR_GREATER
        [Fact]
        public async Task LoadFromTextAsync_ManyRecords_AcrossTheBuffer()
        {
            var people = ManyPeople();
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var read = new System.Collections.Generic.List<Person>();
            await foreach (var person in csv.LoadFromTextAsync(csv.GetAllText(people)))
            {
                read.Add(person);
            }

            Assert.Equal(people.Select(p => (p.Id, p.Name, p.City)), read.Select(p => (p.Id, p.Name, p.City ?? "")));
        }
#endif

        [Fact]
        public void LoadFromText_SkipRow_ReceivesTheRecordAndTheIndex()
        {
            var calls = new System.Collections.Generic.List<(string, int)>();
            var csv = Csv.Person(o =>
            {
                o.RowsToSkip = 1;
                o.SkipRow = (row, index) =>
                {
                    calls.Add((row, index));
                    return row.Contains("skip");
                };
            });

            var people = csv.LoadFromText("title\n1;a;\"x\ny\"\n#comment\n2;skip;z\n3;c;w").ToArray();

            Assert.Equal(new[] { 1, 3 }, people.Select(p => p.Id));
            Assert.Equal("x\ny", people[0].City);
            Assert.Equal(new[] { ("1;a;\"x\ny\"", 1), ("2;skip;z", 3), ("3;c;w", 4) }, calls);
        }

        [Fact]
        public void LoadFromText_Events_ReceiveTheRecord()
        {
            var csv = Csv.Person(o => o.EnableHandlers = true);
            var reading = new System.Collections.Generic.List<string>();
            var read = new System.Collections.Generic.List<string>();
            csv.Options.Handlers.Read.RowReading += (sender, e) => reading.Add(e.Row);
            csv.Options.Handlers.Read.RowRead += (sender, e) => read.Add($"{e.Index}:{e.Row}:{e.Model.Name}");

            var people = csv.LoadFromText("1;a;\"x\ny\"\r\n2;b;z").ToArray();

            Assert.Equal(2, people.Length);
            Assert.Equal(new[] { "1;a;\"x\ny\"", "2;b;z" }, reading);
            Assert.Equal(new[] { "0:1;a;\"x\ny\":a", "1:2;b;z:b" }, read);
        }

        [Fact]
        public void LoadFromText_BadDataFound_ReceivesTheRecord()
        {
            var errors = new System.Collections.Generic.List<CsvException>();
            var csv = Csv.Person(o =>
            {
                o.ValidateValues = true;
                o.BadDataFound = errors.Add;
            });

            var people = csv.LoadFromText("1;a;b\nx;c;d\n3;e;f").ToArray();

            Assert.Equal(new[] { 1, 3 }, people.Select(p => p.Id));
            var error = Assert.Single(errors);
            Assert.Equal("x;c;d", error.Row);
            Assert.Equal(1, error.RowIndex);
            Assert.Equal("x", error.Value);
        }

        [Fact]
        public void DecimalAndIntegerConverters_AreTheSameOfNumberStylesAny_InEveryCulture()
        {
            var values = new[] { "1234.5", "1,234.5", "1.234,5", "1 234,5", "-12", "12-", "(12)", "1e3", " 7 ", "1'234.5", "12,5", "¤5", "$5", "€ 5" };
            var decimalConverter = new DecimalConverter();
            var intConverter = new Int32Converter();
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                foreach (var value in values)
                {
                    decimal.TryParse(value, NumberStyles.Any, culture, out var expectedDecimal);
                    Assert.Equal(expectedDecimal, decimalConverter.ConvertBack(value, typeof(decimal), null, culture));

                    int.TryParse(value, NumberStyles.Any, culture, out var expectedInt);
                    Assert.Equal(expectedInt, intConverter.ConvertBack(value, typeof(int), null, culture));
                }
            }
        }

        public static System.Collections.Generic.IEnumerable<object[]> DateFormats => new[]
        {
            "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd.MM.yyyy", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss",
            "dd/MM/yyyy HH:mm:ss", "yyyyMMdd", "yyyyMMddHHmmss", "HH:mm", "yyyy-MM", "dd-MM-yyyy, HH.mm",
            "dd/MM/dd", "yyyy-MM-dd'T'HH", "d/M/yyyy", "yy-MM-dd", "yyyy-MM-dd hh:mm tt", "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy:MM:dd", "yyyy-MM-dd HH",
        }.Select(x => new object[] { x });

        /// <summary>
        /// The fast parser of the dates returns the same result of TryParseExact of DateTime, DateTimeOffset
        /// and DateOnly, in every culture. The dates include the changes of the daylight saving time
        /// (of Europe and of the United States) for the offset of the local time zone.
        /// </summary>
        [Theory]
        [MemberData(nameof(DateFormats))]
        public void DateConverters_AreTheSameOfTryParseExact_InEveryCulture(string format)
        {
            var dates = new[]
            {
                new DateTime(2022, 4, 2), new DateTime(2024, 2, 29, 23, 59, 59), new DateTime(1, 1, 1), new DateTime(9999, 12, 31, 12, 30, 5),
                new DateTime(1, 1, 1, 1, 0, 0), new DateTime(9999, 12, 31, 23, 0, 0),
                new DateTime(2022, 3, 27, 2, 30, 0), new DateTime(2022, 10, 30, 2, 30, 0),
                new DateTime(2022, 3, 13, 2, 30, 0), new DateTime(2022, 11, 6, 1, 30, 0),
            };
            var dateTimeConverter = new DateTimeConverter();
            var offsetConverter = new DateTimeOffsetConverter();
#if NET6_0_OR_GREATER
            var dateOnlyConverter = new DateOnlyConverter();
#endif
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                var provider = new CsvColumn { ColumnFormat = format }.GetFormatProvider(culture);
                var values = dates.Select(d => Format(d, format, culture)).Where(x => x != null)
                    .Concat(new[]
                    {
                        "2023-02-29", "2022-13-01", "2022-00-10", "2022-04-31", "0000-01-01", "2022-04-02 24:00", "2022-04-02 23:60",
                        " 2022-04-02", "2022-04-02 ", "2022-4-02", "２０２２-04-02", "29/02/2023", "31/04/2022", "02/04/2022",
                        "2022/04/02", "02.04.2022", "20220402", "20220402235959", "12:30", "2022-04", "02-04-2022, 10.30",
                    });
                foreach (var value in values)
                {
                    var message = $"{culture.Name} '{format}' '{value}'";

                    var expected = DateTime.TryParseExact(value, format, culture, DateTimeStyles.None, out var expectedDate);
                    var actual = dateTimeConverter.TryConvertBack(value, typeof(DateTime), null, provider, out var actualDate);
                    Assert.True(expected == actual, $"DateTime {message}: {expected} != {actual}");
                    Assert.True(expectedDate == (DateTime)actualDate && expectedDate.Kind == ((DateTime)actualDate).Kind,
                        $"DateTime {message}: {expectedDate:O} != {actualDate:O}");

                    expected = DateTimeOffset.TryParseExact(value, format, culture, DateTimeStyles.None, out var expectedOffset);
                    actual = offsetConverter.TryConvertBack(value, typeof(DateTimeOffset), null, provider, out var actualOffset);
                    Assert.True(expected == actual, $"DateTimeOffset {message}: {expected} != {actual}");
                    Assert.True(expectedOffset.EqualsExact((DateTimeOffset)actualOffset), $"DateTimeOffset {message}: {expectedOffset:O} != {actualOffset:O}");

#if NET6_0_OR_GREATER
                    expected = DateOnly.TryParseExact(value, format, culture, DateTimeStyles.None, out var expectedDay);
                    actual = dateOnlyConverter.TryConvertBack(value, typeof(DateOnly), null, provider, out var actualDay);
                    Assert.True(expected == actual, $"DateOnly {message}: {expected} != {actual}");
                    Assert.True(expectedDay == (DateOnly)actualDay, $"DateOnly {message}: {expectedDay:O} != {actualDay:O}");
#endif
                }
            }
        }

        [Theory]
        [InlineData("yyyy-MM-dd", "", "2022-04-02")]
        [InlineData("dd/MM/yyyy HH:mm:ss", "it-IT", "02/04/2022 10:30:05")]
        [InlineData("dd.MM.yyyy", "de-DE", "02.04.2022")]
        [InlineData("yyyy-MM-ddTHH:mm", "en-US", "2022-04-02T10:30")]
        public void DateTimeFormatParser_ParsesTheCommonFormats(string format, string culture, string value)
        {
            var info = new CultureInfo(culture).DateTimeFormat;
            var parser = DateTimeFormatParser.Create(format, info);

            Assert.NotNull(parser);
            Assert.True(parser.TryParse(value.AsSpan(), out DateTime date));
            Assert.Equal(DateTime.ParseExact(value, format, info), date);
        }

        /// <summary>
        /// The date as string, or null when the calendar of the culture does not support it
        /// </summary>
        private static string Format(DateTime date, string format, CultureInfo culture)
        {
            try
            {
                return date.ToString(format, culture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static void AssertModels(FastPathModel[] expected, FastPathModel[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                var (e, a) = (expected[i], actual[i]);
                Assert.Equal(e.Id, a.Id);
                Assert.Equal(e.Long, a.Long);
                Assert.Equal(e.UInt, a.UInt);
                Assert.Equal(e.Price, a.Price);
                Assert.Equal(e.Ratio, a.Ratio);
                Assert.Equal(e.Date, a.Date);
                Assert.Equal(e.Active, a.Active);
                Assert.Equal(e.Byte, a.Byte);
                Assert.Equal(e.Guid, a.Guid);
                Assert.Equal(e.NullableInt, a.NullableInt);
                Assert.Equal(e.Kind, a.Kind);
                Assert.Equal(e.Text ?? string.Empty, a.Text ?? string.Empty);
                Assert.Equal(e.Encoded ?? string.Empty, a.Encoded ?? string.Empty);
            }
        }

        private static string SaveToString(Action<Stream> save)
        {
            using (var stream = new MemoryStream())
            {
                save(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static async Task<string> SaveToStringAsync(Func<Stream, Task> save)
        {
            using (var stream = new MemoryStream())
            {
                await save(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
