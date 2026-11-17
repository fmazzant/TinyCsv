namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public class RecordsWriteTests
    {
        private static TinyCsvRecords Records(Action<CsvRecordOptions> configure = null)
        {
            return new TinyCsvRecords(o =>
            {
                o.Delimiter = ",";
                o.NewLine = "\n";
                configure?.Invoke(o);
            });
        }

        private static DataTable Table()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Price", typeof(decimal));
            table.Columns.Add("Date", typeof(DateTime));
            table.Rows.Add(1, "Mario", 10.5m, new DateTime(2026, 1, 2));
            table.Rows.Add(2, "Rossi, Luigi", DBNull.Value, new DateTime(2026, 3, 4));
            return table;
        }

        [Theory]
        [InlineData("Id,Name,City\n1,Mario,Roma\n2,\"Rossi, Luigi\",Milano")]
        [InlineData("Id,Name,City\n1,,Roma\n2,\"Say \"\"hi\"\"\",\"Line1\nLine2\"")]
        [InlineData("Id,Name\n")]
        public void LoadDataTable_GetAllText_RoundTrip(string text)
        {
            var records = Records();

            var table = records.LoadDataTableFromText(text);

            Assert.Equal(text.TrimEnd('\n'), records.GetAllText(table));
        }

        [Fact]
        public void LoadFromText_GetAllText_RoundTrip_WithDictionariesAndDynamic()
        {
            const string text = "Id,Name,City\n1,Mario,Roma\n2,\"Rossi, Luigi\",Milano";
            var records = Records();

            Assert.Equal(text, records.GetAllText(records.LoadFromText(text)));
            Assert.Equal(text, records.GetAllText(records.LoadDynamicFromText(text)));
        }

        [Fact]
        public void GetAllText_DataTable_FormatsWithTheCulture()
        {
            var records = Records(o => { o.Delimiter = ";"; o.Culture = CultureInfo.GetCultureInfo("it-IT"); });

            var text = records.GetAllText(Table());

            Assert.Equal("Id;Name;Price;Date\n1;Mario;10,5;02/01/2026 00:00:00\n2;Rossi, Luigi;;04/03/2026 00:00:00", text);
        }

        [Fact]
        public void GetAllText_DataTable_SkipsTheDeletedRows()
        {
            var table = Table();
            table.AcceptChanges();
            table.Rows[0].Delete();

            var text = Records().GetAllText(table);

            Assert.Equal("Id,Name,Price,Date\n2,\"Rossi, Luigi\",,03/04/2026 00:00:00", text);
        }

        [Fact]
        public void GetAllText_DataReader()
        {
            var table = Table();
            using (var reader = table.CreateDataReader())
            {
                Assert.Equal(Records().GetAllText(table), Records().GetAllText(reader));
            }
        }

        [Fact]
        public void GetAllText_Dictionaries_HeaderFromTheFirstRecord()
        {
            var rows = new List<IDictionary<string, object>>
            {
                new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "Mario" },
                new Dictionary<string, object> { ["Name"] = "Luigi", ["Other"] = "x" },
            };

            var text = Records().GetAllText(rows);

            Assert.Equal("Id,Name\n1,Mario\n,Luigi", text);
        }

        [Fact]
        public void GetAllText_Dynamic_ExpandoAndAnonymous()
        {
            dynamic expando = new ExpandoObject();
            expando.Id = 1;
            expando.Name = "Mario";
            var rows = new List<dynamic> { expando, new { Id = 2, Name = "Luigi" } };

            Assert.Equal("Id,Name\n1,Mario\n2,Luigi", Records().GetAllText(rows));
        }

        [Fact]
        public void ColumnsToWrite_ChoosesTheColumnsAndTheirOrder()
        {
            var records = Records(o => o.ColumnsToWrite = new[] { "Name", "Id" });
            var rows = new[] { new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "Mario", ["City"] = "Roma" } };

            Assert.Equal("Name,Id\nMario,1", records.GetAllText(rows));
            Assert.Equal("Name,Id\nMario,1\n\"Rossi, Luigi\",2", records.GetAllText(Table()));
        }

        [Fact]
        public void ColumnsToWrite_MissingColumnOfADataTable_Throws()
        {
            var records = Records(o => o.ColumnsToWrite = new[] { "Missing" });

            Assert.Throws<ArgumentException>(() => records.GetAllText(Table()));
        }

        [Fact]
        public void GetAllText_QuoteModeAll_QuotesEveryFieldButNull()
        {
            var records = Records(o => o.QuoteMode = CsvQuoteMode.All);
            var table = Table();
            table.Columns.Remove("Date");

            Assert.Equal("\"Id\",\"Name\",\"Price\"\n\"1\",\"Mario\",\"10.5\"\n\"2\",\"Rossi, Luigi\",", records.GetAllText(table));
        }

        [Fact]
        public void GetAllText_QuoteModeNonNumeric_QuotesTheTextOnly()
        {
            var records = Records(o => o.QuoteMode = CsvQuoteMode.NonNumeric);
            var table = Table();
            table.Columns.Remove("Date");

            Assert.Equal("\"Id\",\"Name\",\"Price\"\n1,\"Mario\",10.5\n2,\"Rossi, Luigi\",", records.GetAllText(table));
        }

        [Fact]
        public void GetAllText_NullValueOnWrite()
        {
            var records = Records(o => o.NullValueOnWrite = "NULL");
            var table = Table();
            table.Columns.Remove("Date");

            Assert.Equal("Id,Name,Price\n1,Mario,10.5\n2,\"Rossi, Luigi\",NULL", records.GetAllText(table));
        }

        [Fact]
        public void GetAllText_InjectionProtection_ProtectsTheText()
        {
            var records = Records(o => o.InjectionProtection = CsvInjectionProtection.Escape);
            var rows = new[] { new Dictionary<string, object> { ["Formula"] = "=1+1", ["Number"] = -1 } };

            Assert.Equal("Formula,Number\n\"'=1+1\",-1", records.GetAllText(rows));
        }

        [Fact]
        public void GetAllText_InjectionProtectionThrow_SetsTheRowIndex()
        {
            var records = Records(o => o.InjectionProtection = CsvInjectionProtection.Throw);
            var rows = new[]
            {
                new Dictionary<string, object> { ["Formula"] = "ok" },
                new Dictionary<string, object> { ["Formula"] = "=1+1" },
            };

            var exception = Assert.Throws<CsvInjectionException>(() => records.GetAllText(rows));
            Assert.Equal(2, exception.RowIndex);
        }

        [Fact]
        public void GetAllText_WithoutHeader_SeparatorLineAndBom()
        {
            var records = Records(o => { o.HasHeaderRecord = false; o.WriteSeparatorLine = true; o.WriteByteOrderMark = true; });

            Assert.Equal("﻿sep=,\n1,Mario", records.GetAllText(new[] { new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "Mario" } }));
        }

        [Fact]
        public void GetAllText_ByteArray_IsBase64()
        {
            var rows = new[] { new Dictionary<string, object> { ["Data"] = new byte[] { 1, 2, 3 } } };

            Assert.Equal("Data\nAQID", Records().GetAllText(rows));
        }

        [Fact]
        public void GetAllText_NoRecords_IsEmpty()
        {
            Assert.Equal(string.Empty, Records().GetAllText(new List<IDictionary<string, object>>()));
        }

        [Fact]
        public void SaveToFile_DataTable_ThenLoad()
        {
            var records = Records();
            var path = Path.Combine(Path.GetTempPath(), $"tinycsv-{Guid.NewGuid():N}.csv");
            try
            {
                records.SaveToFile(path, Table());

                var bytes = File.ReadAllBytes(path);
                Assert.NotEqual(0xEF, bytes[0]);
                var loaded = records.LoadDataTableFromFile(path);
                Assert.Equal(2, loaded.Rows.Count);
                Assert.Equal("Rossi, Luigi", loaded.Rows[1]["Name"]);
                Assert.Equal(string.Empty, loaded.Rows[1]["Price"]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SaveToStream_LeavesTheStreamOpen()
        {
            var stream = new MemoryStream();

            Records().SaveToStream(stream, Table());

            Assert.True(stream.CanWrite);
            Assert.Equal(Records().GetAllText(Table()) + "\n", Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task SaveToStreamAsync_DataReader_IsStreamed()
        {
            var table = Table();
            var stream = new MemoryStream();
            using (var reader = table.CreateDataReader())
            {
                await Records().SaveToStreamAsync(stream, reader);
            }

            Assert.Equal(Records().GetAllText(table) + "\n", Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task SaveToFileAsync_Dynamic()
        {
            var path = Path.Combine(Path.GetTempPath(), $"tinycsv-{Guid.NewGuid():N}.csv");
            try
            {
                await Records().SaveToFileAsync(path, new List<dynamic> { new { Id = 1, Name = "Mario" } });

                Assert.Equal("Id,Name\n1,Mario\n", File.ReadAllText(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task SaveToStreamAsync_Cancelled_Throws()
        {
            var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => Records().SaveToStreamAsync(new MemoryStream(), Table(), source.Token));
        }
    }
}
