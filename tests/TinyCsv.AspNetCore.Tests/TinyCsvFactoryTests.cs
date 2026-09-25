namespace TinyCsv.AspNetCore.Tests
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using TinyCsv.AspNetCore.Extensions;
    using Xunit;

    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TinyCsvFactoryTests
    {
        private static void Configure(CsvOptions<Model> options)
        {
            options.Delimiter = ";";
            options.Columns.AddColumn(m => m.Id);
            options.Columns.AddColumn(m => m.Name);
        }

        // the factory registry is static: every test uses its own service names
        private static string UniqueName() => Guid.NewGuid().ToString("N");

        [Fact]
        public void Create_ReturnsConfiguredInstance()
        {
            var csv = new TinyCsvFactory().Create<Model>(Configure);

            var result = csv.LoadFromText("1;Mario").Single();

            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void AddAndGet()
        {
            var factory = new TinyCsvFactory();
            var name = UniqueName();
            var csv = factory.Create<Model>(Configure);

            factory.Add(name, csv);

            Assert.Same(csv, factory.Get<Model>(name));
            Assert.Same(csv, new TinyCsvFactory().Get<Model>(name));
        }

        [Fact]
        public void Add_DuplicateName_Throws()
        {
            var factory = new TinyCsvFactory();
            var name = UniqueName();
            factory.Add(name, factory.Create<Model>(Configure));

            Assert.ThrowsAny<ArgumentException>(() => factory.Add(name, factory.Create<Model>(Configure)));
        }

        [Fact]
        public void Get_UnknownName_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => new TinyCsvFactory().Get<Model>(UniqueName()));
        }

        [Fact]
        public void AddTinyCsv_RegistersSingletonFactory()
        {
            var services = new ServiceCollection();

            services.AddTinyCsv();
            services.AddTinyCsv();
            var provider = services.BuildServiceProvider();

            Assert.Single(services, s => s.ServiceType == typeof(ITinyCsvFactory));
            Assert.Same(provider.GetRequiredService<ITinyCsvFactory>(), provider.GetRequiredService<ITinyCsvFactory>());
        }

        [Fact]
        public void AddTinyCsv_WithName_IsResolvedFromFactory()
        {
            var services = new ServiceCollection();
            var name = UniqueName();

            services.AddTinyCsv<Model>(name, Configure);
            var factory = services.BuildServiceProvider().GetRequiredService<ITinyCsvFactory>();

            var result = factory.Get<Model>(name).LoadFromText("7;Luigi").Single();

            Assert.Equal(7, result.Id);
            Assert.Equal("Luigi", result.Name);
        }
    }
}
