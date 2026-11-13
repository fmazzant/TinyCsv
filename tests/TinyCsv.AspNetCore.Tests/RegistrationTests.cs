namespace Mafesoft.TinyCsv.AspNetCore.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.AspNetCore.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// The registrations of AddTinyCsv: typed and named instances together, the options, the thread safety of the singletons
    /// </summary>
    public class RegistrationTests
    {
        private static void Configure(CsvOptions<Model> options)
        {
            options.Delimiter = ";";
            options.Columns.AddColumn(m => m.Id);
            options.Columns.AddColumn(m => m.Name);
        }

        [Fact]
        public void AddTinyCsv_NullServices_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TinyCsvExtensions.AddTinyCsv(null));
            Assert.Throws<ArgumentNullException>(() => TinyCsvExtensions.AddTinyCsv<Model>(null, "orders", Configure));
        }

        [Fact]
        public void AddTinyCsv_TypedAndNamed_AreDifferentInstances()
        {
            var provider = new ServiceCollection()
                .AddTinyCsv<Model>(Configure)
                .AddTinyCsv<Model>("orders-us", o => { Configure(o); o.Delimiter = ","; })
                .BuildServiceProvider();

            var typed = provider.GetRequiredService<ITinyCsv<Model>>();
            var named = provider.GetRequiredService<ITinyCsvFactory>().Get<Model>("orders-us");

            Assert.NotSame(typed, named);
            Assert.Equal("Mario", typed.LoadFromText("1;Mario").Single().Name);
            Assert.Equal("Mario", named.LoadFromText("1,Mario").Single().Name);
        }

        [Fact]
        public void AddTinyCsv_TheOptionsAreAppliedOncePerContainer()
        {
            var calls = 0;
            var services = new ServiceCollection().AddTinyCsv<Model>("orders", o => { calls++; Configure(o); });

            // the instance is created only when it is resolved
            var factory = services.BuildServiceProvider().GetRequiredService<ITinyCsvFactory>();
            Assert.Equal(0, calls);

            factory.Get<Model>("orders");
            factory.Get<Model>("orders");
            Assert.Equal(1, calls);

            // every container has its own instance
            services.BuildServiceProvider().GetRequiredService<ITinyCsvFactory>().Get<Model>("orders");
            Assert.Equal(2, calls);
        }

        [Fact]
        public void AddTinyCsv_TwoModelsWithDifferentNames()
        {
            var factory = new ServiceCollection()
                .AddTinyCsv<Model>("models", Configure)
                .AddTinyCsv<Other>("others", o => o.Columns.AddColumn(m => m.Id))
                .BuildServiceProvider()
                .GetRequiredService<ITinyCsvFactory>();

            Assert.Equal("Mario", factory.Get<Model>("models").LoadFromText("1;Mario").Single().Name);
            Assert.Equal(42, factory.Get<Other>("others").LoadFromText("42").Single().Id);
        }

        [Fact]
        public async Task Singleton_IsThreadSafe()
        {
            var csv = new ServiceCollection()
                .AddTinyCsv<Model>(o => { Configure(o); o.HasHeaderRecord = true; })
                .BuildServiceProvider()
                .GetRequiredService<ITinyCsv<Model>>();
            var models = Enumerable.Range(0, 200).Select(i => new Model { Id = i, Name = $"Name; \"{i}\"" }).ToArray();
            var expected = csv.GetAllText(models);

            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
            {
                var text = csv.GetAllText(models);
                var read = csv.LoadFromText(text).ToArray();
                return (Text: text, Read: read);
            })));

            foreach (var result in results)
            {
                Assert.Equal(expected, result.Text);
                Assert.Equal(models.Select(m => (m.Id, m.Name)), result.Read.Select(m => (m.Id, m.Name)));
            }
        }
    }
}
