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
    using System.Linq;
    using Mafesoft.TinyCsv.Attributes;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    [HasHeaderRecord(true)]
    [MapColumnsByHeader(true)]
    public class AlternativeNamesModel
    {
        [Column(names: new[] { "Id", "ID", "Code" })]
        public int Id { get; set; }

        [Column(name: "Name")]
        public string Name { get; set; }

        [Column(new[] { "City", "Town" }, format: null), Optional]
        public string City { get; set; }
    }

    public class AlternativeHeaderNamesTests
    {
        private sealed class PersonMap : CsvClassMap<Person>
        {
            public PersonMap()
            {
                Options(o =>
                {
                    o.HasHeaderRecord = true;
                    o.MapColumnsByHeader = true;
                });
                Map(m => m.Id).Name("Id", "ID", "Code");
                Map(m => m.Name).Name("Name", "FullName");
                Map(m => m.City).Name("City", "Town").Optional();
            }
        }

        private static TinyCsv<Person> Lambda()
        {
            return new TinyCsv<Person>(o =>
            {
                o.HasHeaderRecord = true;
                o.MapColumnsByHeader = true;
                o.Columns.AddColumn(m => m.Id).WithNames("ID", "Code");
                o.Columns.AddColumn("Name", m => m.Name).WithNames("FullName");
                o.Columns.AddColumn(m => m.City);
            });
        }

        [Theory]
        [InlineData("Id;Name;City\n7;Mario;Roma")]
        [InlineData("ID;Name;City\n7;Mario;Roma")]
        [InlineData("City;Code;Name\nRoma;7;Mario")]
        [InlineData("Code;FullName;City\n7;Mario;Roma")]
        public void Lambda_EachName_IsFound(string text)
        {
            var result = Lambda().LoadFromText(text).Single();

            Assert.Equal((7, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Theory]
        [InlineData("Id;Name;City\n7;Mario;Roma")]
        [InlineData("CODE;FullName;Town\n7;Mario;Roma")]
        public void ClassMap_EachName_IsFound(string text)
        {
            var result = new TinyCsv<Person>(new PersonMap()).LoadFromText(text).Single();

            Assert.Equal((7, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Theory]
        [InlineData("Id;Name;City\n7;Mario;Roma")]
        [InlineData("Code;Name;Town\n7;Mario;Roma")]
        public void Attribute_EachName_IsFound(string text)
        {
            var result = new TinyCsv<AlternativeNamesModel>().LoadFromText(text).Single();

            Assert.Equal((7, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void FirstNameFoundInTheHeader_Wins()
        {
            // the names of the column in order: Id, ID, Code
            var result = Lambda().LoadFromText("Code;Name;City;Id\n1;Mario;Roma;2").Single();

            Assert.Equal(2, result.Id);
        }

        [Fact]
        public void Names_AreComparedWithTheHeaderNameComparer()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.HasHeaderRecord = true;
                o.MapColumnsByHeader = true;
                o.HeaderNameComparer = StringComparer.Ordinal;
                o.Columns.AddColumn(m => m.Id).WithNames("Code");
            });

            Assert.Equal(7, csv.LoadFromText("code;Code\n1;7").Single().Id);
            Assert.Throws<MissingColumnException>(() => csv.LoadFromText("code;ID\n1;7").ToList());
        }

        [Fact]
        public void FirstName_IsWritten()
        {
            var people = new[] { new Person { Id = 7, Name = "Mario", City = "Roma" } };

            Assert.Equal("Id;Name;City", Lambda().GetAllLines(people)[0]);
            Assert.Equal("Id;Name;City", new TinyCsv<Person>(new PersonMap()).GetAllLines(people)[0]);
            Assert.Equal("Id;Name;City", new TinyCsv<AlternativeNamesModel>().GetAllLines(new[] { new AlternativeNamesModel() })[0]);
        }

        [Fact]
        public void MissingColumn_ListsAllTheNames()
        {
            var exception = Assert.Throws<MissingColumnException>(() => Lambda().LoadFromText("Key;Town\n7;Roma").ToList());

            Assert.Equal(new[] { "Id", "Name", "City" }, exception.MissingColumns);
            Assert.Contains("Id (or ID, Code), Name (or FullName), City", exception.Message);
        }

        [Fact]
        public void OptionalColumn_WithAlternativeNames_MayBeMissing()
        {
            var result = new TinyCsv<Person>(new PersonMap()).LoadFromText("ID;FullName\n7;Mario").Single();

            Assert.Equal((7, "Mario", null), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void ColumnNames_FirstIsTheColumnName()
        {
            var column = Lambda().Options.Columns.First();

            Assert.Equal("Id", column.ColumnName);
            Assert.Equal(new[] { "Id", "ID", "Code" }, column.ColumnNames);
        }

        [Fact]
        public void ColumnNames_WithoutAlternatives()
        {
            var column = Lambda().Options.Columns.Last();

            Assert.Equal(new[] { "City" }, column.ColumnNames);
        }

        [Fact]
        public void WithNames_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TinyCsv<Person>(o => o.Columns.AddColumn(m => m.Id).WithNames(null)));
        }

        [Fact]
        public void Attribute_EmptyNames_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ColumnAttribute(new string[0]));
        }
    }
}
