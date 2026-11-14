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
    using Xunit;

    [WriteByteOrderMark(true)]
    [WriteSeparatorLine(true)]
    [HasHeaderRecord(true)]
    public class ExcelPerson
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }
    }

    public class ExcelFriendlyTests : IDisposable
    {
        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
        private readonly string path = Path.GetTempFileName();

        public void Dispose()
        {
            File.Delete(path);
        }

        private static TinyCsv<Person> Csv(Action<CsvOptions<Person>> configure = null)
        {
            return global::Mafesoft.TinyCsv.Tests.Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                configure?.Invoke(o);
            });
        }

        private static readonly Person[] People = { new Person { Id = 1, Name = "Mattè", City = "Roma" } };

        private static bool HasBom(byte[] bytes) => bytes.Length >= 3 && bytes.Take(3).SequenceEqual(Utf8Bom);

        private static byte[] SaveToStream(TinyCsv<Person> csv)
        {
            var stream = new MemoryStream();
            csv.Save(stream, People);
            return stream.ToArray();
        }

        private byte[] SaveToFile(TinyCsv<Person> csv)
        {
            csv.Save(path, People);
            return File.ReadAllBytes(path);
        }

        private static byte[] WriterToStream(TinyCsv<Person> csv)
        {
            var stream = new MemoryStream();
            using (var writer = csv.CreateWriter(stream))
            {
                writer.WriteRecords(People);
            }
            return stream.ToArray();
        }

        // ---------- byte order mark ----------

        [Fact]
        public void Bom_Default_IsNull_AndNotWritten()
        {
            var csv = Csv();

            Assert.Null(csv.Options.WriteByteOrderMark);
            Assert.False(HasBom(SaveToStream(csv)));
            Assert.False(HasBom(SaveToFile(csv)));
            Assert.False(HasBom(WriterToStream(csv)));
            Assert.NotEqual('﻿', csv.GetAllText(People)[0]);
        }

        [Fact]
        public void Bom_True_IsWritten()
        {
            var csv = Csv(o => o.WriteByteOrderMark = true);

            Assert.True(HasBom(SaveToStream(csv)));
            Assert.True(HasBom(SaveToFile(csv)));
            Assert.True(HasBom(WriterToStream(csv)));
            Assert.Equal("﻿Id;Name;City\r\n1;Mattè;Roma", csv.GetAllText(People));
        }

        [Fact]
        public async Task Bom_True_IsWritten_Async()
        {
            var csv = Csv(o => o.WriteByteOrderMark = true);
            var stream = new MemoryStream();

            await csv.SaveAsync(stream, People);
            var text = await csv.GetAllTextAsync(People);

            Assert.True(HasBom(stream.ToArray()));
            Assert.Equal('﻿', text[0]);
        }

        [Fact]
        public void Bom_False_IsNotWritten_EvenWithAnEncodingWithTheBom()
        {
            var csv = Csv(o =>
            {
                o.TextEncoding = new UTF8Encoding(true);
                o.WriteByteOrderMark = false;
            });

            Assert.False(HasBom(SaveToStream(csv)));
            Assert.False(HasBom(SaveToFile(csv)));
            Assert.False(HasBom(WriterToStream(csv)));
        }

        [Fact]
        public void Bom_Null_FollowsTheEncoding()
        {
            var csv = Csv(o => o.TextEncoding = new UTF8Encoding(true));

            Assert.True(HasBom(SaveToStream(csv)));
        }

        [Fact]
        public void Bom_Unicode()
        {
            var with = SaveToStream(Csv(o =>
            {
                o.TextEncoding = new UnicodeEncoding(false, false);
                o.WriteByteOrderMark = true;
            }));
            var without = SaveToStream(Csv(o =>
            {
                o.TextEncoding = Encoding.Unicode;
                o.WriteByteOrderMark = false;
            }));

            Assert.Equal(new byte[] { 0xFF, 0xFE }, with.Take(2));
            Assert.Equal((byte)'I', without[0]);
        }

        [Fact]
        public void Bom_ReadBack()
        {
            var csv = Csv(o => o.WriteByteOrderMark = true);

            var fromStream = csv.LoadFromStream(new MemoryStream(SaveToStream(csv))).Single();
            var fromText = csv.LoadFromText(csv.GetAllText(People)).Single();

            Assert.Equal((1, "Mattè"), (fromStream.Id, fromStream.Name));
            Assert.Equal((1, "Mattè"), (fromText.Id, fromText.Name));
        }

        // ---------- sep= line ----------

        [Fact]
        public void SeparatorLine_Default_IsNotWritten()
        {
            Assert.False(Csv().Options.WriteSeparatorLine);
            Assert.Equal("Id;Name;City\r\n1;Mattè;Roma", Csv().GetAllText(People));
        }

        [Fact]
        public void SeparatorLine_IsWrittenBeforeTheHeader()
        {
            var csv = Csv(o =>
            {
                o.WriteSeparatorLine = true;
                o.Delimiter = ",";
            });

            Assert.Equal(new[] { "sep=,", "Id,Name,City", "1,Mattè,Roma" }, csv.GetAllLines(People));
            Assert.Equal("sep=,\r\nId,Name,City\r\n1,Mattè,Roma\r\n", Encoding.UTF8.GetString(SaveToStream(csv)));
            Assert.Equal("sep=,\r\nId,Name,City\r\n1,Mattè,Roma\r\n", Encoding.UTF8.GetString(WriterToStream(csv)));
        }

        [Fact]
        public void SeparatorLine_WithoutHeader_AndWithoutRecords()
        {
            var csv = Csv(o =>
            {
                o.WriteSeparatorLine = true;
                o.HasHeaderRecord = false;
            });
            var stream = new MemoryStream();

            using (csv.CreateWriter(stream))
            {
            }

            Assert.Equal("sep=;\r\n", Encoding.UTF8.GetString(stream.ToArray()));
            Assert.Equal("sep=;\r\n1;Mattè;Roma", csv.GetAllText(People));
        }

        [Fact]
        public void SeparatorLine_WithBom()
        {
            var csv = Csv(o =>
            {
                o.WriteSeparatorLine = true;
                o.WriteByteOrderMark = true;
            });

            var bytes = SaveToStream(csv);

            Assert.True(HasBom(bytes));
            Assert.StartsWith("sep=;\r\n", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
        }

        [Theory]
        [InlineData("sep=,\nId,Name,City\n1,Mario,Roma")]
        [InlineData("sep=,\r\nId,Name,City\r\n1,Mario,Roma")]
        [InlineData("SEP=,\nId,Name,City\n1,Mario,Roma")]
        public void SeparatorLine_Read_SetsTheDelimiterAndIsSkipped(string text)
        {
            var result = Csv().LoadFromText(text).Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void SeparatorLine_Read_Tab()
        {
            var result = Csv().LoadFromText("sep=\t\nId\tName\tCity\n1\tMario\tRoma").Single();

            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void SeparatorLine_Read_WinsOverDetectDelimiter()
        {
            var csv = Csv(o => o.DetectDelimiter = true);

            var result = csv.LoadFromText("sep=|\nId|Name|City\n1|Mario, Luigi|Roma").Single();

            Assert.Equal("Mario, Luigi", result.Name);
        }

        [Fact]
        public void SeparatorLine_Read_DetectDelimiter_WithoutTheLine()
        {
            var csv = Csv(o => o.DetectDelimiter = true);

            var result = csv.LoadFromText("Id,Name,City\n1,Mario,Roma").Single();

            Assert.Equal("Roma", result.City);
        }

        [Fact]
        public void SeparatorLine_Read_BeforeRowsToSkip()
        {
            var csv = Csv(o => o.RowsToSkip = 1);

            var result = csv.LoadFromText("sep=,\ntitle\nId,Name,City\n1,Mario,Roma").Single();

            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void SeparatorLine_Read_OnlyTheFirstLine()
        {
            var csv = Csv(o => o.HasHeaderRecord = false);

            var result = csv.LoadFromText("1;sep=,;Roma").Single();

            Assert.Equal("sep=,", result.Name);
        }

        [Fact]
        public void SeparatorLine_Read_EmptySeparator_IsData()
        {
            var csv = Csv(o => o.HasHeaderRecord = false);

            var result = csv.LoadFromText("sep=\n1;Mario;Roma").ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SeparatorLine_Read_Stream_File_AndAsync()
        {
            var csv = Csv(o =>
            {
                o.WriteSeparatorLine = true;
                o.WriteByteOrderMark = true;
                o.Delimiter = ",";
            });
            var reader = Csv();
            csv.Save(path, People);

            Assert.Equal("Mattè", reader.LoadFromStream(new MemoryStream(SaveToStream(csv))).Single().Name);
            Assert.Equal("Mattè", reader.LoadFromFile(path).Single().Name);
            var names = new System.Collections.Generic.List<string>();
            await foreach (var person in reader.LoadFromFileAsync(path))
            {
                names.Add(person.Name);
            }
            Assert.Equal(new[] { "Mattè" }, names);
        }

        [Fact]
        public void SeparatorLine_Read_Records()
        {
            var records = new TinyCsvRecords();

            var record = records.LoadFromText("sep=,\nId,Name\n1,Mario").Single();

            Assert.Equal("Mario", record["Name"]);
        }

        [Fact]
        public void SeparatorLine_Read_LongFirstLine()
        {
            var name = new string('x', 10000);
            var csv = Csv(o => o.HasHeaderRecord = false);

            var result = csv.LoadFromText("1;" + name + ";Roma\n2;b;c").ToList();

            Assert.Equal(name, result[0].Name);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Attributes_SetTheOptions()
        {
            var csv = new TinyCsv<ExcelPerson>();
            var stream = new MemoryStream();

            csv.Save(stream, new[] { new ExcelPerson { Id = 1, Name = "a" } });

            var bytes = stream.ToArray();
            Assert.True(HasBom(bytes));
            Assert.Equal("sep=;\r\nId;Name\r\n1;a\r\n", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
            Assert.Equal("a", csv.LoadFromStream(new MemoryStream(bytes)).Single().Name);
        }
    }
}
