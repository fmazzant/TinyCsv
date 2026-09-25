namespace TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using TinyCsv.Exceptions;
    using Xunit;

    public class ReaderTests
    {
        [Fact]
        public void LoadFromText_SimpleRows()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;Mario;Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Mario", result[0].Name);
            Assert.Equal("Roma", result[0].City);
            Assert.Equal(2, result[1].Id);
            Assert.Equal("Luigi", result[1].Name);
            Assert.Equal("Milano", result[1].City);
        }

        [Fact]
        public void LoadFromText_CrLfLineEndings()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;Mario;Roma\r\n2;Luigi;Milano\r\n").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Roma", result[0].City);
            Assert.Equal("Milano", result[1].City);
        }

        [Fact]
        public void LoadFromText_SkipsHeader()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var result = csv.LoadFromText("Id;Name;City\n1;Mario;Roma").ToList();

            Assert.Single(result);
            Assert.Equal("Mario", result[0].Name);
        }

        [Fact]
        public void LoadFromText_CustomDelimiter()
        {
            var csv = Csv.Person(o => o.Delimiter = ",");

            var result = csv.LoadFromText("1,Mario,Roma").ToList();

            Assert.Equal("Roma", result.Single().City);
        }

        [Fact]
        public void LoadFromText_QuotedFieldWithDelimiter()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"Rossi; Mario\";Roma").Single();

            Assert.Equal("Rossi; Mario", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void LoadFromText_EmptyFields()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;;Roma").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void LoadFromText_TrailingDelimiterIsIgnored()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;Mario;Roma;").Single();

            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void LoadFromText_TrimData()
        {
            var csv = Csv.Person(o => o.TrimData = true);

            var result = csv.LoadFromText("1;  Mario  ; Roma ").Single();

            Assert.Equal("Mario", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void LoadFromText_NoTrimData_KeepsSpaces()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;  Mario  ;Roma").Single();

            Assert.Equal("  Mario  ", result.Name);
        }

        [Fact]
        public void LoadFromText_RowsToSkip()
        {
            var csv = Csv.Person(o => o.RowsToSkip = 2);

            var result = csv.LoadFromText("garbage\nmore garbage\n1;Mario;Roma").ToList();

            Assert.Single(result);
            Assert.Equal("Mario", result[0].Name);
        }

        [Fact]
        public void LoadFromText_SkipRowCallback()
        {
            var csv = Csv.Person(o => o.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#"));

            var result = csv.LoadFromText("# comment\n1;Mario;Roma\n\n2;Luigi;Milano").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Mario", result[0].Name);
            Assert.Equal("Luigi", result[1].Name);
        }

        [Fact]
        public void LoadFromText_CommentNotAllowed_Throws()
        {
            var csv = Csv.Person(o => o.AllowComment = false);

            Assert.Throws<NotAllowCommentException>(() => csv.LoadFromText("# comment\n1;Mario;Roma").ToList());
        }

        [Fact]
        public void LoadFromText_ValidateColumnCount_Throws()
        {
            var csv = Csv.Person(o => o.ValidateColumnCount = true);

            Assert.Throws<InvalidColumnCountException>(() => csv.LoadFromText("1;Mario").ToList());
        }

        [Fact]
        public void LoadFromText_FewerColumnsWithoutValidation_LeavesDefaults()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;Mario").Single();

            Assert.Equal("Mario", result.Name);
            Assert.Null(result.City);
        }

        [Fact]
        public void LoadFromText_BackSlashEscapesQuote()
        {
            var csv = Csv.Person(o => o.AllowBackSlashToEscapeQuote = true);

            var result = csv.LoadFromText("1;\"Mario \\\"Super\\\" Rossi\";Roma").Single();

            Assert.Equal("Mario \"Super\" Rossi", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void LoadFromText_AttributeModel()
        {
            var csv = new TinyCsv<AttributePerson>();

            var result = csv.LoadFromText("Id,Name,BirthDate\n1, Mario ,1980-05-12").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal("Mario", result.Name);
            Assert.Equal(new DateTime(1980, 5, 12), result.BirthDate);
        }

        [Fact]
        public void LoadFromStream_Utf8()
        {
            var csv = Csv.Person();
            var bytes = Encoding.UTF8.GetBytes("1;Niccolò;Forlì");

            var result = csv.LoadFromStream(new MemoryStream(bytes)).Single();

            Assert.Equal("Niccolò", result.Name);
            Assert.Equal("Forlì", result.City);
        }

        [Fact]
        public void LoadFromFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "1;Mario;Roma\n2;Luigi;Milano");
            var csv = Csv.Person();

            var result = csv.LoadFromFile(path).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void LoadFromFile_ReleasesFileHandle()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "1;Mario;Roma");
            var csv = Csv.Person();

            csv.LoadFromFile(path).ToList();

            File.Delete(path);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void GetAllLinesFromText_ReturnsRawRows()
        {
            var csv = Csv.Person();

            var lines = csv.GetAllLinesFromText("1;Mario;Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(new[] { "1;Mario;Roma", "2;Luigi;Milano" }, lines);
        }

        [Fact]
        public void Handlers_RaisedWithRowIndexes()
        {
            var reading = new List<(int Index, string Row)>();
            var read = new List<int>();
            string header = null;
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.EnableHandlers = true;
                o.Handlers.Read.RowHeader += (s, e) => header = e.RowHeader;
                o.Handlers.Read.RowReading += (s, e) => reading.Add((e.Index, e.Row));
                o.Handlers.Read.RowRead += (s, e) => read.Add(e.Index);
            });

            csv.LoadFromText("Id;Name;City\n1;Mario;Roma\n2;Luigi;Milano").ToList();

            Assert.Equal("Id;Name;City", header);
            Assert.Equal(new[] { 0, 1, 2 }, reading.Select(x => x.Index));
            Assert.Equal("1;Mario;Roma", reading[1].Row);
            Assert.Equal(new[] { 1, 2 }, read);
        }

        // ---------------------------------------------------------------
        // Known bugs of the current line-based reader.
        // They document the expected behavior (RFC 4180, a "row" is a
        // logical record that may span multiple physical lines) and will
        // be enabled by the new span-based record reader.
        // ---------------------------------------------------------------

        [Fact]
        public void Multiline_QuotedFieldSpanningTwoLines()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"Mario\nRossi\";Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Mario\nRossi", result[0].Name);
            Assert.Equal("Roma", result[0].City);
            Assert.Equal("Luigi", result[1].Name);
        }

        [Fact]
        public void Multiline_PreservesCrLfInsideField()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"Mario\r\nRossi\";Roma\r\n2;Luigi;Milano\r\n").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Mario\r\nRossi", result[0].Name);
        }

        [Fact]
        public void Multiline_QuotedFieldSpanningManyLinesWithEmptyLine()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"a\n\nb\nc\";Roma").Single();

            Assert.Equal("a\n\nb\nc", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Multiline_IsOneRowForHeaderAndHandlers()
        {
            var reading = new List<(int Index, string Row)>();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.EnableHandlers = true;
                o.Handlers.Read.RowReading += (s, e) => reading.Add((e.Index, e.Row));
            });

            var result = csv.LoadFromText("Id;Name;City\n1;\"Mario\nRossi\";Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { 0, 1, 2 }, reading.Select(x => x.Index));
            Assert.Equal("1;\"Mario\nRossi\";Roma", reading[1].Row);
        }

        [Fact]
        public void Multiline_RowsToSkipCountsRecords()
        {
            var csv = Csv.Person(o => o.RowsToSkip = 1);

            var result = csv.LoadFromText("0;\"skip\nme\";X\n1;Mario;Roma").ToList();

            Assert.Single(result);
            Assert.Equal("Mario", result[0].Name);
        }

        [Fact]
        public void Multiline_GetAllLinesReturnsRecords()
        {
            var csv = Csv.Person();

            var lines = csv.GetAllLinesFromText("1;\"Mario\nRossi\";Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(new[] { "1;\"Mario\nRossi\";Roma", "2;Luigi;Milano" }, lines);
        }

        [Fact]
        public void Quotes_DoubledQuoteIsEscape()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"Mario \"\"Super\"\" Rossi\";Roma").Single();

            Assert.Equal("Mario \"Super\" Rossi", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Quotes_DoubledQuoteFollowedByDelimiter()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"a\"\";b\";Roma").Single();

            Assert.Equal("a\";b", result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Quotes_EmptyQuotedField()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;\"\";Roma").Single();

            Assert.Equal(string.Empty, result.Name);
            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void Quotes_SingleQuotesInUnquotedValueArePreserved()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;'Mario';Roma").Single();

            Assert.Equal("'Mario'", result.Name);
        }

        [Fact]
        public void BackSlash_AtEndOfLineDoesNotThrow()
        {
            var csv = Csv.Person(o => o.AllowBackSlashToEscapeQuote = true);

            var result = csv.LoadFromText("1;Mario;C:\\").Single();

            Assert.Equal("C:\\", result.City);
        }

        [Fact]
        public void EmptyLine_WithoutSkipRow_DoesNotThrow()
        {
            var csv = Csv.Person();

            var result = csv.LoadFromText("1;Mario;Roma\n\n2;Luigi;Milano").ToList();

            Assert.Equal(new[] { "Mario", "Luigi" }, result.Where(x => x.Name != null).Select(x => x.Name));
        }

        [Fact]
        public void SkipRow_ReceivesRecordIndexes()
        {
            var indexes = new List<int>();
            var csv = Csv.Person(o => o.SkipRow = (row, idx) => { indexes.Add(idx); return false; });

            csv.LoadFromText("1;a;b\n2;c;d\n3;e;f").ToList();

            Assert.Equal(new[] { 0, 1, 2 }, indexes);
        }

        [Fact]
        public void SkipRow_WithRowsToSkip_ReceivesRecordIndexes()
        {
            var indexes = new List<int>();
            var csv = Csv.Person(o =>
            {
                o.RowsToSkip = 1;
                o.SkipRow = (row, idx) => { indexes.Add(idx); return false; };
            });

            csv.LoadFromText("skip\n1;a;b\n2;c;d").ToList();

            Assert.Equal(new[] { 1, 2 }, indexes);
        }

        [Fact]
        public void GetAllLinesAndFields_ReturnsFields()
        {
            var csv = Csv.Person();
            var bytes = Encoding.UTF8.GetBytes("1;Mario;Roma\n2;\"Luigi; Jr\";Milano");

            var rows = csv.GetAllLinesAndFieldsFromStream(new MemoryStream(bytes)).ToList();

            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "1", "Mario", "Roma" }, rows[0]);
            Assert.Equal(new[] { "2", "Luigi; Jr", "Milano" }, rows[1]);
        }
    }
}
