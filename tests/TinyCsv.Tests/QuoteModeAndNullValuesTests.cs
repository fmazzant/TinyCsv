/// <summary>
/// 
/// The MIT License (MIT)
/// 
/// Copyright (c) 2020 Federico Mazzanti
/// 
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
/// 
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
/// 
/// </summary>

namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Mafesoft.TinyCsv.Attributes;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public class QuoteModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int? Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Note { get; set; }
    }

    [QuoteMode(CsvQuoteMode.All)]
    [NullValues("NULL", "\\N")]
    [NullValueOnWrite("NULL")]
    public class QuoteAttributeModel
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        [NullValues("N/A")]
        public string Note { get; set; }
    }

    public class NullValuesAutoModel
    {
        public int Id { get; set; }

        [NullValues("-")]
        public string Name { get; set; }
    }

    public class QuoteModeAndNullValuesTests
    {
        private static readonly DateTime Date = new DateTime(2024, 1, 31);

        private static TinyCsv<QuoteModel> Csv(Action<CsvOptions<QuoteModel>> configure = null)
        {
            return new TinyCsv<QuoteModel>(options =>
            {
                configure?.Invoke(options);
            });
        }

        private static QuoteModel Model(string name = "Mario", string note = null, int? quantity = 3)
        {
            return new QuoteModel { Id = 1, Name = name, Price = 1.5m, Quantity = quantity, Date = Date, Note = note };
        }

        // ---------- quote mode ----------

        [Fact]
        public void QuoteMode_DefaultIsMinimal()
        {
            Assert.Equal(CsvQuoteMode.Minimal, new CsvOptions<QuoteModel>().QuoteMode);

            var text = Csv().GetAllText(new[] { Model("Rossi; Mario", "") });

            Assert.Equal("1;\"Rossi; Mario\";1.5;3;01/31/2024 00:00:00;", text);
        }

        [Fact]
        public void QuoteMode_All_QuotesEveryField()
        {
            var csv = Csv(o => o.QuoteMode = CsvQuoteMode.All);

            var text = csv.GetAllText(new[] { Model("Rossi \"Mario\"", "") });

            Assert.Equal("\"1\";\"Rossi \"\"Mario\"\"\";\"1.5\";\"3\";\"01/31/2024 00:00:00\";\"\"", text);
        }

        [Fact]
        public void QuoteMode_All_NullIsAnEmptyFieldWithoutQuotes()
        {
            var csv = Csv(o => o.QuoteMode = CsvQuoteMode.All);

            var text = csv.GetAllText(new[] { Model(null, "", null) });

            Assert.Equal("\"1\";;\"1.5\";;\"01/31/2024 00:00:00\";\"\"", text);
        }

        [Fact]
        public void QuoteMode_All_QuotesTheHeader()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.All;
                o.HasHeaderRecord = true;
            });

            var lines = csv.GetAllLines(new[] { Model() });

            Assert.Equal("\"Id\";\"Name\";\"Price\";\"Quantity\";\"Date\";\"Note\"", lines[0]);
        }

        [Fact]
        public void QuoteMode_All_Writer()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.All;
                o.HasHeaderRecord = true;
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Name);
            });
            var stream = new MemoryStream();

            using (var writer = csv.CreateWriter(stream))
            {
                writer.WriteRecord(Model());
            }

            Assert.Equal("\"Id\";\"Name\"\r\n\"1\";\"Mario\"\r\n", Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public void QuoteMode_NonNumeric_QuotesTheTextFields()
        {
            var csv = Csv(o => o.QuoteMode = CsvQuoteMode.NonNumeric);

            var text = csv.GetAllText(new[] { Model("Mario", "a;b", -3) });

            Assert.Equal("1;\"Mario\";1.5;-3;\"01/31/2024 00:00:00\";\"a;b\"", text);
        }

        [Fact]
        public void QuoteMode_NonNumeric_NumberWithTheDelimiter_IsQuotedAsMinimal()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.NonNumeric;
                o.Delimiter = ",";
                o.Culture = new System.Globalization.CultureInfo("it-IT");
                o.Columns.AddColumn(m => m.Price);
                o.Columns.AddColumn(m => m.Name);
            });

            Assert.Equal("\"1,5\",\"Mario\"", csv.GetAllText(new[] { Model() }));
        }

        [Fact]
        public void QuoteMode_None_NeverQuotes()
        {
            var csv = Csv(o => o.QuoteMode = CsvQuoteMode.None);

            var text = csv.GetAllText(new[] { Model("Rossi \"Mario\"", "") });

            Assert.Equal("1;Rossi \"Mario\";1.5;3;01/31/2024 00:00:00;", text);
        }

        [Theory]
        [InlineData("a;b")]
        [InlineData("a\nb")]
        [InlineData("a\rb")]
        public void QuoteMode_None_DelimiterOrNewLine_Throws(string name)
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.None;
                o.HasHeaderRecord = true;
            });

            var exception = Assert.Throws<InvalidColumnValueException>(() => csv.GetAllText(new[] { Model(), Model(name) }));

            Assert.Equal("Name", exception.ColumnName);
            Assert.Equal(2, exception.RowIndex);
        }

        [Fact]
        public void QuoteMode_None_MultiCharDelimiter_Throws()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.None;
                o.Delimiter = "||";
            });

            Assert.Equal("1||a|b||1.5||3||01/31/2024 00:00:00||", csv.GetAllText(new[] { Model("a|b") }));
            Assert.Throws<InvalidColumnValueException>(() => csv.GetAllText(new[] { Model("a||b") }));
        }

        [Theory]
        [InlineData(CsvQuoteMode.Minimal)]
        [InlineData(CsvQuoteMode.All)]
        [InlineData(CsvQuoteMode.NonNumeric)]
        public void QuoteMode_RoundTrip(CsvQuoteMode mode)
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = mode;
                o.HasHeaderRecord = true;
            });
            var models = new[] { Model("Rossi; \"Mario\"\r\nRoma", "note"), Model(null, "", null) };

            var result = csv.LoadFromText(csv.GetAllText(models)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(models[0].Name, result[0].Name);
            Assert.Equal(1.5m, result[0].Price);
            Assert.Equal(3, result[0].Quantity);
            Assert.Equal(Date, result[0].Date);
            Assert.Equal("note", result[0].Note);
            Assert.Null(result[1].Quantity);
        }

        [Fact]
        public void QuoteMode_All_WithInjectionEscape()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.All;
                o.InjectionProtection = CsvInjectionProtection.Escape;
                o.Columns.AddColumn(m => m.Name);
            });

            Assert.Equal("\"'=1\"", csv.GetAllText(new[] { Model("=1") }));
        }

        [Fact]
        public void QuoteMode_None_WithInjectionEscape_OnlyPrefixes()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.None;
                o.InjectionProtection = CsvInjectionProtection.Escape;
                o.Columns.AddColumn(m => m.Name);
            });

            Assert.Equal("'=1", csv.GetAllText(new[] { Model("=1") }));
        }

        // ---------- null values ----------

        [Fact]
        public void NullValues_Defaults()
        {
            var options = new CsvOptions<QuoteModel>();

            Assert.Null(options.NullValues);
            Assert.Equal(string.Empty, options.NullValueOnWrite);
        }

        [Fact]
        public void NullValues_Read_BecomeNull()
        {
            var csv = Csv(o => o.NullValues = new[] { "NULL", "\\N" });

            var result = csv.LoadFromText("1;NULL;NULL;\\N;2024-01-31;\n2;;1;NULL;2024-01-31;\\N").ToList();

            Assert.Null(result[0].Name);
            Assert.Equal(0m, result[0].Price);
            Assert.Null(result[0].Quantity);
            Assert.Equal("", result[1].Name);
            Assert.Null(result[1].Quantity);
            Assert.Null(result[1].Note);
        }

        [Fact]
        public void NullValues_Read_AreCaseSensitiveAndExact()
        {
            var csv = Csv(o => o.NullValues = new[] { "NULL" });

            var result = csv.LoadFromText("1;null;1;1;2024-01-31; NULL").Single();

            Assert.Equal("null", result.Name);
            Assert.Equal(" NULL", result.Note);
        }

        [Fact]
        public void NullValues_Read_WithValidateValues_DoNotThrow()
        {
            var csv = Csv(o =>
            {
                o.NullValues = new[] { "N/A" };
                o.ValidateValues = true;
            });

            var result = csv.LoadFromText("1;a;N/A;N/A;N/A;").Single();

            Assert.Equal(0m, result.Price);
            Assert.Null(result.Quantity);
            Assert.Equal(default(DateTime), result.Date);
        }

        [Fact]
        public void NullValues_Read_PerColumn_InTheClassMap()
        {
            var csv = new TinyCsv<QuoteModel>(new NullValuesMap());

            var result = csv.LoadFromText("NULL;N/A\nN/A;NULL").ToList();

            // the null values of the column replace the null values of the options
            Assert.Null(result[0].Name);
            Assert.Null(result[0].Note);
            Assert.Equal("N/A", result[1].Name);
            Assert.Equal("NULL", result[1].Note);
        }

        [Fact]
        public void NullValues_Read_NestedColumn()
        {
            var csv = new TinyCsv<InjectionNestedModel>(o =>
            {
                o.NullValues = new[] { "NULL" };
                o.Columns.AddColumn(m => m.Item.Name);
                o.Columns.AddColumn(m => m.Item.Quantity);
            });

            var result = csv.LoadFromText("NULL;NULL").Single();

            Assert.Null(result.Item.Name);
            Assert.Equal(0, result.Item.Quantity);
        }

        [Fact]
        public void NullValues_Write_NullValueOnWrite()
        {
            var csv = Csv(o => o.NullValueOnWrite = "NULL");

            var text = csv.GetAllText(new[] { Model(null, "", null) });

            Assert.Equal("1;NULL;1.5;NULL;01/31/2024 00:00:00;", text);
        }

        [Fact]
        public void NullValues_RoundTrip_NullAndEmpty()
        {
            var csv = Csv(o =>
            {
                o.NullValues = new[] { "\\N" };
                o.NullValueOnWrite = "\\N";
                o.HasHeaderRecord = true;
            });
            var models = new[] { Model(null, ""), Model("", null) };

            var text = csv.GetAllText(models);
            var result = csv.LoadFromText(text).ToList();

            Assert.Null(result[0].Name);
            Assert.Equal("", result[0].Note);
            Assert.Equal("", result[1].Name);
            Assert.Null(result[1].Note);
        }

        [Fact]
        public void NullValues_RoundTrip_QuoteModeAll_NullAndEmpty()
        {
            var csv = Csv(o =>
            {
                o.QuoteMode = CsvQuoteMode.All;
                o.NullValues = new[] { "NULL" };
                o.NullValueOnWrite = "NULL";
            });
            var models = new[] { Model(null, ""), Model("NULL", null) };

            var text = csv.GetAllText(models);
            var result = csv.LoadFromText(text).ToList();

            Assert.Null(result[0].Name);
            Assert.Equal("", result[0].Note);
            Assert.Null(result[1].Note);
        }

        [Fact]
        public void NullValues_Attributes()
        {
            var csv = new TinyCsv<QuoteAttributeModel>();

            var text = csv.GetAllText(new[] { new QuoteAttributeModel { Id = 1, Name = null, Note = "x" } });
            var result = csv.LoadFromText("1;\\N;N/A\n2;N/A;NULL").ToList();

            Assert.Equal("\"1\";NULL;\"x\"", text);
            Assert.Null(result[0].Name);
            Assert.Null(result[0].Note);
            Assert.Equal("N/A", result[1].Name);
            Assert.Equal("NULL", result[1].Note);
        }

        [Fact]
        public void NullValues_Attribute_AutomaticColumns()
        {
            var csv = new TinyCsv<NullValuesAutoModel>(o => { });

            var result = csv.LoadFromText("1;-").Single();

            Assert.Null(result.Name);
        }

        [Fact]
        public void NullValues_Records()
        {
            var records = new TinyCsvRecords(o => o.NullValues = new[] { "NULL" });

            var record = records.LoadFromText("Id;Name\n1;NULL").Single();

            Assert.Equal("1", record["Id"]);
            Assert.Null(record["Name"]);
        }

        private sealed class NullValuesMap : CsvClassMap<QuoteModel>
        {
            public NullValuesMap()
            {
                Options(o => o.NullValues = new[] { "NULL" });
                Map(m => m.Name);
                Map(m => m.Note).NullValues("N/A");
            }
        }
    }
}
