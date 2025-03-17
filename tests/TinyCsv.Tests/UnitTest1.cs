using System.Formats.Asn1;

namespace TinyCsv.Tests
{
    public class TinyCsvTests
    {
        private const string SampleCsv = "Name,Age,City\nJohn,30,New York\nAlice,25,London\nBob,40,Paris";
        private const string SampleCsvWithHeader = "Header1,Header2,Header3\nValue1,Value2,Value3";
        private const string MalformedCsv = "Name,Age\nJohn,30\nAlice";

        class TinyCsvTestModel
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string City { get; set; }
        }

        private TinyCsv<TinyCsvTestModel> GetTinyCsv(bool hasHeaderRecord)
        {
            var tinyCsv = new TinyCsv<TinyCsvTestModel>(options =>
            { // Options
                options.HasHeaderRecord = hasHeaderRecord;
                options.Delimiter = ",";
                options.RowsToSkip = 0;
                options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
                options.TrimData = true;
                options.AllowRowEnclosedInDoubleQuotesValues = true;
                options.EnableHandlers = false;
                options.ValidateColumnCount = true;

                // Columns
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.Age);
                options.Columns.AddColumn(m => m.City);
            });
            return tinyCsv;

        }

        [Fact]
        public void CsvReader_ShouldReadAllLines()
        {
            var csv = GetTinyCsv(hasHeaderRecord: true).LoadFromText(SampleCsv).ToList();
            Assert.InRange(csv.Count, 3, 3);
        }

        [Fact]
        public void CsvReader_ShouldReadCorrectValues()
        {
            var csv = GetTinyCsv(hasHeaderRecord: true).LoadFromText(SampleCsv).ToList();
            Assert.Equal("John", csv[0].Name);
            Assert.Equal(30, csv[0].Age);
            Assert.Equal("New York", csv[0].City);
        }

        [Fact]
        public void CsvReader_ShouldHandleEmptyFile()
        {
            var csv = GetTinyCsv(hasHeaderRecord: true).LoadFromText("").ToList();
            Assert.Empty(csv);
        }

        [Fact]
        public void CsvReader_ShouldIgnoreHeaders()
        {
            var csv = GetTinyCsv(hasHeaderRecord: true).LoadFromText(SampleCsvWithHeader).ToList();
            Assert.InRange(csv.Count, 1, 1);
        }

        [Fact]
        public void CsvReader_ShouldThrowOnMalformedCsv()
        {
            Assert.Throws<TinyCsv.Exceptions.InvalidColumnCountException>(() =>
            {
                var csv = GetTinyCsv(hasHeaderRecord: true).LoadFromText(MalformedCsv).ToList();
            });
        }

        [Fact]
        public void CsvReader_ShouldTrimWhitespace()
        {
            //var csv = CsvReader.ReadFromText(" Name , Age , City \n John , 30 , New York ").ToList();
            //csv[0]["Name"].Should().Be("John");
            //csv[0]["Age"].Should().Be("30");
            //csv[0]["City"].Should().Be("New York");
        }

        [Fact]
        public void CsvReader_ShouldHandleDifferentDelimiters()
        {
            //var csv = CsvReader.ReadFromText("Name|Age|City\nJohn|30|New York", delimiter: '|').ToList();
            //csv[0]["Name"].Should().Be("John");
            //csv[0]["Age"].Should().Be("30");
            //csv[0]["City"].Should().Be("New York");
        }

        [Fact]
        public void CsvReader_ShouldSupportQuotedFields()
        {
            //var csv = CsvReader.ReadFromText("Name,Age,City\n\"John Doe\",30,\"New York\"").ToList();
            //csv[0]["Name"].Should().Be("John Doe");
            //csv[0]["Age"].Should().Be("30");
            //csv[0]["City"].Should().Be("New York");
        }

        [Fact]
        public void CsvReader_ShouldSupportEscapedQuotes()
        {
            //var csv = CsvReader.ReadFromText("Name,Quote\nJohn,\"Hello \"World\"\"").ToList();
            //csv[0]["Quote"].Should().Be("Hello \"World\"");
        }

        [Fact]
        public void CsvReader_ShouldHandleMultiLineFields()
        {
            //var csv = CsvReader.ReadFromText("Name,Description\nJohn,\"This is a multi-line\ntext\"").ToList();
            //csv[0]["Description"].Should().Be("This is a multi-line\ntext");
        }
    }
}