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
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Attributes;
    using Mafesoft.TinyCsv.Conversions;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public class InjectionModel
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int? Delta { get; set; }
        public string Note { get; set; }
    }

    [InjectionProtection(CsvInjectionProtection.Strip)]
    public class InjectionAttributeModel
    {
        [Column]
        public string Name { get; set; }
    }

    [InjectionProtection(CsvInjectionProtection.Escape, new[] { '!' })]
    public class InjectionCharactersAttributeModel
    {
        [Column]
        public string Name { get; set; }
    }

    public class InjectionNestedModel
    {
        public InjectionModel Item { get; set; }
    }

    public class InjectionProtectionTests
    {
        private static TinyCsv<InjectionModel> Csv(CsvInjectionProtection protection, Action<CsvOptions<InjectionModel>> configure = null)
        {
            return new TinyCsv<InjectionModel>(options =>
            {
                options.InjectionProtection = protection;
                configure?.Invoke(options);
            });
        }

        private static string Write<T>(TinyCsv<T> csv, params T[] models)
            where T : class, new()
        {
            return csv.GetAllText(models);
        }

        // ---------- defaults ----------

        [Fact]
        public void Default_IsNone_WithTheOwaspCharacters()
        {
            var options = new CsvOptions<InjectionModel>();

            Assert.Equal(CsvInjectionProtection.None, options.InjectionProtection);
            Assert.Equal(new[] { '=', '+', '-', '@', '\t', '\r' }, options.InjectionCharacters);
        }

        [Fact]
        public void None_WritesTheValueAsItIs()
        {
            var text = Write(Csv(CsvInjectionProtection.None), new InjectionModel { Name = "=1+2", Note = "@SUM(A1)" });

            Assert.Equal("=1+2;0;0;;@SUM(A1)", text);
        }

        // ---------- escape ----------

        [Theory]
        [InlineData("=1+2")]
        [InlineData("+1")]
        [InlineData("-1")]
        [InlineData("@SUM(A1)")]
        [InlineData("\tvalue")]
        [InlineData("=HYPERLINK(\"http://example.com\")")]
        public void Escape_PrefixesWithTheApostropheAndQuotes(string value)
        {
            var text = Write(Csv(CsvInjectionProtection.Escape), new InjectionModel { Name = value });

            var expected = "\"'" + value.Replace("\"", "\"\"") + "\"";
            Assert.Equal(expected + ";0;0;;", text);
        }

        [Fact]
        public void Escape_CarriageReturn_IsEscaped()
        {
            var text = Write(Csv(CsvInjectionProtection.Escape), new InjectionModel { Name = "\r=1" });

            Assert.Equal("\"'\r=1\";0;0;;", text);
        }

        [Fact]
        public void Escape_ValueAlsoQuoted_ForTheDelimiterAndTheQuotes()
        {
            var text = Write(Csv(CsvInjectionProtection.Escape), new InjectionModel { Name = "=1;2", Note = "=\"a\"" });

            Assert.Equal("\"'=1;2\";0;0;;\"'=\"\"a\"\"\"", text);
        }

        [Fact]
        public void Escape_SafeValues_AreNotChanged()
        {
            var text = Write(Csv(CsvInjectionProtection.Escape), new InjectionModel { Name = "a=1", Note = "Rossi; Mario" });

            Assert.Equal("a=1;0;0;;\"Rossi; Mario\"", text);
        }

        [Fact]
        public void Escape_Numbers_AreNotChanged()
        {
            var text = Write(Csv(CsvInjectionProtection.Escape), new InjectionModel { Name = "-5", Quantity = -5, Price = -1.5m, Delta = -3 });

            Assert.Equal("\"'-5\";-5;-1.5;-3;", text);
        }

        [Fact]
        public void Escape_ReadBack_KeepsTheApostrophe()
        {
            var csv = Csv(CsvInjectionProtection.Escape);

            var model = csv.LoadFromText(Write(csv, new InjectionModel { Name = "=1;2" })).Single();

            Assert.Equal("'=1;2", model.Name);
        }

        [Fact]
        public void Escape_WithoutQuoting_OnlyPrefixes()
        {
            var csv = Csv(CsvInjectionProtection.Escape, o => o.AllowRowEnclosedInDoubleQuotesValues = false);

            Assert.Equal("'=1;0;0;;", Write(csv, new InjectionModel { Name = "=1" }));
        }

        [Fact]
        public void Escape_CustomCharacters()
        {
            var csv = Csv(CsvInjectionProtection.Escape, o => o.InjectionCharacters = new[] { '!' });

            Assert.Equal("\"'!cmd\";0;0;;=1", Write(csv, new InjectionModel { Name = "!cmd", Note = "=1" }));
        }

        [Fact]
        public void Escape_NullCharacters_AreTheDefault()
        {
            var csv = Csv(CsvInjectionProtection.Escape, o => o.InjectionCharacters = null);

            Assert.Equal("\"'=1\";0;0;;", Write(csv, new InjectionModel { Name = "=1" }));
        }

        [Fact]
        public void Escape_ColumnWithConverter()
        {
            var csv = Csv(CsvInjectionProtection.Escape, o =>
            {
                o.Columns.AddColumn(m => m.Name, new UpperConverter());
            });

            Assert.Equal("\"'=SUM(A1)\"", Write(csv, new InjectionModel { Name = "=sum(a1)" }));
        }

        [Fact]
        public void Escape_NestedColumn()
        {
            var csv = new TinyCsv<InjectionNestedModel>(o =>
            {
                o.InjectionProtection = CsvInjectionProtection.Escape;
                o.Columns.AddColumn(m => m.Item.Name);
                o.Columns.AddColumn(m => m.Item.Quantity);
            });

            Assert.Equal("\"'=1\";-1", Write(csv, new InjectionNestedModel { Item = new InjectionModel { Name = "=1", Quantity = -1 } }));
        }

        // ---------- strip ----------

        [Fact]
        public void Strip_RemovesTheLeadingCharacters()
        {
            var text = Write(Csv(CsvInjectionProtection.Strip), new InjectionModel { Name = "=+-@1", Note = "@a;b" });

            Assert.Equal("1;0;0;;\"a;b\"", text);
        }

        [Fact]
        public void Strip_OnlyInjectionCharacters_IsEmpty()
        {
            var text = Write(Csv(CsvInjectionProtection.Strip), new InjectionModel { Name = "==", Quantity = -1 });

            Assert.Equal(";-1;0;;", text);
        }

        // ---------- throw ----------

        [Fact]
        public void Throw_ThrowsWithTheRowAndTheColumn()
        {
            var csv = Csv(CsvInjectionProtection.Throw, o => o.HasHeaderRecord = true);
            var models = new[] { new InjectionModel { Name = "a" }, new InjectionModel { Name = "b", Note = "=1" } };

            var exception = Assert.Throws<CsvInjectionException>(() => csv.GetAllText(models));

            Assert.IsAssignableFrom<CsvException>(exception);
            Assert.Equal(2, exception.RowIndex);
            Assert.Equal("Note", exception.ColumnName);
            Assert.Equal("=1", exception.Value);
            Assert.Contains("row 2, column Note", exception.Message);
        }

        [Fact]
        public void Throw_Numbers_DoNotThrow()
        {
            var text = Write(Csv(CsvInjectionProtection.Throw), new InjectionModel { Name = "a", Quantity = -1, Delta = -2 });

            Assert.Equal("a;-1;0;-2;", text);
        }

        [Fact]
        public void Throw_Save_Stream()
        {
            var csv = Csv(CsvInjectionProtection.Throw);

            var exception = Assert.Throws<CsvInjectionException>(() => csv.Save(new MemoryStream(), new[] { new InjectionModel { Name = "a" }, new InjectionModel { Name = "+1" } }));

            Assert.Equal(1, exception.RowIndex);
            Assert.Equal("Name", exception.ColumnName);
        }

        [Fact]
        public async Task Throw_Writer_Async()
        {
            var csv = Csv(CsvInjectionProtection.Throw);
            var writer = csv.CreateWriter(new MemoryStream());

            var exception = await Assert.ThrowsAsync<CsvInjectionException>(() => writer.WriteRecordAsync(new InjectionModel { Name = "@a" }));

            Assert.Equal(0, exception.RowIndex);
        }

        // ---------- writer, attributes ----------

        [Fact]
        public void Writer_EscapesTheRecords()
        {
            var csv = Csv(CsvInjectionProtection.Escape);
            var stream = new MemoryStream();

            using (var writer = csv.CreateWriter(stream))
            {
                writer.WriteRecord(new InjectionModel { Name = "=1" });
            }

            Assert.Equal("\"'=1\";0;0;;\r\n", Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public void Attribute_SetsTheProtection()
        {
            var csv = new TinyCsv<InjectionAttributeModel>();

            Assert.Equal("1", csv.GetAllText(new[] { new InjectionAttributeModel { Name = "=1" } }));
        }

        [Fact]
        public void Attribute_SetsTheCharacters()
        {
            var csv = new TinyCsv<InjectionCharactersAttributeModel>();

            Assert.Equal("\"'!1\"", csv.GetAllText(new[] { new InjectionCharactersAttributeModel { Name = "!1" } }));
            Assert.Equal("=1", csv.GetAllText(new[] { new InjectionCharactersAttributeModel { Name = "=1" } }));
        }

        private sealed class UpperConverter : IValueConverter
        {
            public string Convert(object value, object parameter, IFormatProvider provider) => value?.ToString()?.ToUpperInvariant() ?? string.Empty;

            public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => value;
        }
    }
}
