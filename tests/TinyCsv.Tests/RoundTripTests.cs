namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Extensions;
    using Xunit;

    public class RoundTripModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public decimal Amount { get; set; }
        public double Ratio { get; set; }
        public DateTime Date { get; set; }
        public long Big { get; set; }
        public bool Flag { get; set; }
        public int? Maybe { get; set; }
        public Guid Guid { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Random models written and read back, for every delimiter, NewLine and culture;
    /// the same text read through all the read paths; the special chars at the buffer boundaries.
    /// </summary>
    public class RoundTripTests
    {
        private const string Alphabet = "ab ,;|\t\"'\\\r\nàè€中😀#";

        public static IEnumerable<object[]> Delimiters => new[] { ",", ";", "\t", "|", "||" }.Select(x => new object[] { x });

        public static IEnumerable<object[]> NewLines => new[] { "\r\n", "\n", "\r" }.Select(x => new object[] { x });

        public static IEnumerable<object[]> Cultures => new[] { "", "en-US", "it-IT", "de-DE", "fr-FR" }.Select(x => new object[] { x });

        private static TinyCsv<RoundTripModel> Create(string delimiter, string newLine, string culture, bool header, Action<CsvOptions<RoundTripModel>> configure = null)
        {
            return new TinyCsv<RoundTripModel>(options =>
            {
                options.Delimiter = delimiter;
                options.NewLine = newLine;
                options.Culture = new CultureInfo(culture);
                options.HasHeaderRecord = header;
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Text);
                options.Columns.AddColumn(m => m.Amount);
                options.Columns.AddColumn(m => m.Ratio);
                options.Columns.AddColumn(m => m.Date);
                options.Columns.AddColumn(m => m.Big);
                options.Columns.AddColumn(m => m.Flag);
                options.Columns.AddColumn(m => m.Maybe);
                options.Columns.AddColumn(m => m.Guid);
                options.Columns.AddColumn(m => m.Notes);
                configure?.Invoke(options);
            });
        }

        private static RoundTripModel[] RandomModels(int seed, int count, string delimiter)
        {
            // every char also with a delimiter of more chars: a value that ends with the first chars
            // of the delimiter is quoted by the writer (i.e. "a|" with "||")
            _ = delimiter;
            var alphabet = Alphabet;
            var random = new Random(seed);
            string RandomText()
            {
                switch (random.Next(10))
                {
                    case 0: return null;
                    case 1: return string.Empty;
                    default:
                        var builder = new StringBuilder();
                        var length = random.Next(1, 16);
                        for (int i = 0; i < length; i++)
                        {
                            var c = alphabet[random.Next(alphabet.Length)];
                            if (char.IsLowSurrogate(c))
                            {
                                continue;
                            }
                            builder.Append(c);
                            if (char.IsHighSurrogate(c))
                            {
                                builder.Append(alphabet[alphabet.IndexOf(c) + 1]);
                            }
                        }
                        return builder.ToString();
                }
            }

            return Enumerable.Range(0, count).Select(i => new RoundTripModel
            {
                Id = i,
                Text = RandomText(),
                Amount = random.Next(-1_000_000, 1_000_000) / 100m,
                // a multiple of 1/4: the same text with "R" and with the default format on every framework
                Ratio = random.Next(-1000, 1000) / 4.0,
                // the default format of the dates has the seconds
                Date = new DateTime(2000, 1, 1).AddSeconds(random.Next(0, int.MaxValue)),
                Big = (long)random.Next() * random.Next(-1000, 1000),
                Flag = random.Next(2) == 1,
                Maybe = random.Next(3) == 0 ? (int?)null : random.Next(-100, 100),
                Guid = new Guid(Enumerable.Range(0, 16).Select(_ => (byte)random.Next(256)).ToArray()),
                Notes = RandomText(),
            }).ToArray();
        }

        private static void AssertModels(RoundTripModel[] expected, IReadOnlyList<RoundTripModel> actual)
        {
            Assert.Equal(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                var (e, a) = (expected[i], actual[i]);
                Assert.Equal(e.Id, a.Id);
                // a null string is written as an empty field
                Assert.Equal(e.Text ?? string.Empty, a.Text ?? string.Empty);
                Assert.Equal(e.Amount, a.Amount);
                Assert.Equal(e.Ratio, a.Ratio);
                Assert.Equal(e.Date, a.Date);
                Assert.Equal(e.Big, a.Big);
                Assert.Equal(e.Flag, a.Flag);
                Assert.Equal(e.Maybe, a.Maybe);
                Assert.Equal(e.Guid, a.Guid);
                Assert.Equal(e.Notes ?? string.Empty, a.Notes ?? string.Empty);
            }
        }

        private static string SaveToString(TinyCsv<RoundTripModel> csv, IEnumerable<RoundTripModel> models)
        {
            var stream = new MemoryStream();
            csv.Save(stream, models);
            return new UTF8Encoding(false).GetString(stream.ToArray());
        }

        [Theory]
        [MemberData(nameof(Delimiters))]
        public void RoundTrip_EveryDelimiterAndNewLine(string delimiter)
        {
            foreach (var newLine in new[] { "\r\n", "\n", "\r" })
            {
                foreach (var header in new[] { false, true })
                {
                    var models = RandomModels(delimiter.Length * 31 + newLine.Length, 300, delimiter);
                    var csv = Create(delimiter, newLine, "", header);

                    var text = SaveToString(csv, models);

                    AssertModels(models, csv.LoadFromText(text).ToList());
                    AssertModels(models, csv.LoadFromStream(new ChunkedStream(text, 7)).ToList());
                    Assert.Equal(text.Substring(0, text.Length - newLine.Length), csv.GetAllText(models));
                }
            }
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        public void RoundTrip_EveryCulture(string culture)
        {
            foreach (var delimiter in new[] { ",", ";", "\t" })
            {
                var models = RandomModels(culture.Length * 7 + delimiter[0], 300, delimiter);
                var csv = Create(delimiter, "\r\n", culture, true);

                var text = SaveToString(csv, models);

                AssertModels(models, csv.LoadFromText(text).ToList());
            }
        }

        [Theory]
        [MemberData(nameof(NewLines))]
        public async Task RoundTrip_Async(string newLine)
        {
            var models = RandomModels(newLine.Length, 300, ";");
            var csv = Create(";", newLine, "it-IT", true);

            var stream = new MemoryStream();
            await csv.SaveAsync(stream, models);
            var text = new UTF8Encoding(false).GetString(stream.ToArray());
            var read = new List<RoundTripModel>();
            await foreach (var model in csv.LoadFromStreamAsync(new ChunkedStream(text, 3)))
            {
                read.Add(model);
            }

            Assert.Equal(SaveToString(csv, models), text);
            AssertModels(models, read);
        }

        /// <summary>
        /// The same text, read through all the paths: sync and async, from a string and from a stream,
        /// the fast path and the path with SkipRow and the handlers, the fields (GetAllLinesAndFields) and the records
        /// </summary>
        [Theory]
        [MemberData(nameof(Delimiters))]
        public async Task AllTheReadPaths_ReturnTheSameValues(string delimiter)
        {
            var models = RandomModels(delimiter.Length + 100, 300, delimiter);
            var text = SaveToString(Create(delimiter, "\r\n", "", true), models);

            var csv = Create(delimiter, "\r\n", "", true);
            var skipRow = Create(delimiter, "\r\n", "", true, o => o.SkipRow = (row, index) => false);
            var handlers = Create(delimiter, "\r\n", "", true, o => o.EnableHandlers = true);
            var readRows = new List<string>();
            handlers.Options.Handlers.Read.RowRead += (sender, e) => readRows.Add(e.Row);

            AssertModels(models, csv.LoadFromText(text).ToList());
            AssertModels(models, csv.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(text))).ToList());
            AssertModels(models, csv.LoadFromStream(new ChunkedStream(text, 1)).ToList());
            AssertModels(models, await ToListAsync(csv.LoadFromTextAsync(text)));
            AssertModels(models, await ToListAsync(csv.LoadFromStreamAsync(new ChunkedStream(text, 2))));
            AssertModels(models, skipRow.LoadFromText(text).ToList());
            AssertModels(models, await ToListAsync(skipRow.LoadFromTextAsync(text)));
            AssertModels(models, handlers.LoadFromText(text).ToList());
            Assert.Equal(models.Length, readRows.Count);

            var fields = csv.GetAllLinesAndFieldsFromText(text).ToList();
            AssertModels(models, fields.Skip(1).Select(f => f.GetModelFromStringArray(csv.Options)).ToList());

            // the records without a model return the same fields, by the names of the header
            // (the records have no columns, so a delimiter at the end of the line ends it: a missing field is empty)
            var records = new TinyCsvRecords(o =>
            {
                o.Delimiter = delimiter;
                o.HasHeaderRecord = true;
            }).LoadFromText(text).ToList();
            Assert.Equal(models.Length, records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                Assert.Equal(fields[0], records[i].Keys);
                var values = fields[0].Select((name, index) => index < fields[i + 1].Length ? fields[i + 1][index] : string.Empty);
                Assert.Equal(values, records[i].Values.Select(v => v ?? string.Empty));
            }
        }

        /// <summary>
        /// The special chars (delimiter, quotes, doubled quotes, \r\n) at every position around the end of the buffers
        /// </summary>
        [Fact]
        public void BufferBoundaries_EveryPosition()
        {
            var csv = Csv.Person(o => o.Delimiter = ",");
            const string Tail = "2,\"q\"\"uo,te\r\nx\",\"y\"\r\n3,\"\",\"\"\"\"\r\n4,b,c";
            for (int padding = 4070; padding <= 4110; padding++)
            {
                var name = new string('a', padding);
                var text = $"1,{name},z\r\n{Tail}";

                foreach (var people in new[]
                {
                    csv.LoadFromText(text).ToArray(),
                    csv.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(text))).ToArray(),
                })
                {
                    Assert.Equal(new[] { 1, 2, 3, 4 }, people.Select(p => p.Id));
                    Assert.Equal(name, people[0].Name);
                    Assert.Equal("q\"uo,te\r\nx", people[1].Name);
                    Assert.Equal("y", people[1].City);
                    Assert.Equal("", people[2].Name);
                    Assert.Equal("\"", people[2].City);
                    Assert.Equal("c", people[3].City);
                }
            }
        }

        /// <summary>
        /// A field split across many reads, a quote and a \r\n split between two reads, a multi-byte char split between two reads
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(4096)]
        public async Task BufferBoundaries_SmallReads(int chunkSize)
        {
            var csv = Csv.Person(o => o.Delimiter = ",");
            var text = "1,\"€ a,\"\"b\"\"\r\nc\",😀\r\n2,\"\"\"\",\"\"\r\n3,\"" + new string('x', 9000) + "\",end\r\n4,\r\n";

            var people = csv.LoadFromStream(new ChunkedStream(text, chunkSize)).ToArray();
            var peopleAsync = await ToListAsync(csv.LoadFromStreamAsync(new ChunkedStream(text, chunkSize)));

            foreach (var read in new[] { people, peopleAsync.ToArray() })
            {
                Assert.Equal(new[] { 1, 2, 3, 4 }, read.Select(p => p.Id));
                Assert.Equal("€ a,\"b\"\r\nc", read[0].Name);
                Assert.Equal("😀", read[0].City);
                Assert.Equal("\"", read[1].Name);
                Assert.Equal("", read[1].City);
                Assert.Equal(9000, read[2].Name.Length);
                Assert.Equal("end", read[2].City);
                Assert.Equal("", read[3].Name ?? "");
            }
        }

        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
        {
            var list = new List<T>();
            await foreach (var item in items)
            {
                list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// A UTF-8 stream returning at most chunkSize bytes for each Read
        /// </summary>
        private sealed class ChunkedStream : MemoryStream
        {
            private readonly int chunkSize;

            public ChunkedStream(string text, int chunkSize)
                : base(new UTF8Encoding(false).GetBytes(text))
            {
                this.chunkSize = chunkSize;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, chunkSize));
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                return base.ReadAsync(buffer, offset, Math.Min(count, chunkSize), cancellationToken);
            }

#if NETCOREAPP
            public override int Read(Span<byte> buffer)
            {
                return base.Read(buffer.Slice(0, Math.Min(buffer.Length, chunkSize)));
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
            {
                return base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, chunkSize)), cancellationToken);
            }
#endif
        }
    }
}
