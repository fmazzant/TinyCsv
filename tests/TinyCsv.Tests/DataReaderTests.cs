namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public class DataReaderTests
    {
        private const string Text = "Id,Name,Price,Date\n1,Mario,10.5,2026-01-02\n2,\"Rossi, Luigi\",,2026-03-04\n";

        private static TinyCsvRecords Records(Action<CsvRecordOptions> configure = null)
        {
            return new TinyCsvRecords(o =>
            {
                o.Delimiter = ",";
                configure?.Invoke(o);
            });
        }

        private static Stream Stream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

        private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>
        {
            ["Id"] = typeof(int),
            ["Price"] = typeof(decimal?),
            ["Date"] = typeof(DateTime),
        };

        [Fact]
        public void Untyped_ColumnsAreStrings_DataTableLoad()
        {
            using (var reader = Records().CreateDataReader(Stream(Text)))
            {
                Assert.Equal(4, reader.FieldCount);
                Assert.Equal("Price", reader.GetName(2));
                Assert.Equal(typeof(string), reader.GetFieldType(0));
                Assert.True(reader.HasRows);

                var table = new DataTable();
                table.Load(reader);

                Assert.Equal(2, table.Rows.Count);
                Assert.Equal("Rossi, Luigi", table.Rows[1]["Name"]);
                Assert.Equal(string.Empty, table.Rows[1]["Price"]);
                Assert.Equal(typeof(string), table.Columns["Id"].DataType);
            }
        }

        [Fact]
        public void Untyped_ReadAndGetters()
        {
            using (var reader = Records().CreateDataReader(Stream(Text)))
            {
                Assert.True(reader.Read());
                Assert.Equal("1", reader.GetString(0));
                Assert.Equal("Mario", reader["name"]);
                Assert.Equal(1, reader.GetOrdinal("NAME"));
                var values = new object[4];
                Assert.Equal(4, reader.GetValues(values));
                Assert.Equal("2026-01-02", values[3]);

                Assert.True(reader.Read());
                Assert.False(reader.Read());
            }
        }

        [Fact]
        public void Untyped_WithSchema_ConvertsTheValues()
        {
            using (var reader = Records(o => o.ColumnTypes = Types).CreateDataReader(Stream(Text)))
            {
                Assert.Equal(typeof(int), reader.GetFieldType(0));
                Assert.Equal(typeof(string), reader.GetFieldType(1));
                Assert.Equal(typeof(decimal), reader.GetFieldType(2));

                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetInt32(0));
                Assert.Equal(10.5m, reader.GetDecimal(2));
                Assert.Equal(new DateTime(2026, 1, 2), reader.GetDateTime(3));

                Assert.True(reader.Read());
                Assert.True(reader.IsDBNull(2));
                Assert.Equal(DBNull.Value, reader.GetValue(2));
            }
        }

        [Fact]
        public void Untyped_SchemaTable()
        {
            using (var reader = Records(o => o.ColumnTypes = Types).CreateDataReader(Stream(Text)))
            {
                var schema = reader.GetSchemaTable();

                Assert.Equal(4, schema.Rows.Count);
                Assert.Equal("Id", schema.Rows[0]["ColumnName"]);
                Assert.Equal(0, schema.Rows[0]["ColumnOrdinal"]);
                Assert.Equal(typeof(int), schema.Rows[0]["DataType"]);
                Assert.Equal(true, schema.Rows[2]["AllowDBNull"]);
            }
        }

        [Fact]
        public void Untyped_InvalidValue_ThrowsWithTheRow()
        {
            var text = "Id,Name\n1,Mario\nx,Luigi\n";
            using (var reader = Records(o => o.ColumnTypes = Types).CreateDataReader(Stream(text)))
            {
                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetInt32(0));
                Assert.True(reader.Read());

                var exception = Assert.Throws<InvalidColumnValueException>(() => reader.GetValue(0));
                Assert.Equal(2, exception.RowIndex);
                Assert.Equal("Id", exception.ColumnName);
            }
        }

        [Fact]
        public void Untyped_WithoutHeader_FieldNames()
        {
            using (var reader = Records(o => o.HasHeaderRecord = false).CreateDataReader(Stream("1,Mario\n2,Luigi\n")))
            {
                Assert.Equal(new[] { "Field1", "Field2" }, Enumerable.Range(0, reader.FieldCount).Select(reader.GetName));
                Assert.True(reader.Read());
                Assert.Equal("1", reader.GetValue(0));
            }
        }

        [Fact]
        public void Untyped_HeaderOnly_HasNoRows()
        {
            using (var reader = Records().CreateDataReader(Stream("Id,Name\n")))
            {
                Assert.Equal(2, reader.FieldCount);
                Assert.False(reader.HasRows);
                Assert.False(reader.Read());
            }
        }

        [Fact]
        public void Untyped_ShortRecord_IsDBNull()
        {
            using (var reader = Records().CreateDataReader(Stream("Id,Name\n1\n")))
            {
                Assert.True(reader.Read());
                Assert.True(reader.IsDBNull(1));
            }
        }

        [Fact]
        public void Close_DisposesTheStream()
        {
            var stream = Stream(Text);
            var reader = Records().CreateDataReader(stream);

            reader.Dispose();

            Assert.True(reader.IsClosed);
            Assert.False(stream.CanRead);
            Assert.Throws<InvalidOperationException>(() => reader.Read());
        }

        [Fact]
        public void GetValue_BeforeRead_Throws()
        {
            using (var reader = Records().CreateDataReader(Stream(Text)))
            {
                Assert.Throws<InvalidOperationException>(() => reader.GetValue(0));
            }
        }

        [Fact]
        public void LoadDataTable_WithSchema_TypedColumns()
        {
            var table = Records(o => o.ColumnTypes = Types).LoadDataTableFromText(Text);

            Assert.Equal(typeof(int), table.Columns["Id"].DataType);
            Assert.Equal(typeof(string), table.Columns["Name"].DataType);
            Assert.Equal(typeof(decimal), table.Columns["Price"].DataType);
            Assert.Equal(2, table.Rows[1]["Id"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["Price"]);
            Assert.Equal(new DateTime(2026, 3, 4), table.Rows[1]["Date"]);
        }

        [Fact]
        public void LoadDataTable_WithSchema_UsesTheCulture()
        {
            var records = Records(o =>
            {
                o.Delimiter = ";";
                o.Culture = CultureInfo.GetCultureInfo("it-IT");
                o.ColumnTypes = new Dictionary<string, Type> { ["Price"] = typeof(decimal) };
            });

            var table = records.LoadDataTableFromText("Price\n10,5");

            Assert.Equal(10.5m, table.Rows[0]["Price"]);
        }

        [Fact]
        public void Typed_ColumnsAndValuesOfTheModel()
        {
            var csv = Csv.Person();
            using (var reader = csv.CreateDataReader(Stream("1;Mario;Roma\n2;Luigi;\n")))
            {
                Assert.Equal(new[] { "Id", "Name", "City" }, Enumerable.Range(0, reader.FieldCount).Select(reader.GetName));
                Assert.Equal(typeof(int), reader.GetFieldType(0));

                var table = new DataTable();
                table.Load(reader);

                Assert.Equal(2, table.Rows.Count);
                Assert.Equal(typeof(int), table.Columns["Id"].DataType);
                Assert.Equal(2, table.Rows[1]["Id"]);
                Assert.Equal("Roma", table.Rows[0]["City"]);
            }
        }

        [Fact]
        public void Typed_FromTheInterface()
        {
            ITinyCsv<Person> csv = Csv.Person();
            using (var reader = csv.CreateDataReader(Stream("1;Mario;Roma\n")))
            {
                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetInt32(0));
            }
        }

        public class Address
        {
            public string City { get; set; }
        }

        public class Customer
        {
            public int? Id { get; set; }
            public Address Address { get; set; }
        }

        [Fact]
        public void Typed_NullableAndNestedColumns()
        {
            var csv = new TinyCsv<Customer>(o =>
            {
                o.Delimiter = ",";
                o.HasHeaderRecord = false;
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Address.City);
            });

            using (var reader = csv.CreateDataReader(Stream("1,Roma\n,\n")))
            {
                Assert.Equal(typeof(int), reader.GetFieldType(0));
                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetValue(0));
                Assert.Equal("Roma", reader.GetValue(1));
                Assert.True(reader.Read());
                Assert.True(reader.IsDBNull(0));
            }
        }

        [Fact]
        public void Untyped_File_ManyRows_IsStreamed()
        {
            var path = Path.Combine(Path.GetTempPath(), $"tinycsv-{Guid.NewGuid():N}.csv");
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Id,Name");
                    for (int i = 0; i < 200000; i++)
                    {
                        writer.Write(i);
                        writer.WriteLine(",Name");
                    }
                }

                var count = 0;
                long before;
                using (var reader = Records(o => o.ColumnTypes = Types).CreateDataReader(path))
                {
                    // read some rows, then measure: the memory does not grow with the rows
                    for (int i = 0; i < 1000; i++)
                    {
                        reader.Read();
                        count++;
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    before = GC.GetTotalMemory(true);
                    while (reader.Read())
                    {
                        Assert.Equal(count, reader.GetInt32(0));
                        count++;
                    }
                    var after = GC.GetTotalMemory(true);
                    Assert.True(after - before < 2 * 1024 * 1024, $"The memory grew by {after - before} bytes.");
                }
                Assert.Equal(200000, count);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
