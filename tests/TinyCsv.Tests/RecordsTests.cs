namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public class RecordsTests
    {
        private const string Text = "Id,Name,City\n1,Mario,Roma\n2,\"Rossi, Luigi\",Milano";

        private static TinyCsvRecords Records(Action<CsvRecordOptions> configure = null)
        {
            return new TinyCsvRecords(o =>
            {
                o.Delimiter = ",";
                configure?.Invoke(o);
            });
        }

        [Fact]
        public void LoadFromText_ReadsDictionaries()
        {
            var records = Records().LoadFromText(Text).ToArray();

            Assert.Equal(2, records.Length);
            Assert.Equal("Rossi, Luigi", records[1]["Name"]);
            Assert.Equal("Milano", records[1]["city"]);
        }

        private static async System.Threading.Tasks.Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
        {
            var result = new List<T>();
            await foreach (var item in items)
            {
                result.Add(item);
            }
            return result;
        }

        [Fact]
        public async System.Threading.Tasks.Task LoadAsync_ReadsDictionaries()
        {
            var path = Path.Combine(Path.GetTempPath(), $"tinycsv-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, Text);
            try
            {
                var fromText = await ToListAsync(Records().LoadFromTextAsync(Text));
                var fromStream = await ToListAsync(Records().LoadFromStreamAsync(new MemoryStream(Encoding.UTF8.GetBytes(Text))));
                var fromFile = await ToListAsync(Records().LoadFromFileAsync(path));

                foreach (var records in new[] { fromText, fromStream, fromFile })
                {
                    Assert.Equal(2, records.Count);
                    Assert.Equal("Rossi, Luigi", records[1]["Name"]);
                    Assert.Equal("Milano", records[1]["city"]);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task LoadAsync_BadData_IsReported()
        {
            var errors = new List<CsvException>();
            var records = await ToListAsync(Records(o =>
            {
                o.ValidateColumnCount = true;
                o.BadDataFound = errors.Add;
            }).LoadFromTextAsync("Id,Name\n1,Mario\n2\n3,Luigi"));

            Assert.Equal(new[] { "1", "3" }, records.Select(r => r["Id"]));
            Assert.Single(errors);
        }

        [Fact]
        public void LoadFromText_WithoutHeader_NamesAreFields()
        {
            var records = Records(o => o.HasHeaderRecord = false).LoadFromText("1,Mario\n2,Luigi,Milano").ToArray();

            Assert.Equal("Mario", records[0]["Field2"]);
            Assert.Equal("Milano", records[1]["Field3"]);
        }

        [Fact]
        public void LoadFromText_ShortRecord_MissingValueIsNull()
        {
            var record = Records().LoadFromText("Id,Name,City\n1,Mario").Single();

            Assert.Null(record["City"]);
        }

        [Fact]
        public void LoadFromText_LongRecord_ExtraValueIsAField()
        {
            var record = Records().LoadFromText("Id,Name\n1,Mario,Roma").Single();

            Assert.Equal("Roma", record["Field3"]);
        }

        [Fact]
        public void LoadFromText_EmptyAndRepeatedHeaderNames_AreUnique()
        {
            var record = Records().LoadFromText("Name,,Name\na,b,c").Single();

            Assert.Equal(new[] { "Name", "Field2", "Name_2" }, record.Keys);
        }

        [Fact]
        public void LoadFromText_ValidateColumnCount_BadDataFound()
        {
            var errors = new List<CsvException>();
            var records = Records(o =>
            {
                o.ValidateColumnCount = true;
                o.BadDataFound = errors.Add;
            }).LoadFromText("Id,Name\n1,Mario,Roma\n2,Luigi").ToArray();

            Assert.Single(records);
            Assert.IsType<InvalidColumnCountException>(Assert.Single(errors));
        }

        [Fact]
        public void LoadFromText_DetectsTheDelimiter()
        {
            var records = Records(o => o.DetectDelimiter = true).LoadFromText("Id;Name\n1;Mario").ToArray();

            Assert.Equal("Mario", records[0]["Name"]);
        }

        [Fact]
        public void LoadFromStream_SkipsCommentsAndRows()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("preamble\nId,Name\n# comment\n1,Mario"));

            var record = Records(o => o.RowsToSkip = 1).LoadFromStream(stream).Single();

            Assert.Equal("Mario", record["Name"]);
        }

        [Fact]
        public void LoadDynamicFromText_ReadsDynamicObjects()
        {
            var records = Records().LoadDynamicFromText(Text).ToArray();

            Assert.Equal("Mario", (string)records[0].Name);
            Assert.Equal("Milano", (string)records[1].City);
        }

        [Fact]
        public void LoadDataTableFromText_ReadsTheRows()
        {
            var table = Records().LoadDataTableFromText(Text);

            Assert.Equal(new[] { "Id", "Name", "City" }, table.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName));
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("Rossi, Luigi", table.Rows[1]["Name"]);
        }

        [Fact]
        public void LoadDataTableFromText_MissingValue_IsDBNull()
        {
            var table = Records().LoadDataTableFromText("Id,Name\n1");

            Assert.Equal(DBNull.Value, table.Rows[0]["Name"]);
        }

        [Fact]
        public void LoadDataTableFromText_OnlyHeader_HasTheColumns()
        {
            var table = Records().LoadDataTableFromText("Id,Name");

            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);
        }

        [Fact]
        public void LoadFromFile_ReadsAllKinds()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, Text);
                var records = Records();

                Assert.Equal(2, records.LoadFromFile(path).Count());
                Assert.Equal(2, records.LoadDynamicFromFile(path).Count());
                Assert.Equal(2, records.LoadDataTableFromFile(path).Rows.Count);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void MultiLineQuotedRecord_IsOneRecord()
        {
            var records = Records().LoadFromText("Id,Note\n1,\"line 1\nline 2\"\n2,x").ToArray();

            Assert.Equal(2, records.Length);
            Assert.Equal("line 1\nline 2", records[0]["Note"]);
        }
    }
}
