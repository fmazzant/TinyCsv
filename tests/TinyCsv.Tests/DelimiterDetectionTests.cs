namespace TinyCsv.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using TinyCsv.Attributes;
    using Xunit;

    public class DelimiterDetectionTests
    {
        [Theory]
        [InlineData("Id,Name,City\n1,Mario,Roma\n2,Luigi,Milano")]
        [InlineData("Id;Name;City\n1;Mario;Roma\n2;Luigi;Milano")]
        [InlineData("Id\tName\tCity\n1\tMario\tRoma\n2\tLuigi\tMilano")]
        [InlineData("Id|Name|City\n1|Mario|Roma\n2|Luigi|Milano")]
        public void LoadFromText_DetectsTheDelimiter(string text)
        {
            var csv = Csv.Person(o => { o.HasHeaderRecord = true; o.Delimiter = "#"; o.DetectDelimiter = true; });

            var people = csv.LoadFromText(text).ToArray();

            Assert.Equal(new[] { "Mario", "Luigi" }, people.Select(p => p.Name));
            Assert.Equal(new[] { "Roma", "Milano" }, people.Select(p => p.City));
        }

        [Fact]
        public void LoadFromText_DelimiterInQuotedValuesAndDecimalCommas_IsNotCounted()
        {
            var csv = Csv.Person(o => { o.HasHeaderRecord = true; o.DetectDelimiter = true; });

            var people = csv.LoadFromText("Id;Name;City\n1;\"Rossi, Mario\";\"Roma, RM\"\n2;Luigi;\"1,5\"").ToArray();

            Assert.Equal("Rossi, Mario", people[0].Name);
            Assert.Equal("1,5", people[1].City);
        }

        [Fact]
        public void LoadFromText_MultiLineQuotedValue_IsOneRecord()
        {
            var csv = Csv.Person(o => { o.HasHeaderRecord = true; o.DetectDelimiter = true; });

            var people = csv.LoadFromText("Id,Name,City\n1,\"Mario\nRossi\",Roma\n2,Luigi,Milano").ToArray();

            Assert.Equal(2, people.Length);
            Assert.Equal("Mario\nRossi", people[0].Name);
        }

        [Fact]
        public void LoadFromText_NoCandidate_UsesTheDelimiter()
        {
            var csv = Csv.Person(o => { o.Delimiter = "#"; o.DetectDelimiter = true; });

            var people = csv.LoadFromText("1#Mario#Roma").ToArray();

            Assert.Equal("Roma", people[0].City);
        }

        [Fact]
        public void LoadFromText_SkipsRowsToSkipAndComments()
        {
            var csv = Csv.Person(o => { o.RowsToSkip = 1; o.DetectDelimiter = true; });

            var people = csv.LoadFromText("exported; by; someone; else; here\n# a comment; ;\n1,Mario,Roma\n2,Luigi,Milano").ToArray();

            Assert.Equal(new[] { "Roma", "Milano" }, people.Select(p => p.City));
        }

        [Fact]
        public void LoadFromText_LargeFile_ReadsEveryRecordAfterTheSample()
        {
            var csv = Csv.Person(o => o.DetectDelimiter = true);
            var text = string.Join("\n", Enumerable.Range(1, 20000).Select(i => $"{i},Name {i},City {i}"));

            var people = csv.LoadFromText(text).ToArray();

            Assert.Equal(20000, people.Length);
            Assert.Equal("City 20000", people.Last().City);
        }

        [Fact]
        public void LoadFromStream_DetectsTheDelimiter()
        {
            var csv = Csv.Person(o => o.DetectDelimiter = true);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("1,Mario,Roma\n2,Luigi,Milano"));

            var people = csv.LoadFromStream(stream).ToArray();

            Assert.Equal("Milano", people[1].City);
        }

        [Fact]
        public void GetAllLinesAndFieldsFromText_DetectsTheDelimiter()
        {
            var csv = Csv.Person(o => o.DetectDelimiter = true);

            var fields = csv.GetAllLinesAndFieldsFromText("1|Mario|Roma\n2|Luigi|Milano").ToArray();

            Assert.Equal(new[] { "2", "Luigi", "Milano" }, fields[1]);
        }

        [Fact]
        public void DetectDelimiter_DoesNotChangeTheWriteDelimiter()
        {
            var csv = Csv.Person(o => { o.DetectDelimiter = true; o.NewLine = "\n"; });

            var people = csv.LoadFromText("1,Mario,Roma").ToArray();

            Assert.Equal("1;Mario;Roma", csv.GetAllText(people));
        }

        [DetectDelimiter(true)]
        [HasHeaderRecord(true)]
        public class DetectedPerson
        {
            [Column]
            public int Id { get; set; }

            [Column]
            public string Name { get; set; }
        }

        [Fact]
        public void DetectDelimiterAttribute_DetectsTheDelimiter()
        {
            var csv = new TinyCsv<DetectedPerson>();

            var people = csv.LoadFromText("Id\tName\n1\tMario").ToArray();

            Assert.Equal("Mario", people[0].Name);
        }

#if NET5_0_OR_GREATER
        [Fact]
        public async Task LoadFromTextAsync_DetectsTheDelimiter()
        {
            var csv = Csv.Person(o => o.DetectDelimiter = true);

            var people = new System.Collections.Generic.List<Person>();
            await foreach (var person in csv.LoadFromTextAsync("1,Mario,Roma\n2,Luigi,Milano"))
            {
                people.Add(person);
            }

            Assert.Equal("Milano", people[1].City);
        }
#endif
    }
}
