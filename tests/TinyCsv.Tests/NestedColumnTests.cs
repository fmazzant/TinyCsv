namespace TinyCsv.Tests
{
    using System;
    using System.Linq;
    using TinyCsv.Exceptions;
    using Xunit;

    public class NestedColumnTests
    {
        public class Address
        {
            public string City { get; set; }
            public Country Country { get; set; }
        }

        public class Country
        {
            public string Code { get; set; }
        }

        public class Customer
        {
            public int Id { get; set; }
            public Address Address { get; set; }
            public string[] Tags { get; set; }
            public Customer Parent { get; set; }
        }

        [Fact]
        public void AutomaticColumns_NestedProperties_AreColumns()
        {
            var csv = new TinyCsv<Customer>(o => { o.HasHeaderRecord = true; o.Delimiter = ","; });

            Assert.Equal("Id,Address_City,Address_Country_Code", csv.Options.AsColumnsHeaderLine());
        }

        [Fact]
        public void AutomaticColumns_ReadAndWrite()
        {
            var csv = new TinyCsv<Customer>(o => { o.HasHeaderRecord = true; o.Delimiter = ","; o.NewLine = "\n"; });

            var customers = csv.LoadFromText("Id,Address_City,Address_Country_Code\n1,Roma,IT\n2,,").ToArray();

            Assert.Equal("Roma", customers[0].Address.City);
            Assert.Equal("IT", customers[0].Address.Country.Code);
            Assert.Equal("Id,Address_City,Address_Country_Code\n1,Roma,IT\n2,,", csv.GetAllText(customers));
        }

        [Fact]
        public void AddColumn_NestedProperty_NameJoinedByUnderscore()
        {
            var csv = new TinyCsv<Customer>(o =>
            {
                o.HasHeaderRecord = true;
                o.Delimiter = ",";
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Address.City);
                o.Columns.AddColumn("Country", m => m.Address.Country.Code);
            });

            var customer = csv.LoadFromText("Id,Address_City,Country\n1,Roma,IT").Single();

            Assert.Equal("Id,Address_City,Country", csv.Options.AsColumnsHeaderLine());
            Assert.Equal("Roma", customer.Address.City);
            Assert.Equal("IT", customer.Address.Country.Code);
        }

        [Fact]
        public void Write_NullNestedObject_IsEmpty()
        {
            var csv = new TinyCsv<Customer>(o =>
            {
                o.Delimiter = ",";
                o.Columns.AddColumn(m => m.Id);
                o.Columns.AddColumn(m => m.Address.Country.Code);
            });

            var lines = csv.GetAllLines(new[] { new Customer { Id = 1 }, new Customer { Id = 2, Address = new Address() } });

            Assert.Equal(new[] { "1,", "2," }, lines);
        }

        [Fact]
        public void MapColumnsByHeader_NestedColumns()
        {
            var csv = new TinyCsv<Customer>(o => { o.HasHeaderRecord = true; o.MapColumnsByHeader = true; o.Delimiter = ","; });

            var customer = csv.LoadFromText("Address_Country_Code,Id,Address_City\nIT,7,Roma").Single();

            Assert.Equal(7, customer.Id);
            Assert.Equal("IT", customer.Address.Country.Code);
        }

        [Fact]
        public void AddColumn_PropertyOfString_Throws()
        {
            Assert.Throws<ArgumentException>(() => new TinyCsv<Person>(o => o.Columns.AddColumn(m => m.Name.Length)));
        }

        public class Order
        {
            public int Id { get; set; }
            public Total Total { get; set; }
        }

        public class Total
        {
            public decimal Amount { get; set; }
        }

        [Fact]
        public void NestedValue_NotValid_ThrowsInvalidColumnValueException()
        {
            var csv = new TinyCsv<Order>(o => { o.Delimiter = ","; o.ValidateValues = true; });

            var exception = Assert.Throws<InvalidColumnValueException>(() => csv.LoadFromText("1,abc").ToArray());

            Assert.Equal("Total_Amount", exception.ColumnName);
        }

        [Fact]
        public void NestedValue_UsesTheCulture()
        {
            var csv = new TinyCsv<Order>(o => { o.Delimiter = ";"; o.Culture = new System.Globalization.CultureInfo("it-IT"); });

            var order = csv.LoadFromText("1;12,5").Single();

            Assert.Equal(12.5m, order.Total.Amount);
            Assert.Equal(new[] { "1;12,5" }, csv.GetAllLines(new[] { order }));
        }
    }
}
