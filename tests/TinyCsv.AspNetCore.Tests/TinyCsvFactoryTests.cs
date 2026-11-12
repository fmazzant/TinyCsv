namespace Mafesoft.TinyCsv.AspNetCore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Mafesoft.TinyCsv.AspNetCore.Extensions;
    using Xunit;

    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Other
    {
        public int Id { get; set; }
    }

    public class TinyCsvFactoryTests
    {
        private static void Configure(CsvOptions<Model> options)
        {
            options.Delimiter = ";";
            options.Columns.AddColumn(m => m.Id);
            options.Columns.AddColumn(m => m.Name);
        }

        [Fact]
        public void Create_ReturnsConfiguredInstance()
        {
            var csv = new TinyCsvFactory().Create<Model>(Configure);

            var result = csv.LoadFromText("1;Mario").Single();

            Assert.Equal("Mario", result.Name);
        }

        [Fact]
        public void AddAndGet_IsPerFactory()
        {
            var factory = new TinyCsvFactory();
            var csv = factory.Create<Model>(Configure);

            factory.Add("orders", csv);

            Assert.Same(csv, factory.Get<Model>("orders"));
            Assert.Throws<KeyNotFoundException>(() => new TinyCsvFactory().Get<Model>("orders"));
        }

        [Fact]
        public void Add_DuplicateName_Throws()
        {
            var factory = new TinyCsvFactory();
            factory.Add("orders", factory.Create<Model>(Configure));

            Assert.Throws<InvalidOperationException>(() => factory.Add("orders", factory.Create<Model>(Configure)));
        }

        [Fact]
        public void Get_UnknownName_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => new TinyCsvFactory().Get<Model>("unknown"));
            Assert.Throws<ArgumentNullException>(() => new TinyCsvFactory().Get<Model>(null));
        }

        [Fact]
        public void Get_OtherModel_Throws()
        {
            var factory = new TinyCsvFactory();
            factory.Add("orders", factory.Create<Model>(Configure));

            Assert.Throws<InvalidOperationException>(() => factory.Get<Other>("orders"));
            Assert.False(factory.TryGet<Other>("orders", out _));
            Assert.True(factory.TryGet<Model>("orders", out var csv));
            Assert.NotNull(csv);
        }

        [Fact]
        public void AddTinyCsv_RegistersSingletonFactory()
        {
            var services = new ServiceCollection();

            services.AddTinyCsv().AddTinyCsv();
            var provider = services.BuildServiceProvider();

            Assert.Single(services, s => s.ServiceType == typeof(ITinyCsvFactory));
            Assert.Same(provider.GetRequiredService<ITinyCsvFactory>(), provider.GetRequiredService<ITinyCsvFactory>());
        }

        [Fact]
        public void AddTinyCsv_Typed_RegistersSingleton()
        {
            var provider = new ServiceCollection()
                .AddTinyCsv<Model>(Configure)
                .BuildServiceProvider();

            var csv = provider.GetRequiredService<ITinyCsv<Model>>();

            Assert.Same(csv, provider.GetRequiredService<ITinyCsv<Model>>());
            Assert.Equal("Luigi", csv.LoadFromText("7;Luigi").Single().Name);
        }

        [Fact]
        public void AddTinyCsv_WithName_IsResolvedFromFactory()
        {
            var provider = new ServiceCollection()
                .AddTinyCsv<Model>("orders", Configure)
                .BuildServiceProvider();
            var factory = provider.GetRequiredService<ITinyCsvFactory>();

            var result = factory.Get<Model>("orders").LoadFromText("7;Luigi").Single();

            Assert.Equal(7, result.Id);
            Assert.Equal("Luigi", result.Name);
            Assert.Same(factory.Get<Model>("orders"), factory.Get<Model>("orders"));
        }

        [Fact]
        public void AddTinyCsv_SameNameInTwoServiceCollections_AreIndependent()
        {
            var first = new ServiceCollection()
                .AddTinyCsv<Model>("orders", o => { Configure(o); o.Delimiter = ";"; })
                .BuildServiceProvider()
                .GetRequiredService<ITinyCsvFactory>();
            var second = new ServiceCollection()
                .AddTinyCsv<Model>("orders", o => { Configure(o); o.Delimiter = ","; })
                .BuildServiceProvider()
                .GetRequiredService<ITinyCsvFactory>();

            Assert.NotSame(first, second);
            Assert.NotSame(first.Get<Model>("orders"), second.Get<Model>("orders"));
            Assert.Equal("Mario", first.Get<Model>("orders").LoadFromText("1;Mario").Single().Name);
            Assert.Equal("Mario", second.Get<Model>("orders").LoadFromText("1,Mario").Single().Name);
        }

        [Fact]
        public void AddTinyCsv_OnlyInOneServiceCollection_IsNotVisibleInTheOther()
        {
            new ServiceCollection().AddTinyCsv<Model>("orders", Configure).BuildServiceProvider();
            var other = new ServiceCollection().AddTinyCsv().BuildServiceProvider().GetRequiredService<ITinyCsvFactory>();

            Assert.False(other.TryGet<Model>("orders", out _));
        }

        [Fact]
        public void AddTinyCsv_DuplicateName_Throws()
        {
            var services = new ServiceCollection().AddTinyCsv<Model>("orders", Configure);

            Assert.Throws<InvalidOperationException>(() => services.AddTinyCsv<Other>("orders", o => { }));
        }

        [Fact]
        public void AddTinyCsv_EmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTinyCsv<Model>("", Configure));
        }

        [Fact]
        public void AddTinyCsv_WithName_IsKeyedService()
        {
            var provider = new ServiceCollection()
                .AddTinyCsv<Model>("orders-it", o => { Configure(o); o.Delimiter = ";"; })
                .AddTinyCsv<Model>("orders-us", o => { Configure(o); o.Delimiter = ","; })
                .BuildServiceProvider();

            var it = provider.GetRequiredKeyedService<ITinyCsv<Model>>("orders-it");
            var us = provider.GetRequiredKeyedService<ITinyCsv<Model>>("orders-us");

            Assert.Same(provider.GetRequiredService<ITinyCsvFactory>().Get<Model>("orders-it"), it);
            Assert.Equal("Mario", it.LoadFromText("1;Mario").Single().Name);
            Assert.Equal("Mario", us.LoadFromText("1,Mario").Single().Name);
        }
    }
}
