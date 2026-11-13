#if NET10_0_OR_GREATER
namespace Mafesoft.TinyCsv.AspNetCore.Tests
{
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    /// <summary>
    /// The minimal API sample (samples/CsvSampleMinimalWebApiApp, .NET 10), run in memory by WebApplicationFactory:
    /// the typed instance, the named instances (factory and keyed service) and the CSV response
    /// </summary>
    public class MinimalApiSampleTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> factory;

        public MinimalApiSampleTests(WebApplicationFactory<Program> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task Root_ReturnsTheEndpoints()
        {
            var text = await factory.CreateClient().GetStringAsync("/");

            Assert.Contains("/model1", text);
        }

        [Theory]
        [InlineData("/model1")]
        [InlineData("/model2")]
        [InlineData("/model2-it")]
        public async Task Models_AreStreamedAsJson(string url)
        {
            var response = await factory.CreateClient().GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var models = await response.Content.ReadFromJsonAsync<JsonElement[]>();
            Assert.NotNull(models);
            Assert.Equal(Enumerable.Range(1, 18), models.Select(m => m.GetProperty("id").GetInt32()));
            Assert.Equal("Name 3", models[2].GetProperty("name").GetString());
        }

        [Fact]
        public async Task Model2Csv_IsWrittenToTheResponse()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("/model2.csv");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            var lines = (await response.Content.ReadAsStringAsync()).Split("\r\n").Where(line => line.Length > 0).ToArray();
            // the header and one line for each model
            Assert.Equal(19, lines.Length);
            Assert.StartsWith("Id;Name;", lines[0]);
            Assert.StartsWith("3;Name 3;", lines[3]);
        }
    }
}
#endif
