namespace TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using TinyCsv.Attributes;
    using TinyCsv.Exceptions;
    using Xunit;

    [HasHeaderRecord(true)]
    [MapColumnsByHeader]
    public class MapByHeaderAttributeModel
    {
        [Column(name: "Full Name")]
        public string Name { get; set; }

        [Column]
        public int Id { get; set; }
    }

    [Delimiter(",")]
    [HasHeaderRecord(true)]
    public class NoColumnAttributeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
    }

    public class AutoMappedModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public DayOfWeek Day { get; set; }

        // ignored: not a supported type
        public Address Address { get; set; }
        public List<int> Values { get; set; }

        // ignored: read only and write only
        public string ReadOnly => Name;
        public string WriteOnly { set { } }
    }

    public class HeaderMappingTests
    {
        private static TinyCsv<Person> ByHeader(Action<CsvOptions<Person>> configure = null)
        {
            return Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.MapColumnsByHeader = true;
                configure?.Invoke(o);
            });
        }

        // ---------- map columns by header ----------

        [Fact]
        public void MapByHeader_ColumnsInAnotherOrder()
        {
            var result = ByHeader().LoadFromText("City;Id;Name\nRoma;1;Mario\nMilano;2;Luigi").ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal((1, "Mario", "Roma"), (result[0].Id, result[0].Name, result[0].City));
            Assert.Equal((2, "Luigi", "Milano"), (result[1].Id, result[1].Name, result[1].City));
        }

        [Fact]
        public void MapByHeader_ExtraColumnsAreIgnored()
        {
            var result = ByHeader().LoadFromText("Country;Name;Zip;City;Id\nIT;Mario;00100;Roma;1").Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void MapByHeader_IgnoresCaseAndSpacesByDefault()
        {
            var result = ByHeader().LoadFromText("CITY; name ;iD\nRoma;Mario;1").Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void MapByHeader_QuotedHeaderNames()
        {
            var result = ByHeader().LoadFromText("\"City\";\"Name\";\"Id\"\nRoma;Mario;1").Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void MapByHeader_DuplicateHeaderName_FirstIsUsed()
        {
            var result = ByHeader().LoadFromText("Id;Name;City;Name\n1;Mario;Roma;Other").Single();

            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void MapByHeader_MissingColumn_Throws()
        {
            var csv = ByHeader();

            var exception = Assert.Throws<MissingColumnException>(() => csv.LoadFromText("Id;Town\n1;Roma").ToList());

            Assert.Equal(new[] { "Name", "City" }, exception.MissingColumns);
            Assert.Contains("Name, City", exception.Message);
        }

        [Fact]
        public void MapByHeader_CaseSensitiveComparer()
        {
            var csv = ByHeader(o => o.HeaderNameComparer = StringComparer.Ordinal);

            var exception = Assert.Throws<MissingColumnException>(() => csv.LoadFromText("id;Name;City\n1;Mario;Roma").ToList());

            Assert.Equal(new[] { "Id" }, exception.MissingColumns);
        }

        [Fact]
        public void MapByHeader_WithoutHeaderRecord_Throws()
        {
            var csv = Csv.Person(o => o.MapColumnsByHeader = true);

            Assert.Throws<InvalidOperationException>(() => csv.LoadFromText("1;Mario;Roma").ToList());
        }

        [Fact]
        public void MapByHeader_HeaderAfterRowsToSkip()
        {
            var csv = ByHeader(o => o.RowsToSkip = 1);

            var result = csv.LoadFromText("exported on 2026-11-02\nName;Id;City\nMario;1;Roma").Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void MapByHeader_ShortRow_MissingValuesAreDefault()
        {
            var result = ByHeader().LoadFromText("Name;City;Id\nMario").Single();

            Assert.Equal("Mario", result.Name);
            Assert.Null(result.City);
            Assert.Equal(0, result.Id);
        }

        [Fact]
        public void MapByHeader_EmptyText_NoRows()
        {
            Assert.Empty(ByHeader().LoadFromText(string.Empty));
        }

        [Fact]
        public void MapByHeader_FromStream()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("City;Id;Name\nRoma;1;Mario");

            var result = ByHeader().LoadFromStream(new MemoryStream(bytes)).Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void MapByHeader_SameInstanceReadsFilesWithDifferentOrders()
        {
            var csv = ByHeader();

            var first = csv.LoadFromText("Id;Name;City\n1;Mario;Roma").Single();
            var second = csv.LoadFromText("City;Name;Id\nMilano;Luigi;2").Single();

            Assert.Equal((1, "Mario", "Roma"), (first.Id, first.Name, first.City));
            Assert.Equal((2, "Luigi", "Milano"), (second.Id, second.Name, second.City));
        }

        [Fact]
        public void ByPosition_IsStillTheDefault()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var result = csv.LoadFromText("City;Id;Name\nRoma;1;Mario").Single();

            // by position: Id <- "Roma" (not a number), Name <- "1", City <- "Mario"
            Assert.False(csv.Options.MapColumnsByHeader);
            Assert.Equal((0, "1", "Mario"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void MapByHeader_Attribute()
        {
            var csv = new TinyCsv<MapByHeaderAttributeModel>();

            var result = csv.LoadFromText("Id;Full Name\n1;Mario Rossi").Single();

            Assert.True(csv.Options.MapColumnsByHeader);
            Assert.Equal((1, "Mario Rossi"), (result.Id, result.Name));
        }

        [Fact]
        public void MapByHeader_SavedFileIsReadBack()
        {
            var csv = ByHeader();
            var people = new[] { new Person { Id = 1, Name = "Mario", City = "Roma" } };

            var result = csv.LoadFromText(csv.GetAllText(people)).Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

#if NET5_0_OR_GREATER
        [Fact]
        public async Task MapByHeader_Async()
        {
            var result = new List<Person>();
            await foreach (var person in ByHeader().LoadFromTextAsync("City;Id;Name\nRoma;1;Mario"))
            {
                result.Add(person);
            }

            Assert.Equal((1, "Mario", "Roma"), (result[0].Id, result[0].Name, result[0].City));
        }

        [Fact]
        public async Task MapByHeader_Async_MissingColumn_Throws()
        {
            await Assert.ThrowsAsync<MissingColumnException>(async () =>
            {
                await foreach (var _ in ByHeader().LoadFromTextAsync("Id;Name\n1;Mario"))
                {
                }
            });
        }
#else
        [Fact]
        public async Task MapByHeader_Async()
        {
            var result = (await ByHeader().LoadFromTextAsync("City;Id;Name\nRoma;1;Mario")).ToList();

            Assert.Equal((1, "Mario", "Roma"), (result[0].Id, result[0].Name, result[0].City));
        }

        [Fact]
        public async Task MapByHeader_Async_MissingColumn_Throws()
        {
            await Assert.ThrowsAsync<MissingColumnException>(() => ByHeader().LoadFromTextAsync("Id;Name\n1;Mario"));
        }
#endif

        // ---------- automatic columns ----------

        [Fact]
        public void AutoColumns_FromPublicProperties()
        {
            var csv = new TinyCsv<AutoMappedModel>(o => o.HasHeaderRecord = true);

            Assert.Equal(new[] { "Id", "Name", "Price", "CreatedOn", "Day" }, csv.Options.Columns.Select(c => c.ColumnName));
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, csv.Options.Columns.Select(c => c.ColumnIndex));
        }

        [Fact]
        public void AutoColumns_Read()
        {
            var csv = new TinyCsv<AutoMappedModel>(o => o.HasHeaderRecord = true);

            var result = csv.LoadFromText("Id;Name;Price;CreatedOn;Day\n1;Mario;1.5;2026-11-02;Monday").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal("Mario", result.Name);
            Assert.Equal(1.5m, result.Price);
            Assert.Equal(new DateTime(2026, 11, 2), result.CreatedOn);
            Assert.Equal(DayOfWeek.Monday, result.Day);
            Assert.Null(result.Address);
        }

        [Fact]
        public void AutoColumns_WriteAndReadBack()
        {
            var csv = new TinyCsv<AutoMappedModel>(o => o.HasHeaderRecord = true);
            var model = new AutoMappedModel { Id = 1, Name = "Mario", Price = null, CreatedOn = new DateTime(2026, 11, 2), Day = DayOfWeek.Friday };

            var lines = csv.GetAllLines(new[] { model });
            var result = csv.LoadFromText(string.Join(Environment.NewLine, lines)).Single();

            Assert.Equal("Id;Name;Price;CreatedOn;Day", lines[0]);
            Assert.Equal((1, "Mario", (decimal?)null, model.CreatedOn, DayOfWeek.Friday), (result.Id, result.Name, result.Price, result.CreatedOn, result.Day));
        }

        [Fact]
        public void AutoColumns_NotUsedWhenColumnsAreDefined()
        {
            var csv = Csv.Person();

            Assert.Equal(new[] { "Id", "Name", "City" }, csv.Options.Columns.Select(c => c.ColumnName));

            var onlyName = new TinyCsv<Person>(o => o.Columns.AddColumn(m => m.Name));
            Assert.Equal(new[] { "Name" }, onlyName.Options.Columns.Select(c => c.ColumnName));
        }

        [Fact]
        public void AutoColumns_WithMapColumnsByHeader()
        {
            var csv = new TinyCsv<Person>(o =>
            {
                o.HasHeaderRecord = true;
                o.MapColumnsByHeader = true;
            });

            var result = csv.LoadFromText("Notes;City;Name;Id\nnone;Roma;Mario;1").Single();

            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }

        [Fact]
        public void AutoColumns_AttributeConstructorWithoutColumnAttributes()
        {
            var csv = new TinyCsv<NoColumnAttributeModel>();

            var result = csv.LoadFromText("Id,Name\n1,Mario").Single();

            Assert.Equal((1, "Mario"), (result.Id, result.Name));
        }

        [Fact]
        public void AutoColumns_OptionsConstructor()
        {
            var options = new CsvOptions<Person> { HasHeaderRecord = false };

            var csv = new TinyCsv<Person>(options);
            var result = csv.LoadFromText("1;Mario;Roma").Single();

            Assert.Equal(3, options.Columns.Count);
            Assert.Equal((1, "Mario", "Roma"), (result.Id, result.Name, result.City));
        }
    }
}
