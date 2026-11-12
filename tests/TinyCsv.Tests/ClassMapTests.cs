namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Mafesoft.TinyCsv.Conversions;
    using Xunit;

    public class ClassMapTests
    {
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public DateTime Date { get; set; }
            public Supplier Supplier { get; set; }
        }

        public class Supplier
        {
            public string Name { get; set; }
        }

        public sealed class ProductMap : CsvClassMap<Product>
        {
            public ProductMap()
            {
                Options(o =>
                {
                    o.HasHeaderRecord = true;
                    o.Delimiter = ",";
                    o.NewLine = "\n";
                });
                Map(m => m.Id);
                Map(m => m.Name).Name("product_name");
                Map(m => m.Price).Format("0.00");
                Map(m => m.Date).Format("yyyy-MM-dd");
                Map(m => m.Supplier.Name);
            }
        }

        [Fact]
        public void Constructor_UsesTheColumnsAndTheOptions()
        {
            var csv = new TinyCsv<Product>(new ProductMap());

            var product = csv.LoadFromText("Id,product_name,Price,Date,Supplier_Name\n1,Pen,1.50,2024-02-29,Acme").Single();

            Assert.Equal("Id,product_name,Price,Date,Supplier_Name", csv.Options.AsColumnsHeaderLine());
            Assert.Equal("Pen", product.Name);
            Assert.Equal(1.5m, product.Price);
            Assert.Equal(new DateTime(2024, 2, 29), product.Date);
            Assert.Equal("Acme", product.Supplier.Name);
            Assert.Equal("Id,product_name,Price,Date,Supplier_Name\n1,Pen,1.50,2024-02-29,Acme", csv.GetAllText(new[] { product }));
        }

        [Fact]
        public void UseClassMap_OptionsAfterTheMap_OverrideIt()
        {
            var csv = new TinyCsv<Product>(o =>
            {
                o.UseClassMap<ProductMap>();
                o.Delimiter = ";";
            });

            var product = csv.LoadFromText("Id;product_name;Price;Date;Supplier_Name\n1;Pen;2;2024-01-01;Acme").Single();

            Assert.Equal(2m, product.Price);
        }

        public sealed class IndexMap : CsvClassMap<Product>
        {
            public IndexMap()
            {
                Map(m => m.Name).Index(2);
                Map(m => m.Id).Index(0);
                Map(m => m.Price).Index(1).FormatProvider(new CultureInfo("it-IT"));
            }
        }

        [Fact]
        public void Index_ReadsTheField()
        {
            var csv = new TinyCsv<Product>(new IndexMap());

            var product = csv.LoadFromText("7;2,5;Pen").Single();

            Assert.Equal(7, product.Id);
            Assert.Equal(2.5m, product.Price);
            Assert.Equal("Pen", product.Name);
        }

        public sealed class UpperConverter : IValueConverter
        {
            public string Convert(object value, object parameter, IFormatProvider provider) => value?.ToString().ToLowerInvariant();

            public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => value?.ToUpperInvariant();
        }

        public sealed class ConverterMap : CsvClassMap<Product>
        {
            public ConverterMap()
            {
                Map(m => m.Name).Converter(new UpperConverter());
            }
        }

        [Fact]
        public void Converter_ConvertsTheValue()
        {
            var csv = new TinyCsv<Product>(new ConverterMap());

            var product = csv.LoadFromText("pen").Single();

            Assert.Equal("PEN", product.Name);
            Assert.Equal(new[] { "pen" }, csv.GetAllLines(new[] { product }));
        }

        [Fact]
        public void UseClassMap_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TinyCsv<Product>(o => o.UseClassMap(null)));
        }
    }
}
